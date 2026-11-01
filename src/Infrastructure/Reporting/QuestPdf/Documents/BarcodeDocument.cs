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

        float spread = _request.SideStickersSpreadMm > 0 ? _request.SideStickersSpreadMm : 2.0f;
        float pageMarginH = Math.Max(0.2f, 1.0f - (spread * 0.35f));
        float interStickerGap = 2.0f + (spread * 0.65f);

        for (int r = 0; r < totalRows; r++)
        {
            var rowItems = expandedItems.Skip(r * pageSize).Take(pageSize).ToList();

            container.Page(page =>
            {
                page.Size(100f, 22.86f, Unit.Millimetre);
                page.MarginVertical(0.4f, Unit.Millimetre);
                page.MarginHorizontal(pageMarginH, Unit.Millimetre);
                page.PageColor(Colors.White);

                page.Content().Row(row =>
                {
                    for (int c = 0; c < 3; c++)
                    {
                        if (c > 0)
                        {
                            row.ConstantItem(interStickerGap, Unit.Millimetre); // Gap between stickers
                        }

                        if (c < rowItems.Count)
                        {
                            float leftPad = 0.5f;
                            float rightPad = 0.5f;
                            if (c == 0)
                            {
                                // Shift leftmost sticker outward (left) to decrease outer left margin
                                leftPad = Math.Max(0.2f, 0.5f - (spread * 0.3f));
                                rightPad = 0.5f + (spread * 0.3f);
                            }
                            else if (c == 2)
                            {
                                // Shift rightmost sticker outward (right) to decrease outer right margin
                                leftPad = 0.5f + (spread * 0.3f);
                                rightPad = Math.Max(0.2f, 0.5f - (spread * 0.3f));
                            }

                            row.RelativeItem().Element(cell => RenderStickerCell(cell, rowItems[c], 30f, 7.5f, 5.5f, 6.5f, 7.5f, leftPad, rightPad));
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
        float priceFontSize,
        float leftPaddingMm = 0.5f,
        float rightPaddingMm = 0.5f)
    {
        string barcode = !string.IsNullOrWhiteSpace(item.Barcode)
            ? item.Barcode.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim()
            : "000000";
        var slices = BarcodeGenerator.GenerateSlices(barcode, quietZone: 3);

        string company = _companyName;
        if (company.Length > 24) company = company.Substring(0, 22) + "..";

        string title = item.Title ?? string.Empty;
        if (title.Length > 22) title = title.Substring(0, 20) + "..";

        string? subHead = !string.IsNullOrWhiteSpace(item.CompanySubHeader) ? item.CompanySubHeader : _request.CompanySubHeader;
        bool hasSubHeader = _request.IncludeCompanyName && !string.IsNullOrWhiteSpace(subHead);
        bool isVariable = item.IsVariableWeight || (item.Qty.HasValue && item.Qty.Value > 0);
        bool hasRate = _request.IncludeRate && item.Rate > 0;

        float effectiveBarcodeHeight;
        if (maxLabelWidthMm <= 32f)
        {
            if (hasSubHeader && hasRate)
                effectiveBarcodeHeight = 4.5f;
            else if (hasSubHeader || hasRate)
                effectiveBarcodeHeight = 5.2f;
            else
                effectiveBarcodeHeight = isVariable ? 6.2f : 7.2f;
        }
        else
        {
            effectiveBarcodeHeight = isVariable ? Math.Max(5.5f, barcodeHeightMm - 1.2f) : barcodeHeightMm;
        }

        float effectiveCompanyFontSize = hasSubHeader && maxLabelWidthMm <= 32f ? 5.0f : companyFontSize;
        float subFontSize = 4.0f;
        float effectiveTitleFontSize = hasSubHeader && maxLabelWidthMm <= 32f ? 5.8f : titleFontSize;
        float effectivePriceFontSize = hasSubHeader && maxLabelWidthMm <= 32f ? 6.2f : priceFontSize;
        float rateFontSize = hasSubHeader && maxLabelWidthMm <= 32f ? 4.8f : companyFontSize;

        container
            .PaddingLeft(leftPaddingMm, Unit.Millimetre)
            .PaddingRight(rightPaddingMm, Unit.Millimetre)
            .PaddingVertical(0.2f, Unit.Millimetre)
            .Column(col =>
            {
                col.Spacing(0);

                if (_request.IncludeCompanyName)
                {
                    col.Item().AlignCenter().Text(company)
                        .FontSize(effectiveCompanyFontSize)
                        .Bold()
                        .FontColor(Colors.Black);

                    if (hasSubHeader)
                    {
                        string displaySub = subHead!.Length > 28 ? subHead.Substring(0, 26) + ".." : subHead;
                        col.Item().AlignCenter().Text(displaySub)
                            .FontSize(subFontSize)
                            .FontColor(Colors.Black);
                    }
                }

                if (_request.IncludeTitle)
                {
                    col.Item().AlignCenter().Text(title)
                        .FontSize(effectiveTitleFontSize)
                        .Bold()
                        .FontColor(Colors.Black);
                }

                // Vector barcode bars via QuestPDF Row - no SVG aspect-ratio wrapping or scaling conflicts!
                col.Item()
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
                    ? (barcode.Length > 22 ? 3.6f : (barcode.Length > 18 ? 4.5f : (barcode.Length > 12 ? 5.2f : 6.0f)))
                    : (barcode.Length > 22 ? 4.2f : (barcode.Length > 18 ? 5.5f : (barcode.Length > 12 ? 6.5f : 7.5f)));

                col.Item().AlignCenter().Text(text =>
                {
                    text.ClampLines(1);
                    text.Span(barcode)
                        .FontSize(barcodeFontSize)
                        .Bold()
                        .FontColor(Colors.Black);
                });

                if (isVariable)
                {
                    decimal total = Math.Round(item.TotalAmount ?? ((item.Qty ?? 1) * item.Rate));
                    string unitStr = !string.IsNullOrWhiteSpace(item.Unit) && !decimal.TryParse(item.Unit, out _) ? item.Unit : "kg";

                    decimal dispQty = item.DisplayQty.HasValue ? item.DisplayQty.Value : (item.Qty ?? 1m);
                    string dispUnit = !string.IsNullOrWhiteSpace(item.DisplayUnit) ? item.DisplayUnit : unitStr;
                    string formattedQty = dispQty % 1m == 0m ? dispQty.ToString("0") : dispQty.ToString("0.###");

                    if (_request.IncludePrice)
                    {
                        string priceText = total > 0
                            ? $"Qty: {formattedQty} {dispUnit}  •  Rs. {total:#,##0}"
                            : $"Qty: {formattedQty} {dispUnit}";

                        col.Item().AlignCenter().Text(priceText)
                            .FontSize(effectivePriceFontSize)
                            .Bold()
                            .FontColor(Colors.Black);
                    }
                    else
                    {
                        col.Item().AlignCenter().Text($"Qty: {formattedQty} {dispUnit}")
                            .FontSize(effectivePriceFontSize)
                            .Bold()
                            .FontColor(Colors.Black);
                    }

                    if (hasRate)
                    {
                        decimal rate = Math.Round(item.Rate);
                        col.Item().AlignCenter().Text($"Rate: Rs. {rate:#,##0}/{unitStr}")
                            .FontSize(rateFontSize)
                            .Bold()
                            .FontColor(Colors.Black);
                    }
                }
                else
                {
                    if (_request.IncludePrice)
                    {
                        col.Item().AlignCenter().Text($"Rs. {Math.Round(item.Rate):#,##0}")
                            .FontSize(effectivePriceFontSize + 1.2f)
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
