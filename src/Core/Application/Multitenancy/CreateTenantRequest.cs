using FluentValidation;

namespace Retailer.Application.Multitenancy;

public class CreateTenantRequest
{
    public string Id { get; set; } = default!;
    public string Identifier { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DbProvider { get; set; } = default!;
    public string? AdminEmail { get; set; }
    public bool HasSupplyFeature { get; set; } = true;
    public bool HasSecondaryQty { get; set; } = false;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class CreateTenantResponse
{
    public string Id { get; set; } = default!;
    public string LicenseKey { get; set; } = default!;
}

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(r => r.Identifier)
            .NotEmpty()
            .MaximumLength(64)
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Identifier must be lowercase alphanumeric with hyphens only.");

        RuleFor(r => r.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(r => r.DbProvider)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(r => r.AdminEmail)
            .EmailAddress()
            .When(r => !string.IsNullOrWhiteSpace(r.AdminEmail));

        RuleFor(r => r.ValidUntil)
            .GreaterThan(r => r.ValidFrom ?? DateTime.UtcNow)
            .When(r => r.ValidUntil.HasValue)
            .WithMessage("ValidUntil must be after ValidFrom.");
    }
}
