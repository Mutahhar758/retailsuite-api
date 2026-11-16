using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

/// <summary>
/// Looks up tenants for background work. Cloud mode reads the master <see cref="TenantDbContext"/>;
/// on-premises mode returns the single locally configured store (no master database exists locally).
/// </summary>
public interface ITenantLookup
{
    Task<IReadOnlyList<AppTenantInfo>> GetActiveTenantsAsync(CancellationToken cancellationToken);

    Task<AppTenantInfo?> FindAsync(string idOrIdentifier, CancellationToken cancellationToken);
}
