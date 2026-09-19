using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Balance Sheet (Statement of Financial Position).
/// Generated in-memory using QuestPDF and Lato font.
/// </summary>
public class BalanceSheetDocument : IDocument
{
    private readonly BalanceSheetHeader _header;
    private readonly List<BalanceSheetReportItem> _assetItems;
    private readonly List<BalanceSheetReportItem> _liabilityItems;
    private readonly List<BalanceSheetReportItem> _equityItems;

    public BalanceSheetDocument(
        BalanceSheetHeader header,
        List<BalanceSheetReportItem> assetItems,
        List<BalanceSheetReportItem> liabilityItems,
        List<BalanceSheetReportItem> equityItems)
    {
        _header = header ?? new BalanceSheetHeader();
        _assetItems = assetItems ?? new List<BalanceSheetReportItem>();
        _liabilityItems = liabilityItems ?? new List<BalanceSheetReportItem>();
        _equityItems = equityItems ?? new List<BalanceSheetReportItem>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily(Fonts.Lato).FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(brandCol =>
                {
                    brandCol.Item().Text(_header.CompanyName)
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    brandCol.Item().PaddingTop(2).Text("BALANCE SHEET (STATEMENT OF FINANCIAL POSITION)")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);

                    brandCol.Item().PaddingTop(2).Text($"As of Date: {_header.AsOnDate:dd-MMM-yyyy}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(260).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);

                    if (_header.IsBalanced)
                    {
                        metaCol.Item().PaddingTop(4).Text("✓ BALANCED (Assets = Liab + Equity)")
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(Colors.Green.Darken3);
                    }
                    else
                    {
                        metaCol.Item().PaddingTop(4).Text($"⚠ OUT OF BALANCE: Diff Rs. {_header.Variance:#,##0.00}")
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(Colors.Red.Darken2);
                    }
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // Summary Financial Metrics Strip
            col.Item().PaddingTop(6).PaddingBottom(4).Row(kpiRow =>
            {
                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Assets: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalAssets.ToString("#,##0.00")).FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken3);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Liabilities: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalLiabilities.ToString("#,##0.00")).FontSize(9.5f).Bold().FontColor(Colors.Red.Darken2);
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Total Equity: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalEquity.ToString("#,##0.00")).FontSize(9.5f).Bold().FontColor(Colors.Green.Darken3);
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Column(col =>
        {
            // ASSETS SECTION
            col.Item().Element(c => ComposeSectionTable(c, "1. ASSETS", _assetItems, _header.TotalAssets, Colors.Blue.Darken3));

            col.Item().PaddingTop(12);

            // LIABILITIES SECTION
            col.Item().Element(c => ComposeSectionTable(c, "2. LIABILITIES", _liabilityItems, _header.TotalLiabilities, Colors.Red.Darken2));

            col.Item().PaddingTop(12);

            // EQUITY SECTION
            col.Item().Element(c => ComposeSectionTable(c, "3. EQUITY & CAPITAL", _equityItems, _header.TotalEquity, Colors.Green.Darken3));

            col.Item().PaddingTop(12);

            // TOTAL LIABILITIES & EQUITY RECONCILIATION
            col.Item().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Darken2).Padding(8).Row(r =>
            {
                r.RelativeItem().Text("TOTAL LIABILITIES & EQUITY").FontSize(10).Bold().FontColor(Colors.Grey.Darken4);
                r.AutoItem().Text("Rs. " + _header.TotalLiabilitiesAndEquity.ToString("#,##0.00")).FontSize(10.5f).Bold().FontColor(Colors.Grey.Darken4);
            });
        });
    }

    private void ComposeSectionTable(IContainer container, string title, List<BalanceSheetReportItem> items, decimal totalAmount, string themeColor)
    {
        container.Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(6).Row(r =>
            {
                r.RelativeItem().Text(title).FontSize(9).Bold().FontColor(Colors.Grey.Darken3);
                r.AutoItem().Text("Rs. " + totalAmount.ToString("#,##0.00")).FontSize(9).Bold().FontColor(themeColor);
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3f); // Head / Classification
                    columns.RelativeColumn(4f); // Account Title
                    columns.ConstantColumn(120); // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Category");
                    header.Cell().Element(HeaderCell).Text("Account Title");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount (Rs.)");
                });

                if (items.Count == 0)
                {
                    table.Cell().ColumnSpan(3).Element(BodyCell).AlignCenter().Text("No accounts recorded in this category").FontColor(Colors.Grey.Lighten1);
                }
                else
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                        table.Cell().Element(c => BodyCellCustom(c, bg)).Text(item.Level2 ?? item.Level1 ?? "-").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                        table.Cell().Element(c => BodyCellCustom(c, bg)).Text(item.Title ?? string.Empty).SemiBold();
                        table.Cell().Element(c => BodyCellCustom(c, bg)).AlignRight().Text(item.Amount.ToString("#,##0.00")).SemiBold();
                    }
                }
            });
        });
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Darken1)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.SemiBold().FontSize(7.5f).FontColor(Colors.Grey.Darken3));
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(3.5f)
            .PaddingHorizontal(4);
    }

    private static IContainer BodyCellCustom(IContainer container, string bg)
    {
        return container
            .Background(bg)
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(3.5f)
            .PaddingHorizontal(4);
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("Confidential • Retail Suite Financial Intelligence • Balance Sheet")
                    .FontSize(7.5f)
                    .FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    x.CurrentPageNumber().FontSize(7.5f).SemiBold().FontColor(Colors.Grey.Darken3);
                    x.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    x.TotalPages().FontSize(7.5f).SemiBold().FontColor(Colors.Grey.Darken3);
                });
            });
        });
    }
}
