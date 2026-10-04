using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Brands;

public interface IBrandService : ITransientService
{
    Task<List<BrandResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<List<BrandResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task<BrandResponse?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<string> CreateAsync(BrandCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string id, BrandUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
