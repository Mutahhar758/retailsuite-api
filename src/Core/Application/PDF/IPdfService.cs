namespace Retailer.Application.PDF;

public interface IPdfService : ITransientService
{
    string GeneratePdfTemplate<T>(string templateName, T pdfTemplateModel);
    byte[] HtmlToPdf(string html, string title, bool isLandscape = false);
}