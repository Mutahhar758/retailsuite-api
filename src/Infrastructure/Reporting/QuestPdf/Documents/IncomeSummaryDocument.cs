using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Income Statement / Profit & Loss.
/// Generated in-memory using QuestPDF and Lato font.
/// </summary>
public class IncomeSummaryDocument : IDocument
{
    private readonly IncomeSummaryHeader _header;
    private readonly List<IncomeSummaryLineItem> _salesItems;
    private readonly List<IncomeSummaryLineItem> _cogsItems;
    private readonly List<IncomeSummaryLineItem> _expenseItems;

    public IncomeSummaryDocument(
        IncomeSummaryHeader header,
        List<IncomeSummaryLineItem> salesItems,
        List<IncomeSummaryLineItem> cogsItems,
        List<IncomeSummaryLineItem> expenseItems)
    {
        _header = header ?? new IncomeSummaryHeader();
        _salesItems = salesItems ?? new List<IncomeSummaryLineItem>();
        _cogsItems = cogsItems ?? new List<IncomeSummaryLineItem>();
        _expenseItems = expenseItems ?? new List<IncomeSummaryLineItem>();
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

                    brandCol.Item().PaddingTop(2).Text("INCOME STATEMENT (PROFIT & LOSS)")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);

                    brandCol.Item().PaddingTop(2).Text($"For the Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(260).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);

                    if (_header.IsProfitable)
                    {
                        metaCol.Item().PaddingTop(4).Text($"✓ NET PROFIT: Rs. {_header.NetIncome:#,##0.00} ({_header.NetMarginPct:F1}%)")
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(Colors.Green.Darken3);
                    }
                    else
                    {
                        metaCol.Item().PaddingTop(4).Text($"⚠ NET DEFICIT: Rs. ({Math.Abs(_header.NetIncome):#,##0.00})")
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
                    x.Span("Total Revenue: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalSales.ToString("#,##0.00")).FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken3);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Gross Profit: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.GrossProfit.ToString("#,##0.00")).FontSize(9.5f).Bold().FontColor(Colors.Green.Darken3);
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Net Income: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    string netSign = _header.NetIncome >= 0 ? "Rs. " : "Rs. -";
                    x.Span(netSign + Math.Abs(_header.NetIncome).ToString("#,##0.00")).FontSize(10f).Bold()
                        .FontColor(_header.IsProfitable ? Colors.Green.Darken3 : Colors.Red.Darken2);
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Column(col =>
        {
            // 1. REVENUE / SALES
            col.Item().Element(c => ComposeSectionTable(c, "1. REVENUE / SALES", _salesItems, _header.TotalSales, Colors.Blue.Darken3));

            col.Item().PaddingTop(10);

            // 2. COST OF GOODS SOLD
            col.Item().Element(c => ComposeSectionTable(c, "2. COST OF GOODS SOLD (COGS)", _cogsItems, _header.TotalCogs, Colors.Orange.Darken3));

            // GROSS PROFIT SUB-TOTAL
            col.Item().PaddingTop(6).Background(Colors.Grey.Lighten4).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(6).Row(r =>
            {
                r.RelativeItem().Text("GROSS PROFIT (REVENUE - COGS)").FontSize(9).Bold().FontColor(Colors.Grey.Darken4);
                r.AutoItem().Text($"Rs. {_header.GrossProfit:#,##0.00} ({_header.GrossMarginPct:F1}%)").FontSize(9.5f).Bold().FontColor(Colors.Green.Darken3);
            });

            col.Item().PaddingTop(10);

            // 3. OPERATING EXPENSES
            col.Item().Element(c => ComposeSectionTable(c, "3. OPERATING EXPENSES", _expenseItems, _header.TotalExpenses, Colors.Red.Darken2));

            col.Item().PaddingTop(10);

            // NET PROFIT / LOSS SUMMARY
            col.Item().Background(Colors.Grey.Lighten3).Border(1.5f).BorderColor(Colors.Grey.Darken3).Padding(8).Row(r =>
            {
                r.RelativeItem().Text("NET PROFIT / (LOSS) FOR THE PERIOD").FontSize(10.5f).Bold().FontColor(Colors.Grey.Darken4);
                string sign = _header.NetIncome >= 0 ? "Rs. " : "Rs. (";
                string closing = _header.NetIncome >= 0 ? "" : ")";
                r.AutoItem().Text($"{sign}{Math.Abs(_header.NetIncome):#,##0.00}{closing}").FontSize(11).Bold()
                    .FontColor(_header.IsProfitable ? Colors.Green.Darken3 : Colors.Red.Darken2);
            });
        });
    }

    private void ComposeSectionTable(IContainer container, string title, List<IncomeSummaryLineItem> items, decimal totalAmount, string themeColor)
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
                    columns.RelativeColumn(5f); // Title / Head
                    columns.ConstantColumn(100);// Debit
                    columns.ConstantColumn(100);// Credit
                    columns.ConstantColumn(110);// Net Balance
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Account Title / Classification");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Debit (Rs.)");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Credit (Rs.)");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount (Rs.)");
                });

                if (items.Count == 0)
                {
                    table.Cell().ColumnSpan(4).Element(BodyCell).AlignCenter().Text("No line entries recorded in this category").FontColor(Colors.Grey.Lighten1);
                }
                else
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                        table.Cell().Element(c => BodyCellCustom(c, bg)).Text(item.Title ?? string.Empty).SemiBold();
                        table.Cell().Element(c => BodyCellCustom(c, bg)).AlignRight().Text(item.Debit != 0 ? item.Debit.ToString("#,##0.00") : "-");
                        table.Cell().Element(c => BodyCellCustom(c, bg)).AlignRight().Text(item.Credit != 0 ? item.Credit.ToString("#,##0.00") : "-");
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
                row.RelativeItem().Text("Software powered by Bizgrip Solutions (Contact: 03228258734)")
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
