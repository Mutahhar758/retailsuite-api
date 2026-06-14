namespace Retailer.Application.Legacy.SupplyOrders;

public interface ISupplyOrderService : ITransientService
{
    Task<List<SupplyOrderResponse>> GetAsync(CancellationToken cancellationToken);
    Task<SupplyOrderResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> CreateAsync(SupplyOrderUpsertRequest request, CancellationToken cancellationToken);
    Task<int> UpdateAsync(int id, SupplyOrderUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
