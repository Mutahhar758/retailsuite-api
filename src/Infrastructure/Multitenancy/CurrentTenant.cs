using Retailer.Application.Common.Interfaces;
using Finbuckle.MultiTenant.Abstractions;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

public class CurrentTenant : ICurrentTenant
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public CurrentTenant(IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor) =>
        _multiTenantContextAccessor = multiTenantContextAccessor;

    public string? Id => _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

    public string? Name => _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Name;

    public string? ConnectionString => _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.ConnectionString;

    public bool IsValid =>
        _multiTenantContextAccessor.MultiTenantContext?.TenantInfo is { } tenant
        && tenant.IsActive
        && tenant.ValidFrom <= DateTime.UtcNow
        && (tenant.ValidUntil == null || tenant.ValidUntil >= DateTime.UtcNow);
}
