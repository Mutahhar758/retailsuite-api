namespace Retailer.Infrastructure.Tunnel;

public static class TunnelConstants
{
    public const string HubPath = "/tunnel-hub";
    public const string TenantHeader = "X-Store-Tenant-Id";
    public const string LicenseHeader = "X-Store-License-Key";
    public const string LoopbackHeader = "X-Tunneled-From-Cloud";
}

public sealed class TunneledHttpRequest
{
    public string RequestId { get; set; } = string.Empty;

    public string Method { get; set; } = "GET";

    public string PathAndQuery { get; set; } = "/";

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? BodyBase64 { get; set; }
}

public sealed class TunneledHttpResponse
{
    public string RequestId { get; set; } = string.Empty;

    public int StatusCode { get; set; } = 200;

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? BodyBase64 { get; set; }
}

public sealed class LicenseHeartbeatResponse
{
    public bool IsActive { get; set; }

    public DateTime? ValidUntilUtc { get; set; }

    public DateTime ServerUtc { get; set; }
}
