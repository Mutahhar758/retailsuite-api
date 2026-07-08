namespace Retailer.Application.Multitenancy;

public class TenantDto
{
    public string Id { get; set; } = default!;
    public string Identifier { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? DbProvider { get; set; }
    public string? AdminEmail { get; set; }
    public string? LicenseKey { get; set; }
    public bool IsActive { get; set; }
    public bool HasSupplyFeature { get; set; }
    public bool HasSecondaryQty { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}
