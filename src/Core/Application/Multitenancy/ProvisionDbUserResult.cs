namespace Retailer.Application.Multitenancy;

public class ProvisionDbUserResult
{
    public string TenantId { get; set; } = default!;
    public string Identifier { get; set; } = default!;
    public string DbProvider { get; set; } = default!;
    public string DatabaseName { get; set; } = default!;
    public string DbUser { get; set; } = default!;
    public bool Success { get; set; }
    public string? Message { get; set; }
}
