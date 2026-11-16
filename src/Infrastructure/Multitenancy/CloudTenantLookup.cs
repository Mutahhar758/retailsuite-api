using Microsoft.EntityFrameworkCore;
using Retailer.Shared.Common;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal class CloudTenantLookup : ITenantLookup
{
    private readonly TenantDbContext _tenantDbContext;

    public CloudTenantLookup(TenantDbContext tenantDbContext) => _tenantDbContext = tenantDbContext;

    public async Task<IReadOnlyList<AppTenantInfo>> GetActiveTenantsAsync(CancellationToken cancellationToken)
    {
        var now = PakistanTime.Now;
        return await _tenantDbContext.TenantInfo
            .AsNoTracking()
            .Where(t => t.IsActive && t.ValidFrom <= now && (t.ValidUntil == null || t.ValidUntil >= now))
            .ToListAsync(cancellationToken);
    }

    public Task<AppTenantInfo?> FindAsync(string idOrIdentifier, CancellationToken cancellationToken) =>
        _tenantDbContext.TenantInfo.FirstOrDefaultAsync(
            t => t.Id == idOrIdentifier || t.Identifier == idOrIdentifier, cancellationToken);
}
