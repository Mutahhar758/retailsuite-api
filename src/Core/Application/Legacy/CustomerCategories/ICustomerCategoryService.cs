using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.CustomerCategories;

public interface ICustomerCategoryService : ITransientService
{
    Task<List<CustomerCategoryResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task<List<CustomerCategoryLookupResponse>> GetLookupAsync(CancellationToken cancellationToken);
    Task<string> CreateAsync(CustomerCategoryCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string code, CustomerCategoryUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string code, CancellationToken cancellationToken);
}
