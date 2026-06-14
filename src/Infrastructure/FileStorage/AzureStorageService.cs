using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Retailer.Application.Storage;
using Azure.Storage.Blobs.Models;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.FileStorage;

public class AzureStorageService : IAzureStorageService
{
    private readonly AzureStorageSettings _storageSettings;

    /// <summary>Azure blob reference/Path.</summary>
    private readonly string _azureBlobReference;

    public AzureStorageService(IOptions<AzureStorageSettings> settings)
    {
        _storageSettings = settings.Value;
        _azureBlobReference = _storageSettings.AzureBlobReference;
    }

    /// <summary>To Upload file/doc on Azure Blob.</summary>
    /// <param name="file">The local file to be uploade to Azure</param>
    /// <param name="partitionPath">The partition path of Azure Blob where file to be saved.</param>
    /// <param name="fileName">Document/File Name, to be saved in Blob.</param>
    public async Task<AzureUploadResponse> UploadAsync(Stream file, string partitionPath, string? fileName = null)
    {
        var container = GetAzureContinerRef();

        string blobPath = $"{_azureBlobReference}/{partitionPath}/{fileName}";
        var blob = container.GetBlobClient(blobPath);

        file.Position = 0;

        string contentType = DocumentConstants.GetContentType(Path.GetExtension(fileName).ToLower());

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blob.UploadAsync(file);

        // Create a new BlobSasBuilder instance. SAS (Shared Access Signature)
        BlobSasBuilder sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _storageSettings.AzureContainerReference,
            BlobName = blobPath,
            ExpiresOn = DateTimeOffset.MaxValue // Set the expiration time for the SAS token to the maximum value.
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read); // Set the permissions for the SAS token.

        // Generate a SAS token for the blob.
        string sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(_storageSettings.AccountName, _storageSettings.AccountKey)).ToString();

        // Use the SAS token to access the blob.
        string blobUrlWithSasToken = blob.Uri + "?" + sasToken;

        return new AzureUploadResponse(blobUrlWithSasToken, blobPath, fileName);
    }

    /// <summary>To download the file/doc from Azure Blob.</summary>
    /// <param name="path">The partition path of Azure Blob where file is saved.</param>
    /// <param name="localDownloadPath">The local download path where file is to be downloaded.</param>
    public async Task DownloadAsync(string path, string localDownloadPath)
    {
        var container = GetAzureContinerRef();

        var blob = container.GetBlobClient(path);
        await blob.DownloadToAsync(localDownloadPath);
    }

    public async Task<byte[]> DownloadAsStreamAsync(string path)
    {
        var container = GetAzureContinerRef();

        var blob = container.GetBlobClient(path);
        using (var memoryStream = new MemoryStream())
        {
            await blob.DownloadToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    private BlobContainerClient GetAzureContinerRef()
    {
        // Create the blob client.
        var blobServiceClient = new BlobServiceClient(_storageSettings.ConnectionString);

        // Retrieve reference to a previously created container.
        var container = blobServiceClient.GetBlobContainerClient(_storageSettings.AzureContainerReference);

        // Create container if not exists.
        container.CreateIfNotExists();

        return container;
    }
}