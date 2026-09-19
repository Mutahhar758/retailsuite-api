using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Retailer.Infrastructure.PDF;

internal static class Startup
{
    internal static IServiceCollection AddPdfConverter(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        return services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
    }
}