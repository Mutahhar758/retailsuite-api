namespace Retailer.Application.Multitenancy;

public class InviteTenantRequest
{
    public string? RecipientEmail { get; set; }
    public string? DesktopSetupUrl { get; set; }
    public string? WebAppUrl { get; set; }
    public string? AndroidAppUrl { get; set; }
    public string? CustomMessage { get; set; }
}
