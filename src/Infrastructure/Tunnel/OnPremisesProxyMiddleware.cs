using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Tunnel;

/// <summary>
/// Cloud Reverse-Proxy Middleware (Section 5 of architecture plan):
/// When a request arrives at the Cloud API for an on-premises tenant (indicated by
/// <c>ConnectionString == "on-premises"</c> or header <c>X-Route-Via-Tunnel: true</c>),
/// forwards the HTTP request over the outbound SignalR tunnel in a single roundtrip.
/// </summary>
public sealed class OnPremisesProxyMiddleware : IMiddleware
{
    public const string LoopbackHeader = TunnelConstants.LoopbackHeader;
    private static readonly TimeSpan ProxyTimeout = TimeSpan.FromSeconds(45);
    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Transfer-Encoding", "TE", "Trailer", "Upgrade", "Host", "Content-Length",
    };

    private readonly CloudTunnelRegistry _registry;
    private readonly IHubContext<CloudRelayHub> _hubContext;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ILogger<OnPremisesProxyMiddleware> _logger;

    public OnPremisesProxyMiddleware(
        CloudTunnelRegistry registry,
        IHubContext<CloudRelayHub> hubContext,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ILogger<OnPremisesProxyMiddleware> logger)
    {
        _registry = registry;
        _hubContext = hubContext;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith(TunnelConstants.HubPath, StringComparison.OrdinalIgnoreCase)
            || context.Request.Headers.ContainsKey(LoopbackHeader))
        {
            await next(context);
            return;
        }

        var tenant = _tenantAccessor.MultiTenantContext?.TenantInfo;
        if (tenant is null)
        {
            await next(context);
            return;
        }

        var isOnPremTenant = string.Equals(tenant.ConnectionString, "on-premises", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Request.Headers["X-Route-Via-Tunnel"], "true", StringComparison.OrdinalIgnoreCase);

        if (!isOnPremTenant)
        {
            await next(context);
            return;
        }

        if (!_registry.IsStoreConnected(tenant.Identifier))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                Message = $"On-premises store '{tenant.Identifier}' is currently offline or unreachable.",
            });
            return;
        }

        var requestDto = await BuildTunnelRequestAsync(context);
        try
        {
            var responseDto = await _registry.DispatchAsync(
                _hubContext,
                tenant.Identifier,
                requestDto,
                ProxyTimeout,
                context.RequestAborted);

            context.Response.StatusCode = responseDto.StatusCode;
            foreach (var (key, value) in responseDto.Headers)
            {
                if (!HopByHopHeaders.Contains(key))
                {
                    context.Response.Headers[key] = value;
                }
            }

            if (!string.IsNullOrEmpty(responseDto.BodyBase64))
            {
                var bodyBytes = Convert.FromBase64String(responseDto.BodyBase64);
                await context.Response.Body.WriteAsync(bodyBytes, context.RequestAborted);
            }
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Tunnel request {RequestId} to store '{Tenant}' timed out.", requestDto.RequestId, tenant.Identifier);
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await context.Response.WriteAsJsonAsync(new { Message = $"On-premises store '{tenant.Identifier}' did not respond in time." });
        }
    }

    private static async Task<TunneledHttpRequest> BuildTunnelRequestAsync(HttpContext context)
    {
        string? bodyBase64 = null;
        if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms, context.RequestAborted);
            if (ms.Length > 0)
            {
                bodyBase64 = Convert.ToBase64String(ms.ToArray());
            }
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Request.Headers)
        {
            if (!HopByHopHeaders.Contains(header.Key))
            {
                headers[header.Key] = header.Value.ToString();
            }
        }

        return new TunneledHttpRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            Method = context.Request.Method,
            PathAndQuery = $"{context.Request.Path}{context.Request.QueryString}",
            Headers = headers,
            BodyBase64 = bodyBase64,
        };
    }
}
