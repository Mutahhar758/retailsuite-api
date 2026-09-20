using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Legacy;

namespace Retailer.Application.Legacy.Inventory;

public record FifoAllocationResult(
    decimal CostPrice,
    decimal CostAmount,
    decimal ShortageQty,
    List<TransactionFifoMapping> Mappings);

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
}
