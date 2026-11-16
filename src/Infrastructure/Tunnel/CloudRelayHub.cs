using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Retailer.Infrastructure.Multitenancy;

namespace Retailer.Infrastructure.Tunnel;

/// <summary>
/// Cloud SignalR Hub (/tunnel-hub) that accepts outbound encrypted connections from on-premises store servers,
/// verifies their store license key, and relays remote HTTP calls and license heartbeats.
/// </summary>
[AllowAnonymous]
public sealed class CloudRelayHub : Hub
{
    public const string HubPath = TunnelConstants.HubPath;
    public const string TenantHeader = TunnelConstants.TenantHeader;
    public const string LicenseHeader = TunnelConstants.LicenseHeader;

    private readonly CloudTunnelRegistry _registry;
    private readonly ITenantLookup _tenantLookup;
    private readonly ILogger<CloudRelayHub> _logger;

    public CloudRelayHub(
        CloudTunnelRegistry registry,
        ITenantLookup tenantLookup,
        ILogger<CloudRelayHub> logger)
    {
        _registry = registry;
        _tenantLookup = tenantLookup;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var tenantId = httpContext?.Request.Headers[TenantHeader].ToString();
        var licenseKey = httpContext?.Request.Headers[LicenseHeader].ToString();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("Rejected tunnel connection {ConnectionId}: missing {Header}.", Context.ConnectionId, TenantHeader);
            Context.Abort();
            return;
        }

        var tenant = await _tenantLookup.FindAsync(tenantId, Context.ConnectionAborted);
        if (tenant is null)
        {
            _logger.LogWarning("Rejected tunnel connection for unknown tenant '{TenantId}'.", tenantId);
            Context.Abort();
            return;
        }

        if (!string.IsNullOrWhiteSpace(tenant.LicenseKey)
            && !string.Equals(tenant.LicenseKey, licenseKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Rejected tunnel connection for tenant '{TenantId}': invalid license key.", tenantId);
            Context.Abort();
            return;
        }

        _registry.RegisterStore(tenant.Identifier, Context.ConnectionId);
        _logger.LogInformation("On-premises store '{TenantId}' connected to tunnel hub (ConnectionId={ConnectionId}).", tenant.Identifier, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _registry.UnregisterConnection(Context.ConnectionId, out var tenantId);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogInformation("On-premises store '{TenantId}' disconnected from tunnel hub.", tenantId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public Task DeliverHttpResponse(TunneledHttpResponse response)
    {
        _registry.CompleteRequest(response);
        return Task.CompletedTask;
    }

    public async Task<LicenseHeartbeatResponse> Heartbeat()
    {
        var httpContext = Context.GetHttpContext();
        var tenantId = httpContext?.Request.Headers[TenantHeader].ToString() ?? string.Empty;
        var tenant = await _tenantLookup.FindAsync(tenantId, Context.ConnectionAborted);

        return new LicenseHeartbeatResponse
        {
            IsActive = tenant is { IsActive: true },
            ValidUntilUtc = tenant?.ValidUntil?.ToUniversalTime(),
            ServerUtc = DateTime.UtcNow,
        };
    }
}
