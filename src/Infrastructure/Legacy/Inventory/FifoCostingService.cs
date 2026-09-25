using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Inventory;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Multitenancy;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Infrastructure.Persistence.Transactions;
using Retailer.Shared.Common.Constants;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Legacy.Inventory;

public class FifoCostingService : IFifoCostingService
{
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<TransactionFifoMapping> _fifoMappingRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FifoCostingService> _logger;

    public FifoCostingService(
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<TransactionFifoMapping> fifoMappingRepository,
        IServiceProvider serviceProvider,
        ILogger<FifoCostingService> logger)
    {
        _itemTransactionRepository = itemTransactionRepository;
        _fifoMappingRepository = fifoMappingRepository;
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

        // Fetch available "in" batches for this item ordered chronologically
        var availableBatches = await _itemTransactionRepository.GetAll()
            .Where(x => x.ItemId == outTx.ItemId && x.TranType == "in" && x.RemainingQty > 0)
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

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var txRepo = scope.ServiceProvider.GetRequiredService<IRepository<ItemTransaction>>();
        var mapRepo = scope.ServiceProvider.GetRequiredService<IRepository<TransactionFifoMapping>>();
        var txManager = scope.ServiceProvider.GetRequiredService<EfTransactionManager>();

        try
        {
            // 2. Delete mappings affected from fromDate forward
            var staleMappings = await mapRepo.GetAll()
                .Where(m => m.OutTransaction.ItemId == itemId && m.OutTransaction.VDate >= fromDate)
                .ToListAsync(cancellationToken);

            if (staleMappings.Count > 0)
            {
                await mapRepo.DeleteRangeAsync(staleMappings, false);
            }

            // 3. Reset batches
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

            // 4. Nullify sales costs from fromDate forward
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

            // 5. Chronological sequential replay of all transactions from fromDate forward
            // Purchases ('in') ordered before sales ('out') on the same timestamp
            var replayTransactions = await txRepo.GetAll()
                .Where(x => x.ItemId == itemId && x.VDate >= fromDate)
                .OrderBy(x => x.VDate)
                .ThenBy(x => x.VTime)
                .ThenBy(x => x.TranType == "in" ? 0 : 1)
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
                .ThenByDescending(x => x.VTime)
                .ThenByDescending(x => x.TranType == "in" ? 0 : 1)
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

                        // Only consume from batches received on or before this sale date/time
                        var eligibleBatches = activePool
                            .Where(b => b.RemainingQty > 0 && (b.VDate < tx.VDate || (b.VDate == tx.VDate && b.VTime <= tx.VTime)))
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

            if (txManager.Transaction != null)
            {
                await txManager.Transaction.CommitAsync(cancellationToken);
                await txManager.Transaction.DisposeAsync();
                txManager.Transaction = null;
            }

            _logger.LogInformation(
                "Completed FIFO rebuild for Tenant {TenantId}, Item {ItemId}, from Date {FromDate}",
                tenantId, itemId, fromDate);
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
}
