using Finbuckle.MultiTenant.Abstractions;

namespace Retailer.Domain.Multitenancy;

public class TenantInfo : ITenantInfo
{
    public string Id { get; set; } = default!;

    public string Identifier { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string? ConnectionString { get; set; }

    public string? DbProvider { get; set; }

    public string? AdminEmail { get; set; }

    public string? LicenseKey { get; set; }

    public bool IsActive { get; set; } = true;
    public bool HasSupplyFeature { get; set; } = true;
    public bool HasSecondaryQty { get; set; } = false;
    public bool HasKotFeature { get; set; } = false;

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

    public DateTime? ValidUntil { get; set; }
}
