using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Legacy;

namespace Retailer.Application.Legacy.Inventory;

public record FifoAllocationResult(
    decimal CostPrice,
    decimal CostAmount,
    decimal ShortageQty,
    List<TransactionFifoMapping> Mappings);

public record FifoRepairResult(
    int UpdatedAmountCount,
    int RebuiltItemCount);

public interface IFifoCostingService : ITransientService
{
    Task<FifoAllocationResult> AllocateFifoCostAsync(
        ItemTransaction outTx,
        CancellationToken cancellationToken = default);

    Task RebuildItemFifoAsync(
        string tenantId,
        string itemId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default);

    Task MarkDirtyAsync(
        string itemId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default);

    Task MarkDirtyAsync(
        IEnumerable<string> itemIds,
        DateOnly fromDate,
        CancellationToken cancellationToken = default);

    Task FlushDirtyItemsAsync(
        IReadOnlyCollection<string>? itemIds = null,
        CancellationToken cancellationToken = default);

    Task FlushAllTenantsAsync(
        CancellationToken cancellationToken = default);

    Task<FifoRepairResult> RepairAndRebuildAllAsync(
        bool repairWandaAmounts,
        CancellationToken cancellationToken = default);
}
