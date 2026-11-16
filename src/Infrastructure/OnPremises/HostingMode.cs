using Microsoft.Extensions.Configuration;

namespace Retailer.Infrastructure.OnPremises;

/// <summary>
/// Resolves whether this process runs as the multi-tenant Cloud API or as a dedicated
/// single-store On-Premises server. The <c>OnPremises</c> build configuration
/// (<c>ON_PREMISES</c> symbol) is always on-premises; otherwise <c>HostingMode</c> config decides.
/// </summary>
public static class HostingMode
{
    public const string Cloud = "Cloud";
    public const string OnPremises = "OnPremises";

    public static bool IsOnPremises(IConfiguration configuration)
    {
#if ON_PREMISES
        return true;
#else
        return string.Equals(configuration["HostingMode"], OnPremises, StringComparison.OrdinalIgnoreCase);
#endif
    }
}
