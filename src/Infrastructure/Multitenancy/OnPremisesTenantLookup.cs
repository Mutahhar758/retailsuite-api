using Microsoft.Extensions.Options;
using Retailer.Infrastructure.OnPremises;
using Retailer.Infrastructure.Persistence;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal class OnPremisesTenantLookup : ITenantLookup
{
    private readonly AppTenantInfo _tenant;

    public OnPremisesTenantLookup(IOptions<StoreSettings> store, IOptions<DatabaseSettings> database) =>
        _tenant = StoreTenantFactory.Create(store.Value, database.Value);

    public Task<IReadOnlyList<AppTenantInfo>> GetActiveTenantsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AppTenantInfo>>(new[] { _tenant });

    public Task<AppTenantInfo?> FindAsync(string idOrIdentifier, CancellationToken cancellationToken) =>
        Task.FromResult<AppTenantInfo?>(
            idOrIdentifier == _tenant.Id || idOrIdentifier == _tenant.Identifier ? _tenant : null);
}
