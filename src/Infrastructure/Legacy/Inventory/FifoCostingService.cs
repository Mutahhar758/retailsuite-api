using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Inventory;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Auth;
using Retailer.Infrastructure.Multitenancy;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Infrastructure.Persistence.Transactions;
using Retailer.Infrastructure.State;
using Retailer.Shared.Common;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Legacy.Inventory;

public class FifoCostingService : IFifoCostingService
{
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<TransactionFifoMapping> _fifoMappingRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly EfTransactionManager _transactionManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FifoCostingService> _logger;

    public FifoCostingService(
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<TransactionFifoMapping> fifoMappingRepository,
        ApplicationDbContext dbContext,
        EfTransactionManager transactionManager,
        IServiceProvider serviceProvider,
        ILogger<FifoCostingService> logger)
    {
        _itemTransactionRepository = itemTransactionRepository;
        _fifoMappingRepository = fifoMappingRepository;
        _dbContext = dbContext;
        _transactionManager = transactionManager;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<FifoAllocationResult> AllocateFifoCostAsync(
        ItemTransaction outTx,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outTx.ItemId) || outTx.QtyOut <= 0)
        {
            outTx.CostPrice = 0m;
            outTx.CostAmount = 0m;
            return new FifoAllocationResult(0m, 0m, 0m, new List<TransactionFifoMapping>());
        }

        // Fetch available "in" batches for this item received on or before this sale date, ordered chronologically
        var availableBatches = await _itemTransactionRepository.GetAll()
            .Where(x => x.ItemId == outTx.ItemId && x.TranType == "in" && x.RemainingQty > 0 && x.VDate <= outTx.VDate)
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.VTime)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        decimal neededQty = outTx.QtyOut;
        decimal totalCost = 0m;
        var mappings = new List<TransactionFifoMapping>();

        foreach (var batch in availableBatches)
        {
            if (neededQty <= 0)
            {
                break;
            }

            var takeQty = Math.Min(batch.RemainingQty, neededQty);
            batch.RemainingQty -= takeQty;
            await _itemTransactionRepository.UpdateAsync(batch, false);

            var mapping = new TransactionFifoMapping
            {
                OutTransactionId = outTx.Id,
                InTransactionId = batch.Id,
                QtyConsumed = takeQty,
                CostRate = batch.Rate,
                CostAmount = Math.Round(takeQty * batch.Rate, 2)
            };

            await _fifoMappingRepository.AddAsync(mapping, false);
            mappings.Add(mapping);

            totalCost += takeQty * batch.Rate;
            neededQty -= takeQty;
        }

        decimal shortageQty = Math.Max(0, neededQty);

        // Stamp output costs (shortfall has rate 0)
        outTx.CostAmount = Math.Round(totalCost, 2);
        outTx.CostPrice = outTx.QtyOut > 0 ? Math.Round(totalCost / outTx.QtyOut, 4) : 0m;

        await _itemTransactionRepository.UpdateAsync(outTx, false);

        _logger.LogInformation(
            "FIFO cost allocated for Tx {OutTxId}: Item {ItemId}, Qty {Qty}, CostPrice {CostPrice}, CostAmount {CostAmount}, Shortage {Shortage}",
            outTx.Id, outTx.ItemId, outTx.QtyOut, outTx.CostPrice, outTx.CostAmount, shortageQty);

        return new FifoAllocationResult(outTx.CostPrice.Value, outTx.CostAmount.Value, shortageQty, mappings);
    }

    public Task MarkDirtyAsync(
        string itemId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return Task.CompletedTask;
        }

        return MarkDirtyAsync(new[] { itemId }, fromDate, cancellationToken);
    }

    public async Task MarkDirtyAsync(
        IEnumerable<string> itemIds,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        if (itemIds is null)
        {
            return;
        }

        var ids = itemIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        if (_transactionManager.Transaction == null && ApplicationState.IsStarted)
        {
            _transactionManager.Transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        await _dbContext.Set<ItemDetail>()
            .Where(i => ids.Contains(i.Id) && (i.FifoRebuildFromDate == null || i.FifoRebuildFromDate > fromDate))
            .ExecuteUpdateAsync(
                s => s.SetProperty(i => i.FifoRebuildFromDate, fromDate),
                cancellationToken);

        foreach (var entry in _dbContext.ChangeTracker.Entries<ItemDetail>())
        {
            if (ids.Contains(entry.Entity.Id) &&
                (entry.Entity.FifoRebuildFromDate == null || entry.Entity.FifoRebuildFromDate > fromDate))
            {
                entry.Entity.FifoRebuildFromDate = fromDate;
                entry.Property(x => x.FifoRebuildFromDate).IsModified = false;
            }
        }

        _logger.LogDebug(
            "Marked {Count} item(s) dirty for FIFO rebuild from {FromDate}: {ItemIds}",
            ids.Count, fromDate, string.Join(", ", ids));
    }

    public async Task FlushDirtyItemsAsync(
        IReadOnlyCollection<string>? itemIds = null,
        CancellationToken cancellationToken = default)
    {
        if (itemIds is not null && itemIds.Count == 0)
        {
            return;
        }

        var query = _dbContext.Set<ItemDetail>()
            .AsNoTracking()
            .Where(x => x.FifoRebuildFromDate != null);

        if (itemIds is not null)
        {
            var distinctIds = itemIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (distinctIds.Count == 0)
            {
                return;
            }

            query = query.Where(x => distinctIds.Contains(x.Id));
        }

        var dirtyItems = await query
            .Select(x => new { x.Id, FromDate = x.FifoRebuildFromDate!.Value })
            .ToListAsync(cancellationToken);

        if (dirtyItems.Count == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Flushing FIFO rebuild for {Count} dirty item(s).",
            dirtyItems.Count);

        foreach (var dirty in dirtyItems)
        {
            bool startedOwnTx = false;
            if (_transactionManager.Transaction == null)
            {
                _transactionManager.Transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                startedOwnTx = true;
            }

            try
            {
                await AcquireItemRowLockAsync(_dbContext, dirty.Id, cancellationToken);

                var currentFromDate = await _dbContext.Set<ItemDetail>()
                    .AsNoTracking()
                    .Where(x => x.Id == dirty.Id)
                    .Select(x => x.FifoRebuildFromDate)
                    .FirstOrDefaultAsync(cancellationToken);

                if (currentFromDate.HasValue)
                {
                    await RebuildItemFifoCoreAsync(
                        dirty.Id,
                        currentFromDate.Value,
                        _dbContext,
                        _itemTransactionRepository,
                        _fifoMappingRepository,
                        cancellationToken);
                }

                if (startedOwnTx && _transactionManager.Transaction != null)
                {
                    await _transactionManager.Transaction.CommitAsync(cancellationToken);
                    await _transactionManager.Transaction.DisposeAsync();
                    _transactionManager.Transaction = null;
                }

                _dbContext.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing FIFO for Item {ItemId} from {FromDate}.", dirty.Id, dirty.FromDate);
                if (startedOwnTx && _transactionManager.Transaction != null)
                {
                    await _transactionManager.Transaction.RollbackAsync(cancellationToken);
                    await _transactionManager.Transaction.DisposeAsync();
                    _transactionManager.Transaction = null;
                }

                _dbContext.ChangeTracker.Clear();
                throw;
            }
        }
    }

    public async Task FlushAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var now = PakistanTime.Now;
        using var rootScope = _serviceProvider.CreateScope();
        var tenantDbContext = rootScope.ServiceProvider.GetRequiredService<TenantDbContext>();

        var tenants = await tenantDbContext.TenantInfo
            .AsNoTracking()
            .Where(t => t.IsActive && t.ValidFrom <= now && (t.ValidUntil == null || t.ValidUntil >= now))
            .ToListAsync(cancellationToken);

        foreach (var tenant in tenants)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tenantAccessor = scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
                if (tenantAccessor is IMultiTenantContextSetter tenantSetter)
                {
                    tenantSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
                }

                var currentUserInitializer = scope.ServiceProvider.GetService<ICurrentUserInitializer>();
                currentUserInitializer?.SetCurrentUserId("system");

                var scopedFifoService = scope.ServiceProvider.GetRequiredService<IFifoCostingService>();
                await scopedFifoService.FlushDirtyItemsAsync(null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing dirty FIFO items for tenant '{TenantIdentifier}'.", tenant.Identifier);
            }
        }
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task RebuildItemFifoAsync(
        string tenantId,
        string itemId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting FIFO rebuild for Tenant {TenantId}, Item {ItemId}, from Date {FromDate}",
            tenantId, itemId, fromDate);

        using var scope = _serviceProvider.CreateScope();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("TenantId is null or empty. Aborting FIFO rebuild for Item {ItemId}.", itemId);
            return;
        }

        // 1. Establish tenant context inside background worker
        var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var tenantInfo = await tenantDbContext.TenantInfo.FirstOrDefaultAsync(
            t => t.Id == tenantId || t.Identifier == tenantId, cancellationToken);
        if (tenantInfo is null)
        {
            _logger.LogWarning("Tenant {TenantId} not found. Aborting FIFO rebuild.", tenantId);
            return;
        }

        var tenantAccessor = scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        if (tenantAccessor is IMultiTenantContextSetter tenantSetter)
        {
            tenantSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenantInfo);
        }

        var currentUserInitializer = scope.ServiceProvider.GetService<ICurrentUserInitializer>();
        currentUserInitializer?.SetCurrentUserId("system");

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var txRepo = scope.ServiceProvider.GetRequiredService<IRepository<ItemTransaction>>();
        var mapRepo = scope.ServiceProvider.GetRequiredService<IRepository<TransactionFifoMapping>>();
        var txManager = scope.ServiceProvider.GetRequiredService<EfTransactionManager>();

        try
        {
            if (txManager.Transaction == null)
            {
                txManager.Transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            await AcquireItemRowLockAsync(dbContext, itemId, cancellationToken);

            var itemFromDate = await dbContext.Set<ItemDetail>()
                .AsNoTracking()
                .Where(x => x.Id == itemId)
                .Select(x => x.FifoRebuildFromDate)
                .FirstOrDefaultAsync(cancellationToken);

            var effectiveFromDate = itemFromDate.HasValue && itemFromDate.Value < fromDate
                ? itemFromDate.Value
                : fromDate;

            await RebuildItemFifoCoreAsync(
                itemId,
                effectiveFromDate,
                dbContext,
                txRepo,
                mapRepo,
                cancellationToken);

            if (txManager.Transaction != null)
            {
                await txManager.Transaction.CommitAsync(cancellationToken);
                await txManager.Transaction.DisposeAsync();
                txManager.Transaction = null;
            }

            _logger.LogInformation(
                "Completed FIFO rebuild for Tenant {TenantId}, Item {ItemId}, from Date {FromDate}",
                tenantId, itemId, effectiveFromDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during FIFO rebuild for Tenant {TenantId}, Item {ItemId}", tenantId, itemId);
            if (txManager.Transaction != null)
            {
                await txManager.Transaction.RollbackAsync(cancellationToken);
                await txManager.Transaction.DisposeAsync();
                txManager.Transaction = null;
            }

            throw;
        }
    }

    private async Task RebuildItemFifoCoreAsync(
        string itemId,
        DateOnly fromDate,
        ApplicationDbContext dbContext,
        IRepository<ItemTransaction> txRepo,
        IRepository<TransactionFifoMapping> mapRepo,
        CancellationToken cancellationToken)
    {
        // 1. Delete mappings affected from fromDate forward
        var staleMappings = await mapRepo.GetAll()
            .Where(m => m.OutTransaction.ItemId == itemId && m.OutTransaction.VDate >= fromDate)
            .ToListAsync(cancellationToken);

        if (staleMappings.Count > 0)
        {
            await mapRepo.DeleteRangeAsync(staleMappings, false);
        }

        // 2. Reset batches
        var allInBatches = await txRepo.GetAll()
            .Where(x => x.ItemId == itemId && x.TranType == "in")
            .ToListAsync(cancellationToken);

        // Fetch surviving consumptions for batches before fromDate
        var survivingConsumptions = await mapRepo.GetAll()
            .Where(m => m.OutTransaction.ItemId == itemId && m.OutTransaction.VDate < fromDate)
            .GroupBy(m => m.InTransactionId)
            .Select(g => new { InTxId = g.Key, Consumed = g.Sum(x => x.QtyConsumed) })
            .ToDictionaryAsync(x => x.InTxId, x => x.Consumed, cancellationToken);

        foreach (var batch in allInBatches)
        {
            if (batch.VDate >= fromDate)
            {
                batch.RemainingQty = batch.QtyIn;
            }
            else
            {
                var consumedBefore = survivingConsumptions.GetValueOrDefault(batch.Id, 0m);
                batch.RemainingQty = Math.Max(0m, batch.QtyIn - consumedBefore);
            }

            await txRepo.UpdateAsync(batch, false);
        }

        // 3. Nullify sales costs from fromDate forward
        var outTxs = await txRepo.GetAll()
            .Where(x => x.ItemId == itemId && x.TranType == "out" && x.VDate >= fromDate)
            .ToListAsync(cancellationToken);

        foreach (var outTx in outTxs)
        {
            outTx.CostPrice = null;
            outTx.CostAmount = null;
            await txRepo.UpdateAsync(outTx, false);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 4. Chronological sequential replay of all transactions from fromDate forward
        // Purchases ('in') ordered before sales ('out') on the same date regardless of entry wall-clock VTime
        var replayTransactions = await txRepo.GetAll()
            .Where(x => x.ItemId == itemId && x.VDate >= fromDate)
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.TranType == "in" ? 0 : 1)
            .ThenBy(x => x.VTime)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        // Fetch surviving available batches received strictly before fromDate
        var activePool = await txRepo.GetAll()
            .Where(x => x.ItemId == itemId && x.TranType == "in" && x.VDate < fromDate && x.RemainingQty > 0)
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.VTime)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        // Seed the running balance from the last transaction before fromDate.
        // If none exists, the running balance starts at 0.
        var seedRunningQty = await txRepo.GetAll()
            .Where(x => x.ItemId == itemId && x.VDate < fromDate)
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.TranType == "in" ? 0 : 1)
            .ThenByDescending(x => x.VTime)
            .ThenByDescending(x => x.Id)
            .Select(x => (decimal?)x.RunningQtyBalance)
            .FirstOrDefaultAsync(cancellationToken) ?? 0m;

        decimal runningQty = seedRunningQty;

        foreach (var tx in replayTransactions)
        {
            if (tx.TranType == "in")
            {
                // As we step forward in time, incoming batches become available
                if (!activePool.Any(b => b.Id == tx.Id))
                {
                    activePool.Add(tx);
                    activePool = activePool
                        .OrderBy(x => x.VDate)
                        .ThenBy(x => x.VTime)
                        .ThenBy(x => x.Id)
                        .ToList();
                }

                // Inward transaction increases running balance
                runningQty += tx.QtyIn;
                tx.RunningQtyBalance = runningQty;
                await txRepo.UpdateAsync(tx, false);
            }
            else if (tx.TranType == "out")
            {
                if (tx.QtyOut > 0)
                {
                    decimal neededQty = tx.QtyOut;
                    decimal totalCost = 0m;

                    // Only consume from batches received on or before this sale date
                    var eligibleBatches = activePool
                        .Where(b => b.RemainingQty > 0 && b.VDate <= tx.VDate)
                        .ToList();

                    foreach (var batch in eligibleBatches)
                    {
                        if (neededQty <= 0)
                        {
                            break;
                        }

                        var takeQty = Math.Min(batch.RemainingQty, neededQty);
                        batch.RemainingQty -= takeQty;
                        await txRepo.UpdateAsync(batch, false);

                        var mapping = new TransactionFifoMapping
                        {
                            OutTransactionId = tx.Id,
                            InTransactionId = batch.Id,
                            QtyConsumed = takeQty,
                            CostRate = batch.Rate,
                            CostAmount = Math.Round(takeQty * batch.Rate, 2)
                        };

                        await mapRepo.AddAsync(mapping, false);
                        totalCost += takeQty * batch.Rate;
                        neededQty -= takeQty;
                    }

                    // Uncovered shortage has rate 0
                    tx.CostAmount = Math.Round(totalCost, 2);
                    tx.CostPrice = tx.QtyOut > 0 ? Math.Round(totalCost / tx.QtyOut, 4) : 0m;
                }
                else
                {
                    tx.CostAmount = 0m;
                    tx.CostPrice = 0m;
                }

                // Outward transaction decreases running balance
                runningQty -= tx.QtyOut;
                tx.RunningQtyBalance = runningQty;
                await txRepo.UpdateAsync(tx, false);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Clear FifoRebuildFromDate on ItemDetail now that FIFO and running balances are up to date
        await dbContext.Set<ItemDetail>()
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(i => i.FifoRebuildFromDate, (DateOnly?)null),
                cancellationToken);
    }

    public async Task<FifoRepairResult> RepairAndRebuildAllAsync(
        bool repairWandaAmounts,
        CancellationToken cancellationToken = default)
    {
        int updatedAmountCount = 0;

        if (repairWandaAmounts)
        {
            var wandaVTypes = new[] { "PU", "SL", "PR", "SR", "SA" };
            var candidateTxs = await _itemTransactionRepository.GetAll()
                .Where(x => wandaVTypes.Contains(x.VType)
                    && x.SecRate != null && x.SecRate != 0m
                    && ((x.SecQtyIn ?? 0m) != 0m || (x.SecQtyOut ?? 0m) != 0m))
                .ToListAsync(cancellationToken);

            if (candidateTxs.Count > 0)
            {
                var puVNos = candidateTxs.Where(x => x.VType == "PU").Select(x => x.VNo).Distinct().ToList();
                var slVNos = candidateTxs.Where(x => x.VType == "SL").Select(x => x.VNo).Distinct().ToList();
                var srVNos = candidateTxs.Where(x => x.VType == "SR").Select(x => x.VNo).Distinct().ToList();

                var puAddLessMap = puVNos.Count > 0
                    ? await _dbContext.Set<PurchaseDetail>()
                        .AsNoTracking()
                        .Where(d => d.VType == "PU" && puVNos.Contains(d.VNo))
                        .ToDictionaryAsync(d => (d.VNo, d.Seq), d => d.AddLess, cancellationToken)
                    : new Dictionary<(string VNo, int Seq), decimal>();

                var slDiscountMap = slVNos.Count > 0
                    ? await _dbContext.Set<Sale>()
                        .AsNoTracking()
                        .Where(s => s.VType == "SL" && slVNos.Contains(s.VNo))
                        .ToDictionaryAsync(s => (s.VNo, s.Seq), s => s.Discount, cancellationToken)
                    : new Dictionary<(string VNo, int Seq), decimal>();

                var srDiscountMap = srVNos.Count > 0
                    ? await _dbContext.Set<SaleRetDetail>()
                        .AsNoTracking()
                        .Where(d => d.VType == "SR" && srVNos.Contains(d.VNo))
                        .ToDictionaryAsync(d => (d.VNo, d.Seq), d => d.Discount ?? 0m, cancellationToken)
                    : new Dictionary<(string VNo, int Seq), decimal>();

                foreach (var tx in candidateTxs)
                {
                    decimal expectedAmount = tx.VType switch
                    {
                        "PU" => (tx.QtyIn * tx.Rate) + puAddLessMap.GetValueOrDefault((tx.VNo, tx.Seq), 0m),
                        "SL" => (tx.QtyOut * tx.Rate) - slDiscountMap.GetValueOrDefault((tx.VNo, tx.Seq), 0m),
                        "PR" => tx.QtyOut * tx.Rate,
                        "SR" => (tx.QtyIn * tx.Rate) - srDiscountMap.GetValueOrDefault((tx.VNo, tx.Seq), 0m),
                        "SA" => (tx.QtyIn - tx.QtyOut) * tx.Rate,
                        _ => tx.Amount
                    };

                    if (tx.Amount != expectedAmount)
                    {
                        tx.Amount = expectedAmount;
                        await _itemTransactionRepository.UpdateAsync(tx, false);
                        updatedAmountCount++;
                    }
                }

                if (updatedAmountCount > 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        var itemMinDates = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .GroupBy(x => x.ItemId!)
            .Select(g => new { ItemId = g.Key, MinDate = g.Min(x => x.VDate) })
            .ToListAsync(cancellationToken);

        foreach (var entry in itemMinDates)
        {
            await MarkDirtyAsync(entry.ItemId, entry.MinDate, cancellationToken);
        }

        if (_transactionManager.Transaction != null)
        {
            await _transactionManager.Transaction.CommitAsync(cancellationToken);
            await _transactionManager.Transaction.DisposeAsync();
            _transactionManager.Transaction = null;
        }

        if (itemMinDates.Count > 0)
        {
            var itemIds = itemMinDates.Select(x => x.ItemId).ToList();
            await FlushDirtyItemsAsync(itemIds, cancellationToken);
        }

        _logger.LogInformation(
            "Completed FIFO and amount repair: UpdatedAmountCount={UpdatedAmountCount}, RebuiltItemCount={RebuiltItemCount}",
            updatedAmountCount, itemMinDates.Count);

        return new FifoRepairResult(updatedAmountCount, itemMinDates.Count);
    }

    private async Task AcquireItemRowLockAsync(
        ApplicationDbContext dbContext,
        string itemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var provider = dbContext.Database.ProviderName;
            if (provider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM ItemDetail WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}",
                    new object[] { itemId },
                    cancellationToken);
            }
            else if (provider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM \"public\".\"ItemDetail\" WHERE id = {0} FOR UPDATE",
                    new object[] { itemId },
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not acquire row lock for Item {ItemId} during FIFO rebuild.", itemId);
        }
    }
}
