using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Inventory;

public interface IInventoryService : ITransientService
{
    Task<List<InventoryItemResponse>> GetItemsAsync(string? itemCategoryCode, CancellationToken cancellationToken);
    Task<InventoryItemResponse?> GetItemAsync(string id, CancellationToken cancellationToken);
    Task<string> UpsertItemAsync(InventoryItemUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteItemAsync(string id, CancellationToken cancellationToken);
    Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken);
}
