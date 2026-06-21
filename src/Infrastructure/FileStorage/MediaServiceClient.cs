using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.FileStorage;

public class MediaServiceClient : IMediaServiceClient, ITransientService
{
    private readonly HttpClient _httpClient;
    private readonly MediaServiceSettings _settings;
    private readonly ICurrentTenant _currentTenant;

    public MediaServiceClient(HttpClient httpClient, IOptions<MediaServiceSettings> settings, ICurrentTenant currentTenant)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _currentTenant = currentTenant;
    }

    public async Task<PresignedUploadUrlResponse?> GetUploadUrlAsync(string fileName, string subFolder, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.BaseUrl)) return null;

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
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<MediaServiceApiResponse<PresignedUploadUrlResponse>>(cancellationToken: cancellationToken);
            return result?.Success == true ? result.Data : null;
        }
        catch
        {
            return null;
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
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<MediaServiceApiResponse<SasTokenResponse>>(cancellationToken: cancellationToken);
            return result?.Success == true ? result.Data : null;
        }
        catch
        {
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
