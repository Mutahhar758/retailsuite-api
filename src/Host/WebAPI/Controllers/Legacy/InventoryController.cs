using Retailer.Application.Legacy.Inventory;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class InventoryController : VersionNeutralApiController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("items/presigned-upload-url")]
    [MustHavePermission(new[] { AppAction.Create, AppAction.Update }, AppResource.InventoryItems)]
    [OpenApiOperation("Generate pre-signed upload URL for product media.", "")]
    public async Task<HttpResponseDto<PresignedUploadUrlResponse?>> GetPresignedUploadUrlAsync([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        var response = await _inventoryService.GetPresignedUploadUrlAsync(fileName, cancellationToken);
        return response.ToInformationResponse();
    }

    [HttpGet("items")]
    [MustHavePermission(AppAction.View, AppResource.InventoryItems)]
    [OpenApiOperation("Get inventory items.", "")]
    public async Task<HttpResponseDto<List<InventoryItemResponse>>> GetItemsAsync([FromQuery] string? itemCategoryCode, CancellationToken cancellationToken)
    {
        var items = await _inventoryService.GetItemsAsync(itemCategoryCode, cancellationToken);
        return items.ToInformationResponse();
    }

    [HttpGet("items/{id}")]
    [MustHavePermission(AppAction.View, AppResource.InventoryItems)]
    [OpenApiOperation("Get inventory item by id.", "")]
    public async Task<HttpResponseDto<InventoryItemResponse?>> GetItemAsync(string id, CancellationToken cancellationToken)
    {
        var item = await _inventoryService.GetItemAsync(id, cancellationToken);
        return item.ToInformationResponse();
    }

    [HttpPost("items")]
    [MustHavePermission(AppAction.Create, AppResource.InventoryItems)]
    [OpenApiOperation("Create inventory item.", "")]
    public async Task<HttpResponseDto<string>> CreateItemAsync(InventoryItemUpsertRequest request, CancellationToken cancellationToken)
    {
        var id = await _inventoryService.UpsertItemAsync(request, cancellationToken);
        return id.ToInformationResponse("Inventory item saved.");
    }

    [HttpPut("items/{id}")]
    [MustHavePermission(AppAction.Update, AppResource.InventoryItems)]
    [OpenApiOperation("Update inventory item.", "")]
    public async Task<HttpResponseDto<string>> UpdateItemAsync(string id, InventoryItemUpsertRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        var itemId = await _inventoryService.UpsertItemAsync(request, cancellationToken);
        return itemId.ToInformationResponse("Inventory item saved.");
    }

    [HttpDelete("items/{id}")]
    [MustHavePermission(AppAction.Delete, AppResource.InventoryItems)]
    [OpenApiOperation("Delete inventory item.", "")]
    public async Task<HttpResponseDto<string>> DeleteItemAsync(string id, CancellationToken cancellationToken)
    {
        await _inventoryService.DeleteItemAsync(id, cancellationToken);
        return "Inventory item deleted.".ToInformationResponse("Inventory item deleted.");
    }
}
