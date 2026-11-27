using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Legacy.Inventory;
using Retailer.Domain.Legacy;
using Retailer.Infrastructure.Persistence.Context;

namespace Retailer.Infrastructure.Persistence.Initialization;

/// <summary>
/// Idempotent startup data repair for:
/// 1. Wanda (HasVariablePackFeature) tenants where ItemTransaction.Amount previously double-counted
///    (Qty * Rate) + (SecQty * SecRate).
/// 2. Any tenant with same-day/prior-day unconsumed 'in' batches left behind while an 'out'
///    transaction on or after that date received CostPrice = 0 due to VTime ordering.
/// Does not modify any billing/voucher master-detail tables or GL entries.
/// </summary>
public class WandaTransactionRepairSeeder : ICustomSeeder
{
    private static readonly string[] WandaVTypes = ["PU", "SL", "PR", "SR", "SA"];

    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IFifoCostingService _fifoCostingService;
    private readonly ILogger<WandaTransactionRepairSeeder> _logger;

    public WandaTransactionRepairSeeder(
        ApplicationDbContext dbContext,
        ICurrentTenant currentTenant,
        IFifoCostingService fifoCostingService,
        ILogger<WandaTransactionRepairSeeder> logger)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _fifoCostingService = fifoCostingService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var itemsToRebuild = new Dictionary<string, DateOnly>(StringComparer.OrdinalIgnoreCase);
            int updatedAmountCount = 0;

            // 1. Repair double-counted ItemTransaction.Amount for Wanda tenants only
            if (_currentTenant.HasVariablePackFeature)
            {
                var candidateTxs = await _dbContext.Set<ItemTransaction>()
                    .Where(x => WandaVTypes.Contains(x.VType)
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
                            updatedAmountCount++;

                            if (!string.IsNullOrWhiteSpace(tx.ItemId))
                            {
                                TrackItemDate(itemsToRebuild, tx.ItemId, tx.VDate);
                            }
                        }
                    }

                    if (updatedAmountCount > 0)
                    {
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }

            // 2. Detect any items with zero-cost outward transactions that have unconsumed 'in' batches on or before that date
            var unallocatedItems = await _dbContext.Set<ItemTransaction>()
                .AsNoTracking()
                .Where(outTx => outTx.TranType == "out"
                    && outTx.QtyOut > 0
                    && !string.IsNullOrWhiteSpace(outTx.ItemId)
                    && (outTx.CostPrice == null || outTx.CostPrice == 0m)
                    && _dbContext.Set<ItemTransaction>().Any(inTx =>
                        inTx.ItemId == outTx.ItemId
                        && inTx.TranType == "in"
                        && inTx.RemainingQty > 0
                        && inTx.VDate <= outTx.VDate))
                .GroupBy(x => x.ItemId!)
                .Select(g => new { ItemId = g.Key, MinDate = g.Min(x => x.VDate) })
                .ToListAsync(cancellationToken);

            foreach (var entry in unallocatedItems)
            {
                TrackItemDate(itemsToRebuild, entry.ItemId, entry.MinDate);
            }

            if (itemsToRebuild.Count > 0)
            {
                foreach (var (itemId, minDate) in itemsToRebuild)
                {
                    await _fifoCostingService.MarkDirtyAsync(itemId, minDate, cancellationToken);
                }

                await _fifoCostingService.FlushDirtyItemsAsync(itemsToRebuild.Keys.ToList(), cancellationToken);

                _logger.LogInformation(
                    "WandaTransactionRepairSeeder completed for tenant '{Tenant}': repaired {UpdatedAmounts} transaction amounts and rebuilt FIFO for {RebuiltItems} item(s).",
                    _currentTenant.Id, updatedAmountCount, itemsToRebuild.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WandaTransactionRepairSeeder failed for tenant '{Tenant}'.", _currentTenant.Id);
        }
    }

    private static void TrackItemDate(Dictionary<string, DateOnly> map, string itemId, DateOnly date)
    {
        if (!map.TryGetValue(itemId, out var existing) || date < existing)
        {
            map[itemId] = date;
        }
    }
}
