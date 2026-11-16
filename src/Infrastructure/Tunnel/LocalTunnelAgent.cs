using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Retailer.Infrastructure.OnPremises;

namespace Retailer.Infrastructure.Tunnel;

/// <summary>
/// Outbound Encrypted Tunnel Agent (Section 4 of architecture plan) running inside the on-premises Windows Service.
/// Implements the 3-Layer Reconnection Strategy:
/// 1. Custom <see cref="InfiniteRetryPolicy"/> on <see cref="HubConnectionBuilder"/>.
/// 2. Outer <see cref="BackgroundService"/> watchdog loop so boot-while-offline recovers automatically.
/// 3. Keep-Alive (15s) + periodic license heartbeat sync into <see cref="OnPremisesLicenseGuard"/>.
/// </summary>
public sealed class LocalTunnelAgent : BackgroundService
{
    private static readonly HttpClient LoopbackClient = new() { Timeout = TimeSpan.FromSeconds(40) };
    private readonly StoreSettings _store;
    private readonly OnPremisesLicenseGuard _licenseGuard;
    private readonly ILogger<LocalTunnelAgent> _logger;

    public LocalTunnelAgent(
        IOptions<StoreSettings> store,
        OnPremisesLicenseGuard licenseGuard,
        ILogger<LocalTunnelAgent> logger)
    {
        _store = store.Value;
        _licenseGuard = licenseGuard;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_store.CloudRelayUrl))
        {
            _logger.LogInformation("CloudRelayUrl is not configured; LocalTunnelAgent will remain idle (100% LAN mode).");
            return;
        }

        var hubUrl = _store.CloudRelayUrl.TrimEnd('/');
        if (!hubUrl.EndsWith(TunnelConstants.HubPath, StringComparison.OrdinalIgnoreCase))
        {
            hubUrl += TunnelConstants.HubPath;
        }

        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Headers[TunnelConstants.TenantHeader] = _store.TenantId;
                if (!string.IsNullOrWhiteSpace(_store.LicenseKey))
                {
                    options.Headers[TunnelConstants.LicenseHeader] = _store.LicenseKey;
                }
            })
            .WithAutomaticReconnect(new InfiniteRetryPolicy())
            .Build();

        connection.KeepAliveInterval = TimeSpan.FromSeconds(15);
        connection.ServerTimeout = TimeSpan.FromSeconds(30);

        connection.On<TunneledHttpRequest>("ExecuteHttpRequest", async request =>
        {
            var response = await ExecuteLocalHttpAsync(request, stoppingToken);
            try
            {
                await connection.InvokeAsync("DeliverHttpResponse", response, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deliver tunnel response for request {RequestId}.", request.RequestId);
            }
        });

        connection.Reconnected += async _ =>
        {
            _logger.LogInformation("Reconnected outbound tunnel to Cloud Hub ({HubUrl}).", hubUrl);
            await SendHeartbeatSafeAsync(connection, stoppingToken);
        };

        // Layer 2: Outer BackgroundService watchdog loop (covers initial boot while internet is down + periodic heartbeats)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (connection.State == HubConnectionState.Disconnected)
                {
                    await connection.StartAsync(stoppingToken);
                    _logger.LogInformation("Connected outbound tunnel to Cloud Hub ({HubUrl}) for store '{TenantId}'.", hubUrl, _store.TenantId);
                }

                if (connection.State == HubConnectionState.Connected)
                {
                    await SendHeartbeatSafeAsync(connection, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cloud tunnel unreachable (store is operating offline on LAN); will retry in 15s.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task SendHeartbeatSafeAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var hb = await connection.InvokeAsync<LicenseHeartbeatResponse>("Heartbeat", cancellationToken);
            if (hb is not null)
            {
                _licenseGuard.RecordCloudHeartbeat(hb.IsActive, hb.ValidUntilUtc, hb.ServerUtc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Heartbeat check failed.");
        }
    }

    private async Task<TunneledHttpResponse> ExecuteLocalHttpAsync(TunneledHttpRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var baseUri = _store.LocalLoopbackUrl.TrimEnd('/');
            using var httpReq = new HttpRequestMessage(new HttpMethod(req.Method), $"{baseUri}{req.PathAndQuery}");
            httpReq.Headers.TryAddWithoutValidation(TunnelConstants.LoopbackHeader, "true");

            if (!string.IsNullOrEmpty(req.BodyBase64))
            {
                var bytes = Convert.FromBase64String(req.BodyBase64);
                httpReq.Content = new ByteArrayContent(bytes);
                if (req.Headers.TryGetValue("Content-Type", out var contentType))
                {
                    httpReq.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                }
            }

            foreach (var (key, value) in req.Headers)
            {
                if (!string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    httpReq.Headers.TryAddWithoutValidation(key, value);
                }
            }

            using var httpResp = await LoopbackClient.SendAsync(httpReq, cancellationToken);
            var respBytes = await httpResp.Content.ReadAsByteArrayAsync(cancellationToken);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in httpResp.Headers)
            {
                headers[h.Key] = string.Join(", ", h.Value);
            }

            foreach (var h in httpResp.Content.Headers)
            {
                headers[h.Key] = string.Join(", ", h.Value);
            }

            return new TunneledHttpResponse
            {
                RequestId = req.RequestId,
                StatusCode = (int)httpResp.StatusCode,
                Headers = headers,
                BodyBase64 = respBytes.Length > 0 ? Convert.ToBase64String(respBytes) : null,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tunneled request {Method} {Path}.", req.Method, req.PathAndQuery);
            return new TunneledHttpResponse
            {
                RequestId = req.RequestId,
                StatusCode = 502,
            };
        }
    }

    /// <summary>
    /// Layer 1: Infinite retry policy (0s, 2s, 5s, 10s, then every 30s + jitter forever).
    /// </summary>
    private sealed class InfiniteRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
            retryContext.PreviousRetryCount switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(2),
                2 => TimeSpan.FromSeconds(5),
                3 => TimeSpan.FromSeconds(10),
                _ => TimeSpan.FromSeconds(30 + Random.Shared.Next(0, 5)),
            };
    }
}
