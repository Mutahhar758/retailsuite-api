using Retailer.Application.Common.Caching;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.Caching;

public class CacheKeyService : ICacheKeyService
{
    private readonly ICurrentTenant _currentTenant;

    public CacheKeyService(ICurrentTenant currentTenant) =>
        _currentTenant = currentTenant;

    public string GetCacheKey(string name, object id, bool includeTenantId = true)
    {
        var tenantPrefix = includeTenantId && !string.IsNullOrWhiteSpace(_currentTenant.Id)
            ? $"{_currentTenant.Id}:"
            : string.Empty;
        return $"{tenantPrefix}{name}-{id}";
    }
}