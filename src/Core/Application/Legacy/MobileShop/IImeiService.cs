using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.MobileShop;

public interface IImeiService : ITransientService
{
    Task<List<ImeiStockResponse>> GetImeiStockAsync(CancellationToken cancellationToken);
    Task<ImeiHistoryResponse?> GetImeiHistoryAsync(string imei, CancellationToken cancellationToken);
    Task<long> AddImeiCostAsync(ImeiCostAdditionRequest request, CancellationToken cancellationToken);
}
