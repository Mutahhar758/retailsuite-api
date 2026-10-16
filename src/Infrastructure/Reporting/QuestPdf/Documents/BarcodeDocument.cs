using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for printing barcode product stickers.
/// Supports:
/// 1. 4-inch Thermal Roll (3 barcodes horizontally across per row, 101.6mm x 25.4mm) - Standard 3-Up
/// 2. Single Thermal Roll (50mm x 30mm)
/// 3. A4 Multi-Sticker Sheet (24 labels per sheet, 3x8 grid)
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
        else if (string.Equals(_request.Layout, "ThermalRoll", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(_request.Layout, "ThermalRollSingle", StringComparison.OrdinalIgnoreCase))
        {
            ComposeThermalRollSingle(container);
        }
        else
        {
            // Default: 4-inch thermal roll with 3 barcodes horizontally (3-Up)
            ComposeThermalRoll3Up(container);
        }
    }

    /// <summary>
    /// 4-inch thermal roll with 3 barcodes horizontally per feed row (101.6mm x 25.4mm / 4" x 1").
    /// </summary>
    private void ComposeThermalRoll3Up(IDocumentContainer container)
    {
        var expandedItems = ExpandItems();

        if (expandedItems.Count == 0)
        {
            container.Page(page =>
            {
                page.Size(101.6f, 25.4f, Unit.Millimetre);
                page.Margin(1, Unit.Millimetre);
                page.Content().AlignCenter().AlignMiddle().Text("No items selected");
            });
            return;
        }

        int pageSize = 3; // 3 stickers horizontally per row
        int totalRows = (int)Math.Ceiling(expandedItems.Count / (double)pageSize);

        for (int r = 0; r < totalRows; r++)
        {
            var rowItems = expandedItems.Skip(r * pageSize).Take(pageSize).ToList();

            container.Page(page =>
            {
                page.Size(101.6f, 25.4f, Unit.Millimetre);
                page.MarginVertical(0.8f, Unit.Millimetre);
                page.MarginHorizontal(1.5f, Unit.Millimetre);
                page.PageColor(Colors.White);

                page.Content().Row(row =>
                {
                    for (int c = 0; c < 3; c++)
                    {
                        if (c > 0)
                        {
                            row.ConstantItem(2, Unit.Millimetre); // Gap between stickers
                        }

                        if (c < rowItems.Count)
                        {
                            row.RelativeItem().Element(cell => RenderStickerCell(cell, rowItems[c], 30f, 7.5f, 5.5f, 6.5f, 7.5f));
                        }
                        else
                        {
                            row.RelativeItem(); // Empty space to preserve 3-column width
                        }
                    }
                });
            });
        }
    }

    /// <summary>
    /// Single-column 50mm x 30mm thermal sticker roll (1-Up).
    /// </summary>
    private void ComposeThermalRollSingle(IDocumentContainer container)
    {
        var expandedItems = ExpandItems();

        if (expandedItems.Count == 0)
        {
            container.Page(page =>
            {
                page.Size(50, 30, Unit.Millimetre);
                page.Margin(1.5f, Unit.Millimetre);
                page.Content().AlignCenter().AlignMiddle().Text("No items selected");
            });
            return;
        }

        foreach (var item in expandedItems)
        {
            container.Page(page =>
            {
                page.Size(50, 30, Unit.Millimetre);
                page.Margin(1.5f, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.Content().Element(cell => RenderStickerCell(cell, item, 46f, 9.5f, 6.5f, 7.5f, 8.5f));
            });
        }
    }

    /// <summary>
    /// Multi-label A4 Sheet (24 labels: 3 columns x 8 rows).
    /// </summary>
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
                page.Margin(10, Unit.Millimetre);
                page.PageColor(Colors.White);

                page.Content().Grid(grid =>
                {
                    grid.Columns(3);
                    grid.Spacing(6);

                    for (int i = 0; i < pageItems.Count; i++)
                    {
                        var item = pageItems[i];
                        grid.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.White)
                            .Element(cell => RenderStickerCell(cell, item, 58f, 10f, 6.5f, 7.5f, 8.5f));
                    }
                });
            });
        }
    }

    private void RenderStickerCell(
        IContainer container,
        BarcodePrintItemDto item,
        float maxLabelWidthMm,
        float barcodeHeightMm,
        float companyFontSize,
        float titleFontSize,
        float priceFontSize)
    {
        string barcode = !string.IsNullOrWhiteSpace(item.Barcode) ? item.Barcode : "000000";
        var slices = BarcodeGenerator.GenerateSlices(barcode, quietZone: 3);

        string company = _companyName;
        if (company.Length > 24) company = company.Substring(0, 22) + "..";

        string title = item.Title ?? string.Empty;
        if (title.Length > 22) title = title.Substring(0, 20) + "..";

        bool isVariable = item.IsVariableWeight || (item.Qty.HasValue && item.Qty.Value > 0);
        float effectiveBarcodeHeight = isVariable ? Math.Max(5f, barcodeHeightMm - 1.2f) : barcodeHeightMm;

        container
            .PaddingHorizontal(0.5f, Unit.Millimetre)
            .PaddingVertical(0.5f, Unit.Millimetre)
            .Column(col =>
            {
                if (_request.IncludeCompanyName)
                {
                    col.Item().AlignCenter().Text(company)
                        .FontSize(companyFontSize)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);
                }

                if (_request.IncludeTitle)
                {
                    col.Item().AlignCenter().Text(title)
                        .FontSize(titleFontSize)
                        .Bold()
                        .FontColor(Colors.Black);
                }

                // Vector barcode bars via QuestPDF Row - no SVG aspect-ratio wrapping or scaling conflicts!
                col.Item()
                    .PaddingTop(0.5f)
                    .PaddingBottom(0.5f)
                    .AlignCenter()
                    .Width(maxLabelWidthMm - 3, Unit.Millimetre)
                    .Height(effectiveBarcodeHeight, Unit.Millimetre)
                    .Row(row =>
                    {
                        foreach (var slice in slices)
                        {
                            if (slice.IsBlack)
                                row.RelativeItem(slice.Width).Background(Colors.Black);
                            else
                                row.RelativeItem(slice.Width);
                        }
                    });

                float barcodeFontSize = maxLabelWidthMm <= 32f
                    ? (barcode.Length > 22 ? 2.8f : (barcode.Length > 18 ? 3.2f : (barcode.Length > 12 ? 3.8f : 4.8f)))
                    : (barcode.Length > 22 ? 3.5f : (barcode.Length > 18 ? 4.0f : (barcode.Length > 12 ? 4.8f : 5.5f)));

                col.Item().AlignCenter().Text(text =>
                {
                    text.ClampLines(1);
                    text.Span(barcode)
                        .FontSize(barcodeFontSize)
                        .FontColor(Colors.Grey.Darken2);
                });

                if (_request.IncludePrice)
                {
                    if (isVariable)
                    {
                        decimal total = Math.Round(item.TotalAmount ?? ((item.Qty ?? 1) * item.Rate));
                        decimal rate = Math.Round(item.Rate);
                        string unitStr = !string.IsNullOrWhiteSpace(item.Unit) && !decimal.TryParse(item.Unit, out _) ? item.Unit : "kg";
                        col.Item().AlignCenter().Text($"Qty: {item.Qty:0.###} {unitStr}  •  Rs. {total:#,##0}")
                            .FontSize(priceFontSize - 0.5f)
                            .Bold()
                            .FontColor(Colors.Black);

                        col.Item().AlignCenter().Text($"Rate: Rs. {rate:#,##0}/{unitStr}")
                            .FontSize(companyFontSize - 0.5f)
                            .FontColor(Colors.Grey.Darken2);
                    }
                    else
                    {
                        col.Item().AlignCenter().Text($"Rs. {Math.Round(item.Rate):#,##0}")
                            .FontSize(priceFontSize)
                            .Bold()
                            .FontColor(Colors.Black);
                    }
                }
            });
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
