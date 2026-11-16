using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Retailer.Infrastructure.OnPremises;

/// <summary>
/// Enforces the 3-Layer Offline Window &amp; Anti-Clock-Rollback Guard (Section 7 of architecture plan):
/// 1. Hardware Tick Counter (<see cref="Environment.TickCount64"/>): immune to OS clock changes.
/// 2. Monotonic High-Water Mark (<c>LastKnownUtc</c>): detects backward clock tampering immediately.
/// 3. Cloud-Issued Lease Status &amp; Expiry (<c>IsActive</c> / <c>LeaseExpiresUtc</c>).
/// </summary>
public sealed class OnPremisesLicenseGuard
{
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(5);
    private readonly StoreSettings _storeSettings;
    private readonly ILogger<OnPremisesLicenseGuard> _logger;
    private readonly object _sync = new();

    private long _lastSyncTickMs;
    private DateTime _lastKnownUtc;
    private DateTime? _leaseExpiresUtc;
    private bool _isActive = true;
    private bool _clockTampered;
    private string? _lockReason;

    public OnPremisesLicenseGuard(
        IOptions<StoreSettings> storeSettings,
        ILogger<OnPremisesLicenseGuard> logger)
    {
        _storeSettings = storeSettings.Value;
        _logger = logger;
        _lastSyncTickMs = Environment.TickCount64;
        _lastKnownUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Called by the tunnel agent whenever a heartbeat response arrives from the Cloud Hub.
    /// </summary>
    public void RecordCloudHeartbeat(bool isActive, DateTime? validUntilUtc, DateTime serverUtc)
    {
        lock (_sync)
        {
            _lastSyncTickMs = Environment.TickCount64;
            if (serverUtc > _lastKnownUtc)
            {
                _lastKnownUtc = serverUtc;
            }

            _isActive = isActive;
            _leaseExpiresUtc = validUntilUtc;
            _clockTampered = false;
            _lockReason = isActive ? null : "Subscription deactivated by cloud administrator.";
        }
    }

    /// <summary>
    /// Validates the 3-layer offline lease before allowing local API requests.
    /// Soft lock allows read-only GET requests while blocking write operations (POST/PUT/DELETE) on expiry.
    /// </summary>
    public (bool Allowed, string? Reason) EvaluateRequest(string httpMethod)
    {
        lock (_sync)
        {
            var nowUtc = DateTime.UtcNow;

            // Layer 2: Monotonic High-Water Mark (Clock Rollback Trap)
            if (nowUtc + ClockSkewTolerance < _lastKnownUtc)
            {
                _clockTampered = true;
                _lockReason = "System clock rollback detected. Please restore accurate system date/time or reconnect to the cloud.";
                _logger.LogWarning("On-premises clock rollback detected: UtcNow={NowUtc:O} < LastKnownUtc={LastKnownUtc:O}", nowUtc, _lastKnownUtc);
                return (false, _lockReason);
            }

            if (nowUtc > _lastKnownUtc)
            {
                _lastKnownUtc = nowUtc;
            }

            if (_clockTampered)
            {
                return (false, _lockReason);
            }

            // Hard deactivation from Cloud Admin
            if (!_isActive)
            {
                return (false, _lockReason ?? "Subscription is inactive.");
            }

            var isWriteOperation = !string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(httpMethod, "HEAD", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(httpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase);

            // Layer 1: Hardware Tick Counter (Environment.TickCount64) when CloudRelayUrl is configured
            if (!string.IsNullOrWhiteSpace(_storeSettings.CloudRelayUrl) && _storeSettings.MaxOfflineHours > 0)
            {
                var elapsedOfflineMs = Environment.TickCount64 - _lastSyncTickMs;
                var maxOfflineMs = TimeSpan.FromHours(_storeSettings.MaxOfflineHours).TotalMilliseconds;
                if (elapsedOfflineMs > maxOfflineMs && isWriteOperation)
                {
                    return (false, $"Offline operating window ({_storeSettings.MaxOfflineHours} hours) exceeded. Please connect to the internet to renew license lease.");
                }
            }

            // Layer 3: Cloud-Issued Lease Expiry Boundary
            var effectiveUtc = nowUtc > _lastKnownUtc ? nowUtc : _lastKnownUtc;
            if (_leaseExpiresUtc.HasValue && effectiveUtc > _leaseExpiresUtc.Value && isWriteOperation)
            {
                return (false, "Store subscription lease has expired. Please renew your subscription.");
            }

            return (true, null);
        }
    }
}
