using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Common.Enums;

namespace Retailer.Application.Legacy.Manufacturing;

public interface IManufacturingService : ITransientService
{
    Task<List<ProductionResponse>> GetListAsync(ProductionListFilter filter, CancellationToken cancellationToken);
    Task<ProductionResponse> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<ProductionEstimateResponse> CalculateEstimateAsync(ProductionEstimateRequest request, CancellationToken cancellationToken);
    Task<string> CreateAsync(ProductionCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, ProductionUpdateRequest request, CancellationToken cancellationToken);
    Task UpdateStatusAsync(string voucherNo, ProductionStatus newStatus, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
}
