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

        var connectionString = isPostgreSql
            ? multitenancySettings?.DefaultConnectionString ?? dbSettings?.ConnectionString
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

            if (isPostgreSql)
            {
                configuration.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString));
            }
            else if (isSqlServer)
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
