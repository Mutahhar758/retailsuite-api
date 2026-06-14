using Retailer.Application.Common.Interfaces;
using Retailer.Application.Multitenancy;
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

        services.AddScoped<ICurrentTenant, CurrentTenant>();
        services.AddScoped<TenantMiddleware>();
        services.AddScoped<TenantLogEnricherMiddleware>();
        services.AddTransient<TenantDatabaseInitializer>();
        services.AddScoped<ITenantService, TenantService>();

        services
            .AddMultiTenant<AppTenantInfo>()
            .WithEFCoreStore<TenantDbContext, AppTenantInfo>()
            .WithHeaderStrategy("X-Tenant-ID")
            .WithClaimStrategy("tenant_id");

        return services;
    }

    internal static IApplicationBuilder UseMultitenancy(this IApplicationBuilder app) =>
        app.UseMultiTenant().UseMiddleware<TenantLogEnricherMiddleware>();

    internal static IApplicationBuilder UseTenantValidation(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantMiddleware>();
}
