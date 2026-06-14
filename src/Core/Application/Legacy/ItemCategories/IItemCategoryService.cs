namespace Retailer.Application.Legacy.ItemCategories;

public interface IItemCategoryService : ITransientService
{
    Task<List<ItemCategoryResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task CreateAsync(ItemCategoryCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string code, ItemCategoryUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string code, CancellationToken cancellationToken);
}
