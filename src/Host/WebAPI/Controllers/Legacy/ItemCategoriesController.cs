using Retailer.Application.Legacy.ItemCategories;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class ItemCategoriesController : VersionNeutralApiController
{
    private readonly IItemCategoryService _itemCategoryService;

    public ItemCategoriesController(IItemCategoryService itemCategoryService)
    {
        _itemCategoryService = itemCategoryService;
    }

    [HttpGet]
    [OpenApiOperation("Get active item categories.", "")]
    public async Task<HttpResponseDto<List<ItemCategoryResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var itemCategories = await _itemCategoryService.GetActiveAsync(cancellationToken);
        return itemCategories.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create an item category.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(ItemCategoryCreateRequest request, CancellationToken cancellationToken)
    {
        await _itemCategoryService.CreateAsync(request, cancellationToken);
        return "Item category created.".ToInformationResponse("Item category created.");
    }

    [HttpPut("{code}")]
    [OpenApiOperation("Update an item category.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string code, ItemCategoryUpdateRequest request, CancellationToken cancellationToken)
    {
        await _itemCategoryService.UpdateAsync(code, request, cancellationToken);
        return "Item category updated.".ToInformationResponse("Item category updated.");
    }

    [HttpDelete("{code}")]
    [OpenApiOperation("Delete an item category.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string code, CancellationToken cancellationToken)
    {
        await _itemCategoryService.DeleteAsync(code, cancellationToken);
        return "Item category deleted.".ToInformationResponse("Item category deleted.");
    }
}
