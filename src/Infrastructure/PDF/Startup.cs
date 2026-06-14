using Microsoft.Extensions.DependencyInjection;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Retailer.Infrastructure.PDF;

internal static class Startup
{
    internal static IServiceCollection AddPdfConverter(this IServiceCollection services) =>
    services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

}