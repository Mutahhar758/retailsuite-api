namespace Retailer.Application.Storage;

public interface IAzureStorageService : IScopedService
{
    Task<AzureUploadResponse> UploadAsync(Stream file, string partitionPath, string? fileName = null);

    Task DownloadAsync(string path, string localDownloadPath);

    Task<byte[]> DownloadAsStreamAsync(string path);
}
