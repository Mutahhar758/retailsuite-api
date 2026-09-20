using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Retailer.Infrastructure.Common;
using Retailer.Infrastructure.Multitenancy;
using Retailer.Infrastructure.Persistence;

namespace Retailer.Infrastructure.BackgroundJobs;

internal static class Startup
{
    internal static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration config)
    {
        var dbSettings = config.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>();
        var multitenancySettings = config.GetSection(nameof(MultitenancySettings)).Get<MultitenancySettings>();
        var dbProvider = (dbSettings?.DBProvider ?? DbProviderKeys.Npgsql).ToLowerInvariant();
        var isPostgreSql = dbProvider is DbProviderKeys.Npgsql or "postgres" or "npgsql";
        var isSqlServer = dbProvider is DbProviderKeys.SqlServer or "sqlserver" or "mssql";

        // Hangfire should live centrally in the Root Tenant Registry Database (RetailSuiteMain)
        var connectionString = !string.IsNullOrWhiteSpace(multitenancySettings?.DefaultConnectionString)
            ? multitenancySettings.DefaultConnectionString
            : dbSettings?.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        services.AddHangfire((provider, configuration) =>
        {
            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            var isPgConnection = isPostgreSql || connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase);

            if (isPgConnection)
            {
                configuration.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString));
            }
            else
            {
                configuration.UseSqlServerStorage(connectionString);
            }
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Math.Max(Environment.ProcessorCount * 2, 4);
        });

        return services;
    }

    internal static IApplicationBuilder UseBackgroundJobs(this IApplicationBuilder app)
    {
        var storage = app.ApplicationServices.GetService<JobStorage>();
        if (storage is null)
        {
            return app;
        }

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
            DashboardTitle = "RetailSuite Jobs"
        });

        return app;
    }
}
