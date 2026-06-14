using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.FileStorage;

internal static class Startup
{
    internal static IApplicationBuilder UseFileStorage(this IApplicationBuilder app) =>
        app.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Files")),
            RequestPath = new PathString("/Files")
        });

    internal static IServiceCollection AddAzure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MediaServiceSettings>(config.GetSection(nameof(MediaServiceSettings)));
        services.AddHttpClient<IMediaServiceClient, MediaServiceClient>();
        return services.Configure<AzureStorageSettings>(config.GetSection(nameof(AzureStorageSettings)));
    }
}