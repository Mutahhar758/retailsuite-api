using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Retailer.Infrastructure.OnPremises;

namespace Retailer.Infrastructure.Tunnel;

internal static class Startup
{
    internal static IServiceCollection AddTunnel(this IServiceCollection services, IConfiguration config)
    {
        services.AddSignalR();

#if ON_PREMISES
        services.AddSingleton<OnPremisesLicenseGuard>();
        services.AddHostedService<LocalTunnelAgent>();
#else
        if (HostingMode.IsOnPremises(config))
        {
            services.AddSingleton<OnPremisesLicenseGuard>();
            services.AddHostedService<LocalTunnelAgent>();
        }
        else
        {
            services.AddSingleton<CloudTunnelRegistry>();
            services.AddScoped<OnPremisesProxyMiddleware>();
        }
#endif

        return services;
    }

    internal static IApplicationBuilder UseOnPremisesTunnelProxy(this IApplicationBuilder app, IConfiguration config)
    {
#if !ON_PREMISES
        if (!HostingMode.IsOnPremises(config))
        {
            app.UseMiddleware<OnPremisesProxyMiddleware>();
        }
#endif

        return app;
    }

    internal static IEndpointRouteBuilder MapTunnelEndpoints(this IEndpointRouteBuilder endpoints)
    {
#if !ON_PREMISES
        endpoints.MapHub<CloudRelayHub>(TunnelConstants.HubPath);
#endif
        return endpoints;
    }
}
