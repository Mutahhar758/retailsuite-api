using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

public class ProfitByCustomerDocument : IDocument
{
    private readonly ProfitByCustomerReportHeader _header;
    private readonly List<ProfitByCustomerReportItem> _items;

    public ProfitByCustomerDocument(
        ProfitByCustomerReportHeader header,
        List<ProfitByCustomerReportItem> items)
    {
        _header = header ?? new ProfitByCustomerReportHeader();
        _items = items ?? new List<ProfitByCustomerReportItem>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28, Unit.Point);
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

                    brandCol.Item().PaddingTop(2).Text("PROFIT BY CUSTOMER REPORT")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);

                    brandCol.Item().PaddingTop(2).Text($"Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}" + (!string.IsNullOrWhiteSpace(_header.CustomerFilter) ? $"  |  Filter: {_header.CustomerFilter}" : string.Empty))
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(300).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);

                    metaCol.Item().PaddingTop(4).Text($"TOTAL PROFIT: Rs. {_header.GrossProfit:#,##0.00} ({_header.GrossMarginPct:F1}%)")
                        .FontSize(10.5f)
                        .Bold()
                        .FontColor(_header.IsProfitable ? Colors.Green.Darken3 : Colors.Red.Darken2);
                });
            });

            // Summary Metric Pill Cards
            col.Item().PaddingTop(8).PaddingBottom(6).Row(kpiRow =>
            {
                ComposeKpiCard(kpiRow.RelativeItem(), "TOTAL REVENUE", $"Rs. {_header.TotalSales:#,##0.00}", Colors.Blue.Darken2);
                kpiRow.ConstantItem(8);
                ComposeKpiCard(kpiRow.RelativeItem(), "TOTAL COST", $"Rs. {_header.TotalCost:#,##0.00}", Colors.Amber.Darken3);
                kpiRow.ConstantItem(8);
                ComposeKpiCard(kpiRow.RelativeItem(), "GROSS PROFIT", $"Rs. {_header.GrossProfit:#,##0.00}", _header.IsProfitable ? Colors.Green.Darken3 : Colors.Red.Darken2);
                kpiRow.ConstantItem(8);
                ComposeKpiCard(kpiRow.RelativeItem(), "PROFIT MARGIN", $"{_header.GrossMarginPct:F1}%", Colors.Indigo.Darken2);
                kpiRow.ConstantItem(8);
                ComposeKpiCard(kpiRow.RelativeItem(), "CUSTOMERS", _header.TotalCustomers.ToString(), Colors.Grey.Darken3);
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeKpiCard(IContainer container, string title, string value, string accentColor)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Grey.Lighten5).Padding(6).Column(c =>
        {
            c.Item().Text(title).FontSize(6.5f).Bold().FontColor(Colors.Grey.Darken1);
            c.Item().PaddingTop(1).Text(value).FontSize(9.5f).Bold().FontColor(accentColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(30);   // Sr#
                cols.RelativeColumn(3.5f); // Customer Name
                cols.RelativeColumn(1.5f); // City
                cols.ConstantColumn(50);   // Invoices
                cols.ConstantColumn(70);   // Qty Sold
                cols.ConstantColumn(95);   // Total Sales
                cols.ConstantColumn(95);   // Cost Amount
                cols.ConstantColumn(95);   // Gross Profit
                cols.ConstantColumn(60);   // Margin %
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Element(HeaderCellStyle).Text("#").Bold();
                header.Cell().Element(HeaderCellStyle).Text("Customer Title").Bold();
                header.Cell().Element(HeaderCellStyle).Text("City").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Invoices").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty Sold").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Sales (Rs.)").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Cost (Rs.)").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Profit (Rs.)").Bold();
                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Margin %").Bold();

                static IContainer HeaderCellStyle(IContainer c) =>
                    c.Background(Colors.Grey.Lighten4)
                     .BorderBottom(1.5f)
                     .BorderColor(Colors.Grey.Darken1)
                     .PaddingVertical(4)
                     .PaddingHorizontal(4);
            });

            // Rows
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                table.Cell().Element(c => CellStyle(c, bg)).Text((i + 1).ToString());
                table.Cell().Element(c => CellStyle(c, bg)).Text(item.AccountTitle).SemiBold();
                table.Cell().Element(c => CellStyle(c, bg)).Text(item.City ?? "-");
                table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text(item.InvoiceCount.ToString());
                table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text($"{item.TotalQty:#,##0.##}");
                table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text($"{item.TotalSales:#,##0.00}");
                table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text($"{item.TotalCost:#,##0.00}");
                table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text($"{item.GrossProfit:#,##0.00}")
                    .Bold()
                    .FontColor(item.GrossProfit >= 0 ? Colors.Green.Darken3 : Colors.Red.Darken2);
                table.Cell().Element(c => CellStyle(c, bg)).AlignRight().Text($"{item.GrossMarginPct:F1}%");
            }

            // Summary Totals Row
            table.Cell().ColumnSpan(3).Element(TotalCellStyle).Text("GRAND TOTALS").Bold();
            table.Cell().Element(TotalCellStyle).AlignRight().Text(_items.Sum(x => x.InvoiceCount).ToString()).Bold();
            table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_header.TotalQtySold:#,##0.##}").Bold();
            table.Cell().Element(TotalCellStyle).AlignRight().Text($"Rs. {_header.TotalSales:#,##0.00}").Bold();
            table.Cell().Element(TotalCellStyle).AlignRight().Text($"Rs. {_header.TotalCost:#,##0.00}").Bold();
            table.Cell().Element(TotalCellStyle).AlignRight().Text($"Rs. {_header.GrossProfit:#,##0.00}").Bold()
                .FontColor(_header.IsProfitable ? Colors.Green.Darken3 : Colors.Red.Darken2);
            table.Cell().Element(TotalCellStyle).AlignRight().Text($"{_header.GrossMarginPct:F1}%").Bold();

            static IContainer CellStyle(IContainer c, string bg) =>
                c.Background(bg)
                 .BorderBottom(0.5f)
                 .BorderColor(Colors.Grey.Lighten3)
                 .PaddingVertical(3.5f)
                 .PaddingHorizontal(4);

            static IContainer TotalCellStyle(IContainer c) =>
                c.Background(Colors.Grey.Lighten3)
                 .BorderTop(1.5f)
                 .BorderBottom(1.5f)
                 .BorderColor(Colors.Grey.Darken2)
                 .PaddingVertical(4.5f)
                 .PaddingHorizontal(4);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(x =>
            {
                x.Span("RetailSuite Financial Reporting  |  Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });

            row.AutoItem().Text($"Confidential — Printed: {DateTime.Now:yyyy-MM-dd HH:mm}")
                .FontSize(7.5f)
                .FontColor(Colors.Grey.Darken1);
        });
    }
}
