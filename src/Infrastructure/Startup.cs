using System.Runtime.CompilerServices;
using Retailer.Infrastructure.Auth;
using Retailer.Infrastructure.Auth.InternalServiceAuthorization;
using Retailer.Infrastructure.Caching;
using Retailer.Infrastructure.Common;
using Retailer.Infrastructure.Cors;
using Retailer.Infrastructure.FileStorage;
using Retailer.Infrastructure.Identity;
using Retailer.Infrastructure.Link;
using Retailer.Infrastructure.Localization;
using Retailer.Infrastructure.Mailing;
using Retailer.Infrastructure.Mapping;
using Retailer.Infrastructure.Middleware;
using Retailer.Infrastructure.Multitenancy;
using Retailer.Infrastructure.OpenApi;
using Retailer.Infrastructure.PDF;
using Retailer.Infrastructure.Persistence;
using Retailer.Infrastructure.Persistence.Initialization;
using Retailer.Infrastructure.SecurityHeaders;
using Retailer.Shared.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Infrastructure.Test")]

namespace Retailer.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MapsterSettings.Configure();
        return services
            .AddApiVersioning()
            .AddMultitenancy(config)
            .AddAuth(config)
            .AddAzure(config)
            .AddCaching(config)
            .AddCorsPolicy(config)
            .AddRequestLogging(config)
            .AddExceptionMiddleware()
            .AddUserSessionMiddleware()
            .AddEfTransactionMiddleware()
            .AddHealthCheck()
            .AddLocalization(config)
            .AddPdfConverter()
            .AddOpenApiDocumentation(config)
            .AddPersistence()
            .AddRouting(options => options.LowercaseUrls = true)
            .AddServices()
            .AddMailing(config)
            .AddInternalServicesKey(config)
            .AddExternalLinks(config);
    }

    private static IServiceCollection AddApiVersioning(this IServiceCollection services) =>
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });

    private static IServiceCollection AddHealthCheck(this IServiceCollection services) =>
        services.AddHealthChecks().Services;

    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        // Initializes root tenant registry DB, migrates and seeds every tenant's database.
        await scope.ServiceProvider.GetRequiredService<TenantDatabaseInitializer>()
            .InitializeAsync(cancellationToken);
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder, IConfiguration config) =>
        builder
            .UseLocalization(config)
            .UseStaticFiles()
            .UseSecurityHeaders(config)
            .UseFileStorage()
            .UseCorsPolicy()
            .UseRouting()
            .UseMultitenancy()
            .UseTenantValidation()
            .UseRequestLogging(config)
            .UseExceptionMiddleware()
            .UseUserSessionMiddleware()
            .UseAuthentication()
            .UseCurrentUser()
            .UseAuthorization()
            .UseEfTransaction()
            .UseOpenApiDocumentation(config);

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers().RequireAuthorization();
        builder.MapHealthCheck();
        return builder;
    }

    private static IEndpointConventionBuilder MapHealthCheck(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHealthChecks("/api/health");
}