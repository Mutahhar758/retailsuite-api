using System.ComponentModel.DataAnnotations;

namespace Retailer.Infrastructure.OnPremises;

/// <summary>
/// Identity of the single store served by an on-premises installation (config section "StoreSettings").
/// Values are written by the installer into the DPAPI-encrypted local store file.
/// </summary>
public class StoreSettings : IValidatableObject
{
    public string TenantId { get; set; } = string.Empty;

    public string StoreName { get; set; } = string.Empty;

    public string? AdminEmail { get; set; }

    /// <summary>Initial admin password generated at install time (used only when the admin user is first seeded).</summary>
    public string? InitialAdminPassword { get; set; }

    public string? LicenseKey { get; set; }

    public string IndustryType { get; set; } = "general";

    public string BusinessSubType { get; set; } = "general_store";

    public bool HasSupplyFeature { get; set; } = true;

    public bool HasSecondaryQty { get; set; }

    public bool HasKotFeature { get; set; }

    public bool HasVariablePackFeature { get; set; }

    public bool HasMobileShopFeature { get; set; }

    public string? CloudRelayUrl { get; set; }

    public string LocalLoopbackUrl { get; set; } = "http://127.0.0.1:5050";

    /// <summary>Maximum operating hours allowed without a cloud heartbeat when CloudRelayUrl is configured (default 72h).</summary>
    public int MaxOfflineHours { get; set; } = 72;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(TenantId))
        {
            yield return new ValidationResult("StoreSettings.TenantId is not configured", new[] { nameof(TenantId) });
        }
    }
}
