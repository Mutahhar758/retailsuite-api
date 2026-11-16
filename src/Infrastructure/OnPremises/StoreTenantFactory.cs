using Retailer.Infrastructure.Persistence;
using Retailer.Shared.Common;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.OnPremises;

/// <summary>Builds the one fixed tenant of an on-premises installation from local configuration.</summary>
internal static class StoreTenantFactory
{
    internal static AppTenantInfo Create(StoreSettings store, DatabaseSettings database) => new()
    {
        Id = store.TenantId,
        Identifier = store.TenantId,
        Name = string.IsNullOrWhiteSpace(store.StoreName) ? store.TenantId : store.StoreName,
        ConnectionString = database.ConnectionString,
        DbProvider = database.DBProvider,
        AdminEmail = store.AdminEmail,
        IsActive = true,
        IndustryType = store.IndustryType,
        BusinessSubType = store.BusinessSubType,
        ValidFrom = PakistanTime.Now.Date.AddDays(-1),
    };
}
