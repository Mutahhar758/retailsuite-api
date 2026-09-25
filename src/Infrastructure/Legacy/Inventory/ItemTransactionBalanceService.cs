using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Inventory;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Infrastructure.Persistence.Transactions;
using Retailer.Infrastructure.State;

namespace Retailer.Infrastructure.Legacy.Inventory;

public class ItemTransactionBalanceService : IItemTransactionBalanceService
{
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly EfTransactionManager _transactionManager;
    private readonly ILogger<ItemTransactionBalanceService> _logger;

    public ItemTransactionBalanceService(
        IRepository<ItemTransaction> itemTransactionRepository,
        ApplicationDbContext dbContext,
        EfTransactionManager transactionManager,
        ILogger<ItemTransactionBalanceService> logger)
    {
        _itemTransactionRepository = itemTransactionRepository;
        _dbContext = dbContext;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task ProcessVoucherRunningBalancesAsync(
        string tenantId,
        string voucherType,
        string voucherNo,
        DateOnly voucherDate,
        IReadOnlyList<ItemTransaction> transactions,
        CancellationToken cancellationToken = default)
    {
        if (transactions == null || transactions.Count == 0)
            return;

        var itemsGroup = transactions
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .GroupBy(x => x.ItemId!);

        foreach (var group in itemsGroup)
        {
            var itemId = group.Key;

            // Ensure a database transaction is active and lock the item row to serialize concurrent balance updates
            await EnsureRowLockAsync(itemId, cancellationToken);

            var voucherItemTxs = group
                .OrderBy(x => x.TranType == "in" ? 0 : 1)
                .ThenBy(x => x.Seq)
                .ToList();

            // Find the most recent transaction for this item on or before voucherDate (excluding this voucher)
            var prevTx = await _itemTransactionRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.ItemId == itemId
                    && !(x.VType == voucherType && x.VNo == voucherNo)
                    && x.VDate <= voucherDate)
                .OrderByDescending(x => x.VDate)
                .ThenByDescending(x => x.VTime)
                .ThenByDescending(x => x.TranType == "in" ? 0 : 1)
                .ThenByDescending(x => x.Id)
                .Select(x => new { x.Id, x.RunningQtyBalance, x.VDate })
                .FirstOrDefaultAsync(cancellationToken);

            decimal runningQty = prevTx?.RunningQtyBalance ?? 0m;

            foreach (var tx in voucherItemTxs)
            {
                runningQty += (tx.QtyIn - tx.QtyOut);
                tx.RunningQtyBalance = runningQty;
            }

            // Check if there are subsequent transactions (backdated entry)
            var hasSubsequent = await _itemTransactionRepository.GetAll()
                .AsNoTracking()
                .AnyAsync(x => x.ItemId == itemId
                    && !(x.VType == voucherType && x.VNo == voucherNo)
                    && (x.VDate > voucherDate || (x.VDate == voucherDate && prevTx != null && x.Id > prevTx.Id)),
                    cancellationToken);

            if (hasSubsequent || voucherDate < DateOnly.FromDateTime(DateTime.Today))
            {
                _logger.LogInformation(
                    "Backdated transaction detected for Item {ItemId} at {VoucherDate}. Enqueuing FIFO and running balance rebuild.",
                    itemId, voucherDate);

                BackgroundJob.Enqueue<IFifoCostingService>(x =>
                    x.RebuildItemFifoAsync(tenantId, itemId, voucherDate, CancellationToken.None));
            }
        }
    }

    private async Task EnsureRowLockAsync(string itemId, CancellationToken cancellationToken)
    {
        try
        {
            if (_transactionManager.Transaction == null && ApplicationState.IsStarted)
            {
                _transactionManager.Transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            var provider = _dbContext.Database.ProviderName;
            if (provider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM ItemDetail WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}",
                    new object[] { itemId },
                    cancellationToken);
            }
            else if (provider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM \"public\".\"ItemDetail\" WHERE id = {0} FOR UPDATE",
                    new object[] { itemId },
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not acquire row lock for Item {ItemId}. Proceeding with balance calculation.", itemId);
        }
    }
}
