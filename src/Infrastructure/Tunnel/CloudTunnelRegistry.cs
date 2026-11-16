using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Retailer.Infrastructure.Tunnel;

/// <summary>
/// Maintains active SignalR tunnel connections from on-premises stores and correlates
/// proxied HTTP requests with their responses.
/// </summary>
public sealed class CloudTunnelRegistry
{
    private readonly ConcurrentDictionary<string, string> _tenantConnections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _connectionTenants = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<TunneledHttpResponse>> _pendingRequests = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterStore(string tenantId, string connectionId)
    {
        _tenantConnections[tenantId] = connectionId;
        _connectionTenants[connectionId] = tenantId;
    }

    public void UnregisterConnection(string connectionId, out string? tenantId)
    {
        if (_connectionTenants.TryRemove(connectionId, out tenantId) && tenantId is not null)
        {
            _tenantConnections.TryRemove(new KeyValuePair<string, string>(tenantId, connectionId));
        }
    }

    public bool IsStoreConnected(string tenantId) =>
        _tenantConnections.ContainsKey(tenantId);

    public bool TryGetConnectionId(string tenantId, out string? connectionId) =>
        _tenantConnections.TryGetValue(tenantId, out connectionId);

    public async Task<TunneledHttpResponse> DispatchAsync(
        IHubContext<CloudRelayHub> hubContext,
        string tenantId,
        TunneledHttpRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!_tenantConnections.TryGetValue(tenantId, out var connectionId))
        {
            throw new InvalidOperationException($"On-premises store '{tenantId}' is currently offline.");
        }

        var tcs = new TaskCompletionSource<TunneledHttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[request.RequestId] = tcs;

        try
        {
            await hubContext.Clients.Client(connectionId)
                .SendAsync("ExecuteHttpRequest", request, cancellationToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            return await tcs.Task.WaitAsync(cts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(request.RequestId, out _);
        }
    }

    public void CompleteRequest(TunneledHttpResponse response)
    {
        if (_pendingRequests.TryRemove(response.RequestId, out var tcs))
        {
            tcs.TrySetResult(response);
        }
    }
}
