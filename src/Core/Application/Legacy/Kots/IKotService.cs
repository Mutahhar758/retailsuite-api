using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Kots;

public interface IKotService : ITransientService
{
    // KOT Orders
    Task<KotOrderResponse> CreateAsync(KotOrderCreateRequest request, CancellationToken cancellationToken);
    Task<List<KotOrderResponse>> GetActiveListAsync(string? prepStationId, CancellationToken cancellationToken);
    Task<KotOrderResponse?> GetByTokenOrIdAsync(string query, CancellationToken cancellationToken);
    Task UpdateItemStatusAsync(int orderId, int itemId, string status, CancellationToken cancellationToken);
    Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken);
    Task FinalizePaymentAsync(int orderId, string saleVoucherNo, CancellationToken cancellationToken);
}
