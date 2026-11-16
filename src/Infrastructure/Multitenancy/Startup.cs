using Retailer.Application.Common.Interfaces;
using Retailer.Application.Multitenancy;
using Retailer.Infrastructure.OnPremises;
using Retailer.Infrastructure.Persistence;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal static class Startup
{
    internal static IServiceCollection AddMultitenancy(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<TenantMiddleware>();
        services.AddScoped<TenantLogEnricherMiddleware>();

#if ON_PREMISES
        return services.AddOnPremisesMultitenancy(config);
#else
        return HostingMode.IsOnPremises(config)
            ? services.AddOnPremisesMultitenancy(config)
            : services.AddCloudMultitenancy(config);
#endif
    }

    /// <summary>
    /// On-premises: a single fixed store. No <see cref="TenantDbContext"/>, no tenant catalog and no tenant
    /// management services are registered; the tenant is resolved statically from local configuration.
    /// </summary>
    private static IServiceCollection AddOnPremisesMultitenancy(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<StoreSettings>()
            .BindConfiguration(nameof(StoreSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var store = config.GetSection(nameof(StoreSettings)).Get<StoreSettings>() ?? new StoreSettings();
        var database = config.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>() ?? new DatabaseSettings();

        if (string.IsNullOrWhiteSpace(store.TenantId))
        {
            throw new InvalidOperationException(
                "On-premises mode requires StoreSettings:TenantId. Run the provisioning command (--onprem-provision) first.");
        }

        var tenant = StoreTenantFactory.Create(store, database);

        services.AddSingleton<ITenantLookup, OnPremisesTenantLookup>();
        services.AddTransient<OnPremisesDatabaseInitializer>();

        services
            .AddMultiTenant<AppTenantInfo>()
            .WithInMemoryStore(options => options.Tenants.Add(tenant))
            .WithStaticStrategy(tenant.Identifier);

        return services;
    }

#if !ON_PREMISES
    private static IServiceCollection AddCloudMultitenancy(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<MultitenancySettings>()
            .BindConfiguration(nameof(MultitenancySettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var settings = config.GetSection(nameof(MultitenancySettings)).Get<MultitenancySettings>();

        // Register TenantDbContext against the root connection string (always PostgreSQL)
        services.AddDbContext<TenantDbContext>(options =>
        {
            var dbSettings = config.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>();
            var rootConnectionString = settings?.DefaultConnectionString
                ?? dbSettings?.ConnectionString
                ?? throw new InvalidOperationException("No root connection string configured for multitenancy.");

            options.UseDatabase("postgresql", rootConnectionString, "Migrators.PostgreSQL");
        });

        services.AddScoped<ITenantLookup, CloudTenantLookup>();
        services.AddTransient<TenantDatabaseProvisioner>();
        services.AddTransient<TenantDatabaseInitializer>();
        services.AddScoped<ITenantService, TenantService>();

        services
            .AddMultiTenant<AppTenantInfo>()
            .WithEFCoreStore<TenantDbContext, AppTenantInfo>()
            .WithHeaderStrategy("X-Tenant-ID")
            .WithClaimStrategy("tenant_id");

        return services;
    }
#endif

    internal static IApplicationBuilder UseMultitenancy(this IApplicationBuilder app) =>
        app.UseMultiTenant().UseMiddleware<TenantLogEnricherMiddleware>();

    internal static IApplicationBuilder UseTenantValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantMiddleware>();
}
