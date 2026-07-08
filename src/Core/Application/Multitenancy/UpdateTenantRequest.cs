using FluentValidation;

namespace Retailer.Application.Multitenancy;

public class UpdateTenantRequest
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DbProvider { get; set; } = default!;
    public string? AdminEmail { get; set; }
    public bool HasSupplyFeature { get; set; }
    public bool HasSecondaryQty { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty();

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
