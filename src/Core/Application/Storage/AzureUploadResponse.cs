using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retailer.Application.Storage;
public class AzureUploadResponse
{
    public AzureUploadResponse(string blobUrlWithSasToken, string blobPath, string fileName)
    {
        BlobUrlWithSasToken = blobUrlWithSasToken;
        BlobPath = blobPath;
        FileName = fileName;
    }

    public string BlobUrlWithSasToken { get; }
    public string BlobPath { get; }
    public string FileName { get; }
}
