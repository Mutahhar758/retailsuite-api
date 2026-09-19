using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Stock Balance & Inventory Valuation.
/// Landscape A4 layout generated in-memory using QuestPDF and Lato font.
/// </summary>
public class StockBalanceDocument : IDocument
{
    private readonly StockBalanceHeader _header;
    private readonly List<StockBalanceReportItem> _items;

    public StockBalanceDocument(StockBalanceHeader header, List<StockBalanceReportItem> items)
    {
        _header = header ?? new StockBalanceHeader();
        _items = items ?? new List<StockBalanceReportItem>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(26, Unit.Point);
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

                    brandCol.Item().PaddingTop(2).Text("STOCK BALANCE & INVENTORY VALUATION")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(260).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}")
                        .FontSize(8.5f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    metaCol.Item().PaddingTop(2).Text("Category: " + (_header.CategoryName ?? "All Categories"))
                        .FontSize(8f)
                        .FontColor(Colors.Grey.Darken2);

                    metaCol.Item().PaddingTop(2).Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // Financial Summary KPI Strip
            col.Item().PaddingTop(6).PaddingBottom(6).Row(kpiRow =>
            {
                decimal totalVal = _items.Sum(x => x.TotalValue);
                decimal totalQty = _items.Sum(x => x.ClosingQty);
                decimal totalIn = _items.Sum(x => x.QtyIn);
                decimal totalOut = _items.Sum(x => x.QtyOut);

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total SKUs: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span(_items.Count.ToString()).FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Inward: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span(totalIn.ToString("#,##0")).FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken2);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Outward: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span(totalOut.ToString("#,##0")).FontSize(9.5f).Bold().FontColor(Colors.Orange.Darken3);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Closing Units: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span(totalQty.ToString("#,##0")).FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Inventory Valuation: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + totalVal.ToString("#,##0.00")).FontSize(10.5f).Bold().FontColor(Colors.Green.Darken3);
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);  // Index #
                columns.RelativeColumn(3.5f);// Item Description
                columns.ConstantColumn(46);  // Unit
                columns.ConstantColumn(64);  // Opening Qty
                columns.ConstantColumn(64);  // Qty In
                columns.ConstantColumn(64);  // Qty Out
                columns.ConstantColumn(70);  // Balance Qty
                columns.ConstantColumn(64);  // Rate
                columns.ConstantColumn(80);  // Stock Value
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).AlignCenter().Text("#");
                header.Cell().Element(HeaderCell).Text("Item / Product Name");
                header.Cell().Element(HeaderCell).AlignCenter().Text("Unit");
                header.Cell().Element(HeaderCell).AlignRight().Text("Opening");
                header.Cell().Element(HeaderCell).AlignRight().Text("Inward (+)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Outward (-)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Closing Qty");
                header.Cell().Element(HeaderCell).AlignRight().Text("Rate");
                header.Cell().Element(HeaderCell).AlignRight().Text("Total Value");
            });

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var isEven = (i % 2 == 0);
                var bg = isEven ? Colors.White : Colors.Grey.Lighten5;

                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text((i + 1).ToString()).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                table.Cell().Element(c => BodyCell(c, bg)).Text(item.ItemName ?? string.Empty).SemiBold();
                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text(item.Unit ?? "Pcs");

                // Opening
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.OpeningQty.ToString("#,##0.##"));

                // Inward
                if (item.QtyIn > 0)
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.QtyIn.ToString("#,##0.##")).FontColor(Colors.Blue.Darken2);
                else
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);

                // Outward
                if (item.QtyOut > 0)
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.QtyOut.ToString("#,##0.##")).FontColor(Colors.Orange.Darken3);
                else
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);

                // Closing Qty
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.ClosingQty.ToString("#,##0.##")).SemiBold();

                // Rate
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Rate.ToString("#,##0.00"));

                // Total Value
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.TotalValue.ToString("#,##0.00")).SemiBold();
            }

            // Summary Totals Row
            decimal sumOpening = _items.Sum(x => x.OpeningQty);
            decimal sumIn = _items.Sum(x => x.QtyIn);
            decimal sumOut = _items.Sum(x => x.QtyOut);
            decimal sumBal = _items.Sum(x => x.ClosingQty);
            decimal sumVal = _items.Sum(x => x.TotalValue);

            table.Cell().ColumnSpan(3).Element(TotalCell).Text("TOTAL SUMMARY").Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(sumOpening.ToString("#,##0.##")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(sumIn.ToString("#,##0.##")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(sumOut.ToString("#,##0.##")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(sumBal.ToString("#,##0.##")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text("-").FontColor(Colors.Grey.Darken1);
            table.Cell().Element(TotalCell).AlignRight().Text("Rs. " + sumVal.ToString("#,##0.00")).Bold();
        });
    }

    private static IContainer HeaderCell(IContainer cell)
    {
        return cell.Background(Colors.Grey.Lighten4)
            .BorderTop(1f)
            .BorderColor(Colors.Grey.Lighten2)
            .BorderBottom(1.5f)
            .BorderColor(Colors.Grey.Medium)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.Bold().FontColor(Colors.Grey.Darken3).FontSize(7.5f));
    }

    private static IContainer BodyCell(IContainer cell, string bg)
    {
        return cell.Background(bg)
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(7.5f));
    }

    private static IContainer TotalCell(IContainer cell)
    {
        return cell.Background(Colors.Grey.Lighten5)
            .BorderTop(1f)
            .BorderColor(Colors.Grey.Medium)
            .BorderBottom(2f)
            .BorderColor(Colors.Grey.Medium)
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.Bold().FontSize(8f).FontColor(Colors.Grey.Darken3));
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
                    x.CurrentPageNumber().FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken3);
                    x.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    x.TotalPages().FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken3);
                });
            });
        });
    }
}
