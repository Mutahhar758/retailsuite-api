using System.ComponentModel.DataAnnotations;

namespace Retailer.Infrastructure.Multitenancy;

public class MultitenancySettings : IValidatableObject
{
    public string TenantResolutionStrategy { get; set; } = "header";

    public string DefaultConnectionString { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(DefaultConnectionString))
        {
            yield return new ValidationResult(
                $"{nameof(MultitenancySettings)}.{nameof(DefaultConnectionString)} is not configured",
                new[] { nameof(DefaultConnectionString) });
        }
    }
}
