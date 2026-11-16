using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.Auth.Jwt;

namespace Retailer.Infrastructure.FileStorage;

/// <summary>
/// On-premises implementation of <see cref="IMediaServiceClient"/> that stores and serves media files
/// directly from the local store server (<c>C:\ProgramData\RetailSuite\media</c>) using HMAC-signed
/// upload and view URLs. Requires zero cloud media service connectivity or credentials.
/// </summary>
public sealed class LocalMediaServiceClient : IMediaServiceClient
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentTenant _currentTenant;
    private readonly JwtSettings _jwtSettings;

    public LocalMediaServiceClient(
        IHttpContextAccessor httpContextAccessor,
        ICurrentTenant currentTenant,
        IOptions<JwtSettings> jwtSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentTenant = currentTenant;
        _jwtSettings = jwtSettings.Value;
    }

    public static string RootMediaDirectory
    {
        get
        {
            var baseDir = Environment.GetEnvironmentVariable("RETAILSUITE_DATA_DIR");
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "RetailSuite");
            }

            return Path.Combine(baseDir, "media");
        }
    }

    public Task<PresignedUploadUrlResponse?> GetUploadUrlAsync(
        string fileName,
        string subFolder,
        CancellationToken cancellationToken)
    {
        var tenantId = SanitizeSegment(_currentTenant.Id ?? "store");
        var cleanSubFolder = SanitizeSegment(subFolder);
        var cleanFileName = Path.GetFileName(fileName);
        var uniqueName = $"{Guid.NewGuid():N}_{cleanFileName}";
        var fileId = $"{tenantId}/{cleanSubFolder}/{uniqueName}";

        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var expUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        var sig = ComputeSignature("upload", fileId, expUnix, _jwtSettings.Key);
        var baseUrl = GetRequestBaseUrl();

        var uploadUrl = $"{baseUrl}/api/local-media/upload?fileId={Uri.EscapeDataString(fileId)}&exp={expUnix}&sig={Uri.EscapeDataString(sig)}";

        return Task.FromResult<PresignedUploadUrlResponse?>(new PresignedUploadUrlResponse
        {
            FileId = fileId,
            UploadUrl = uploadUrl,
            ExpiresAt = expiresAt,
        });
    }

    public Task<SasTokenResponse?> GetViewTokenAsync(
        string fileId,
        int expiryHours,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return Task.FromResult<SasTokenResponse?>(null);
        }

        var expiresAt = DateTime.UtcNow.AddHours(expiryHours > 0 ? expiryHours : 24);
        var expUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        var sig = ComputeSignature("view", fileId, expUnix, _jwtSettings.Key);
        var baseUrl = GetRequestBaseUrl();

        var viewUrl = $"{baseUrl}/api/local-media/view?fileId={Uri.EscapeDataString(fileId)}&exp={expUnix}&sig={Uri.EscapeDataString(sig)}";

        return Task.FromResult<SasTokenResponse?>(new SasTokenResponse
        {
            Token = sig,
            ViewUrl = viewUrl,
            DownloadUrl = viewUrl,
            ExpiresAt = expiresAt,
        });
    }

    public static bool VerifySignature(string mode, string fileId, long expUnix, string signature, string secretKey)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
        {
            return false;
        }

        var expected = ComputeSignature(mode, fileId, expUnix, secretKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature ?? string.Empty));
    }

    public static string ResolveSafeFilePath(string fileId)
    {
        var root = Path.GetFullPath(RootMediaDirectory);
        var relative = fileId.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relative));

        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid media file path.");
        }

        return fullPath;
    }

    private static string ComputeSignature(string mode, string fileId, long expUnix, string secretKey)
    {
        var payload = $"{mode}:{fileId}:{expUnix}";
        var keyBytes = Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(secretKey) ? "RetailSuite.LocalMedia.Fallback" : secretKey);
        var hash = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private static string SanitizeSegment(string value)
    {
        var cleaned = new string((value ?? "default").Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "default" : cleaned;
    }

    private string GetRequestBaseUrl()
    {
        var req = _httpContextAccessor.HttpContext?.Request;
        if (req is null)
        {
            return "http://127.0.0.1:5050";
        }

        var scheme = req.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? req.Scheme;
        var host = req.Headers["X-Forwarded-Host"].FirstOrDefault() ?? req.Host.Value;
        return $"{scheme}://{host}";
    }
}
