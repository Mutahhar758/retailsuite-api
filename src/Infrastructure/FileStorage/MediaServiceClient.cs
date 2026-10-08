using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.FileStorage;

public class MediaServiceClient : IMediaServiceClient, ITransientService
{
    private readonly HttpClient _httpClient;
    private readonly MediaServiceSettings _settings;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<MediaServiceClient> _logger;

    public MediaServiceClient(
        HttpClient httpClient,
        IOptions<MediaServiceSettings> settings,
        ICurrentTenant currentTenant,
        ILogger<MediaServiceClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<PresignedUploadUrlResponse?> GetUploadUrlAsync(string fileName, string subFolder, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _logger.LogError("Media service BaseUrl is not configured in settings.");
            throw new InternalServerException("Media storage service is temporarily unavailable. Please report to Bizgrip Solutions.");
        }

        try
        {
            var tenantId = _currentTenant.Id ?? "default";
            var pathPrefixedFileName = $"RetailSuite/{tenantId}/{subFolder.Trim('/')}/{fileName.TrimStart('/')}";

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/api/Files/upload-url")
            {
                Content = JsonContent.Create(new { fileName = pathPrefixedFileName })
            };
            request.Headers.Add("X-Admin-Key", _settings.AdminKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Media service returned HTTP {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                throw new InternalServerException("The storage service is temporarily unavailable. Please report to Bizgrip Solutions.");
            }

            var result = await response.Content.ReadFromJsonAsync<MediaServiceApiResponse<PresignedUploadUrlResponse>>(cancellationToken: cancellationToken);
            if (result == null || !result.Success || result.Data == null)
            {
                var errorMsg = result?.Message ?? result?.Error ?? "Unknown error from media service.";
                _logger.LogError("Media service returned failure response: {ErrorMessage}", errorMsg);
                throw new InternalServerException("Unable to prepare media upload. Please report to Bizgrip Solutions.");
            }

            return result.Data;
        }
        catch (CustomException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to media service at {BaseUrl}", _settings.BaseUrl);
            throw new InternalServerException("Unable to reach the storage server. Please report to Bizgrip Solutions.");
        }
    }

    public async Task<SasTokenResponse?> GetViewTokenAsync(string fileId, int expiryHours, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.BaseUrl)) return null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/api/Files/token")
            {
                Content = JsonContent.Create(new { fileId = fileId, expiryHours = expiryHours, permissions = 1 }) // 1 = Read
            };
            request.Headers.Add("X-Admin-Key", _settings.AdminKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Media service token request for {FileId} returned HTTP {StatusCode}", fileId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MediaServiceApiResponse<SasTokenResponse>>(cancellationToken: cancellationToken);
            return result?.Success == true ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve view token for media ID {FileId}", fileId);
            return null;
        }
    }
}

public class MediaServiceApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

public class MediaServiceSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AdminKey { get; set; } = string.Empty;
}
