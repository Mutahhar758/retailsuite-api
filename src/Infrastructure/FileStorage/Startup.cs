using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.OnPremises;

namespace Retailer.Infrastructure.FileStorage;

internal static class Startup
{
    internal static IApplicationBuilder UseFileStorage(this IApplicationBuilder app)
    {
        var filesPath = Path.Combine(AppContext.BaseDirectory, "Files");
        if (!Directory.Exists(filesPath))
        {
            filesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "RetailSuite",
                "Files");
            Directory.CreateDirectory(filesPath);
        }

        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(filesPath),
            RequestPath = new PathString("/Files"),
        });
    }

    internal static IServiceCollection AddAzure(this IServiceCollection services, IConfiguration config)
    {
#if ON_PREMISES
        services.AddScoped<IMediaServiceClient, LocalMediaServiceClient>();
#else
        if (HostingMode.IsOnPremises(config))
        {
            services.AddScoped<IMediaServiceClient, LocalMediaServiceClient>();
        }
        else
        {
            services.Configure<MediaServiceSettings>(config.GetSection(nameof(MediaServiceSettings)));
            services.AddHttpClient<IMediaServiceClient, MediaServiceClient>();
        }
#endif
        return services.Configure<AzureStorageSettings>(config.GetSection(nameof(AzureStorageSettings)));
    }
}
