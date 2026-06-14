namespace Retailer.Infrastructure.FileStorage;

public class AzureStorageSettings
{
    public string DocumentAllowedExtension { get; set; } = default!;

    public string ConnectionString { get; set; } = default!;

    public string AzurePrivateContainerReference { get; set; } = default!;

    public string AzureContainerReference { get; set; } = default!;

    public string AzureBlobReference { get; set; } = default!;

    public string AccountName { get; set; } = default!;

    public string AccountKey { get; set; } = default!;

    public long FileSizeLimit { get; set; }
}