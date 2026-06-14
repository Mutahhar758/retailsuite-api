using Retailer.Application.PDF;
using RazorEngineCore;
using System.Text;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;

namespace Retailer.Infrastructure.PDF;

public class PdfService : IPdfService
{
    private readonly IConverter htmlToPdfConverter;
    public PdfService(IConverter htmlToPdfConverter)
    {
        this.htmlToPdfConverter = htmlToPdfConverter;
    }

    public string GeneratePdfTemplate<T>(string templateName, T pdfTemplateModel)
    {
        string template = GetTemplate(templateName);

        IRazorEngine razorEngine = new RazorEngine();
        var modifiedTemplate = razorEngine.Compile(template);

        return modifiedTemplate.Run(pdfTemplateModel);
    }

    public static string GetTemplate(string templateName)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string tmplFolder = Path.Combine(baseDirectory, "PdfTemplates");
        string filePath = Path.Combine(tmplFolder, $"{templateName}.cshtml");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs, Encoding.Default);
        string htmlBody = sr.ReadToEnd();
        sr.Close();

        return htmlBody;
    }

    public byte[] HtmlToPdf(string html, string title, bool isLandscape = false)
    {
        var objectSettings = new ObjectSettings()
        {
            HtmlContent = html,
            WebSettings = { DefaultEncoding = "utf-8" },
        };

        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings =
            {
                ColorMode = ColorMode.Color,
                Orientation = isLandscape ? Orientation.Landscape : Orientation.Portrait,
                PaperSize = PaperKind.A4,
                DPI = 300,
                DocumentTitle = title
            },
            Objects = { objectSettings }
        };

        return htmlToPdfConverter.Convert(doc);
    }
}