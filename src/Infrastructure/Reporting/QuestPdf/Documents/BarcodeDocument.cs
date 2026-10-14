using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for printing barcode product stickers.
/// Supports both Thermal Roll (50mm x 30mm) standard roll stickers and A4 multi-label sheets (24 labels per sheet).
/// </summary>
public class BarcodeDocument : IDocument
{
    private readonly BarcodePrintRequest _request;
    private readonly string _companyName;

    public BarcodeDocument(BarcodePrintRequest request, string fallbackCompanyName = "Retail Suite")
    {
        _request = request ?? new BarcodePrintRequest();
        _companyName = !string.IsNullOrWhiteSpace(_request.CompanyName)
            ? _request.CompanyName
            : (!string.IsNullOrWhiteSpace(fallbackCompanyName) ? fallbackCompanyName : "Retail Suite");
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        if (string.Equals(_request.Layout, "SheetA4", StringComparison.OrdinalIgnoreCase))
        {
            ComposeSheetA4(container);
        }
        else
        {
            ComposeThermalRoll(container);
        }
    }

    private void ComposeThermalRoll(IDocumentContainer container)
    {
        var expandedItems = ExpandItems();

        if (expandedItems.Count == 0)
        {
            container.Page(page =>
            {
                page.Size(50, 30, Unit.Millimetre);
                page.Margin(2, Unit.Millimetre);
                page.Content().AlignCenter().AlignMiddle().Text("No items selected");
            });
            return;
        }

        foreach (var item in expandedItems)
        {
            string barcode = !string.IsNullOrWhiteSpace(item.Barcode) ? item.Barcode : "000000";
            string svg = BarcodeGenerator.GenerateSvg(barcode, barHeight: 40, unitWidth: 2, quietZone: 6);

            container.Page(page =>
            {
                // Standard 50mm x 30mm continuous sticker roll
                page.Size(50, 30, Unit.Millimetre);
                page.Margin(1.5f, Unit.Millimetre);
                page.PageColor(Colors.White);

                page.Content().Column(col =>
                {
                    if (_request.IncludeCompanyName)
                    {
                        col.Item().AlignCenter().Text(_companyName)
                            .FontSize(6.5f)
                            .Bold()
                            .FontColor(Colors.Grey.Darken3);
                    }

                    if (_request.IncludeTitle)
                    {
                        col.Item().AlignCenter().Text(item.Title ?? string.Empty)
                            .FontSize(7.5f)
                            .Bold()
                            .FontColor(Colors.Black);
                    }

                    col.Item().PaddingVertical(1).AlignCenter().Height(10.5f, Unit.Millimetre).Svg(svg);

                    col.Item().AlignCenter().Text(barcode)
                        .FontSize(6f)
                        .FontColor(Colors.Grey.Darken2);

                    if (_request.IncludePrice)
                    {
                        col.Item().AlignCenter().Text($"Rs. {item.Rate:#,##0.00}")
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(Colors.Black);
                    }
                });
            });
        }
    }

    private void ComposeSheetA4(IDocumentContainer container)
    {
        var expandedItems = ExpandItems();
        int pageSize = 24; // 3 columns x 8 rows
        int totalPages = (int)Math.Ceiling(expandedItems.Count / (double)pageSize);
        if (totalPages == 0) totalPages = 1;

        for (int p = 0; p < totalPages; p++)
        {
            var pageItems = expandedItems.Skip(p * pageSize).Take(pageSize).ToList();

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(12, Unit.Millimetre);
                page.PageColor(Colors.White);

                page.Content().Grid(grid =>
                {
                    grid.Columns(3);
                    grid.Spacing(6);

                    for (int i = 0; i < pageItems.Count; i++)
                    {
                        var item = pageItems[i];
                        string barcode = !string.IsNullOrWhiteSpace(item.Barcode) ? item.Barcode : "000000";
                        string svg = BarcodeGenerator.GenerateSvg(barcode, barHeight: 40, unitWidth: 2, quietZone: 6);

                        grid.Item().Border(0.75f).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.White).Padding(5).Column(sticker =>
                            {
                                if (_request.IncludeCompanyName)
                                {
                                    sticker.Item().AlignCenter().Text(_companyName)
                                        .FontSize(6.5f)
                                        .Bold()
                                        .FontColor(Colors.Grey.Darken2);
                                }

                                if (_request.IncludeTitle)
                                {
                                    sticker.Item().AlignCenter().Text(item.Title ?? string.Empty)
                                        .FontSize(7.5f)
                                        .Bold()
                                        .FontColor(Colors.Grey.Darken4);
                                }

                                sticker.Item().PaddingVertical(1.5f).AlignCenter().Height(12, Unit.Millimetre).Svg(svg);

                                sticker.Item().AlignCenter().Text(barcode)
                                    .FontSize(6.5f)
                                    .FontColor(Colors.Grey.Darken2);

                                if (_request.IncludePrice)
                                {
                                    sticker.Item().AlignCenter().Text($"Rs. {item.Rate:#,##0.00}")
                                        .FontSize(9f)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken3);
                                }
                            });
                    }
                });
            });
        }
    }

    private List<BarcodePrintItemDto> ExpandItems()
    {
        var result = new List<BarcodePrintItemDto>();
        if (_request.Items == null) return result;

        foreach (var item in _request.Items)
        {
            int copies = Math.Max(1, item.Copies);
            for (int i = 0; i < copies; i++)
            {
                result.Add(item);
            }
        }

        return result;
    }
}
