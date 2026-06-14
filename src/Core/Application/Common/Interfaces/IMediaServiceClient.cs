using System;
using System.Threading;
using System.Threading.Tasks;

namespace Retailer.Application.Common.Interfaces;

public interface IMediaServiceClient
{
    Task<PresignedUploadUrlResponse?> GetUploadUrlAsync(string fileName, CancellationToken cancellationToken);
    Task<SasTokenResponse?> GetViewTokenAsync(string fileId, int expiryHours, CancellationToken cancellationToken);
}

public class PresignedUploadUrlResponse
{
    public string? FileId { get; set; }
    public string? UploadUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class SasTokenResponse
{
    public string? Token { get; set; }
    public string? ViewUrl { get; set; }
    public string? DownloadUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
