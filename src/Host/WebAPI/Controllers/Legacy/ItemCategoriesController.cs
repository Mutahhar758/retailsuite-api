using Retailer.Application.Legacy.ItemCategories;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class ItemCategoriesController : VersionNeutralApiController
{
    private readonly IItemCategoryService _itemCategoryService;

    public ItemCategoriesController(IItemCategoryService itemCategoryService)
    {
        _itemCategoryService = itemCategoryService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.ItemCategories)]
    [OpenApiOperation("Get active item categories.", "")]
    public async Task<HttpResponseDto<List<ItemCategoryResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var itemCategories = await _itemCategoryService.GetActiveAsync(cancellationToken);
        return itemCategories.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.ItemCategories)]
    [OpenApiOperation("Create an item category.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(ItemCategoryCreateRequest request, CancellationToken cancellationToken)
    {
        await _itemCategoryService.CreateAsync(request, cancellationToken);
        return "Item category created.".ToInformationResponse("Item category created.");
    }

    [HttpPut("{code}")]
    [MustHavePermission(AppAction.Update, AppResource.ItemCategories)]
    [OpenApiOperation("Update an item category.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string code, ItemCategoryUpdateRequest request, CancellationToken cancellationToken)
    {
        await _itemCategoryService.UpdateAsync(code, request, cancellationToken);
        return "Item category updated.".ToInformationResponse("Item category updated.");
    }

    [HttpDelete("{code}")]
    [MustHavePermission(AppAction.Delete, AppResource.ItemCategories)]
    [OpenApiOperation("Delete an item category.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string code, CancellationToken cancellationToken)
    {
        await _itemCategoryService.DeleteAsync(code, cancellationToken);
        return "Item category deleted.".ToInformationResponse("Item category deleted.");
    }

    [HttpPost("presigned-upload-url")]
    [MustHavePermission(new[] { AppAction.Create, AppAction.Update }, AppResource.ItemCategories)]
    [OpenApiOperation("Generate pre-signed upload URL for product category image.", "")]
    public async Task<HttpResponseDto<PresignedUploadUrlResponse?>> GetPresignedUploadUrlAsync([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        var response = await _itemCategoryService.GetPresignedUploadUrlAsync(fileName, cancellationToken);
        return response.ToInformationResponse();
    }
}
