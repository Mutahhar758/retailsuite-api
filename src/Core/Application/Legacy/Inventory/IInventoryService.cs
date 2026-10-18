using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Inventory;

public interface IInventoryService : ITransientService
{
    Task<List<InventoryItemResponse>> GetItemsAsync(string? itemCategoryCode, CancellationToken cancellationToken);
    Task<List<InventoryItemLookupResponse>> GetItemsLookupAsync(string? itemCategoryCode, CancellationToken cancellationToken);
    Task<InventoryItemResponse?> GetItemAsync(string id, CancellationToken cancellationToken);
    Task<string> UpsertItemAsync(InventoryItemUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteItemAsync(string id, CancellationToken cancellationToken);
    Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken);
    Task<BulkOperationResult> BulkUpsertAsync(InventoryItemBulkUpsertRequest request, CancellationToken cancellationToken);
    Task<List<ItemCustomerCategoryPriceDto>> GetCustomerCategoryPricesAsync(string? customerCategoryId, CancellationToken cancellationToken);
    Task<List<ItemCustomerCategoryPriceDto>> GetItemCategoryPricesAsync(string itemId, CancellationToken cancellationToken);
    Task SaveItemCategoryPricesAsync(string itemId, List<ItemCustomerCategoryPriceUpsertRequest> prices, CancellationToken cancellationToken);
}
