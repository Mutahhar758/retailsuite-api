using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Milk / Purchase vs Supply Comparison.
/// Landscape A4 layout generated in-memory using QuestPDF and Lato font.
/// </summary>
public class MilkComparisonDocument : IDocument
{
    private readonly MilkComparisonHeader _header;
    private readonly List<PurchaseSupplyComparisonLineResponse> _items;

    public MilkComparisonDocument(MilkComparisonHeader header, List<PurchaseSupplyComparisonLineResponse> items)
    {
        _header = header ?? new MilkComparisonHeader();
        _items = items ?? new List<PurchaseSupplyComparisonLineResponse>();
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

                    brandCol.Item().PaddingTop(2).Text("PURCHASE VS SUPPLY COMPARISON (MILK / COMMODITY)")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(280).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}")
                        .FontSize(8.5f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    metaCol.Item().PaddingTop(2).Text($"Item: {_header.ItemTitle} ({_header.UnitTitle})")
                        .FontSize(8f)
                        .FontColor(Colors.Grey.Darken2);

                    metaCol.Item().PaddingTop(2).Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // KPI Summary Strip
            col.Item().PaddingTop(6).PaddingBottom(6).Row(kpiRow =>
            {
                var s = _header.Summary;

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Purchased: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"{s.TotalPurchaseQty:N0} {_header.UnitTitle}").FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken2);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Dispatched: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"{s.TotalDispatchedQty:N0} {_header.UnitTitle}").FontSize(9.5f).Bold().FontColor(Colors.Green.Darken2);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Net Variance: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"{s.TotalNetDiffQty:N0} {_header.UnitTitle}").FontSize(9.5f).Bold()
                        .FontColor(s.TotalNetDiffQty < 0 ? Colors.Red.Darken2 : (s.TotalNetDiffQty > 0 ? Colors.Purple.Darken2 : Colors.Green.Darken2));
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Avg Rates (P / S): ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {s.AvgPurchaseRate:N1} / Rs. {s.AvgSupplyRate:N1}").FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken3);
                });
            });

            col.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.3f); // Date & Day
                columns.RelativeColumn(1.1f); // Purchase Qty
                columns.RelativeColumn(1.0f); // Purchase Rate
                columns.RelativeColumn(1.3f); // Purchase Amt
                columns.RelativeColumn(1.1f); // Supply Qty
                columns.RelativeColumn(1.0f); // Supply Rate
                columns.RelativeColumn(1.3f); // Supply Amt
                columns.RelativeColumn(1.0f); // Counter Sale
                columns.RelativeColumn(1.2f); // Total Dispatched
                columns.RelativeColumn(1.1f); // Net Diff
                columns.RelativeColumn(1.0f); // Status
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Element(BlockHeader).Text("Date / Day").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Purch Qty").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Purch Rate").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Purch Amt").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Supply Qty").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Supply Rate").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Supply Amt").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Sale Qty").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Total Out").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Variance").SemiBold();
                header.Cell().Element(BlockHeader).AlignCenter().Text("Status").SemiBold();
            });

            // Rows
            int index = 0;
            foreach (var item in _items)
            {
                index++;
                bool isAlt = index % 2 == 0;

                table.Cell().Element(c => BlockCell(c, isAlt)).Text(x =>
                {
                    x.Span($"{item.Date:dd-MMM} ").SemiBold();
                    x.Span($"({item.DayName})").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                });

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.PurchaseQty == 0 ? "-" : $"{item.PurchaseQty:N0}");
                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.PurchaseAvgRate == 0 ? "-" : $"{item.PurchaseAvgRate:N1}");
                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.PurchaseAmount == 0 ? "-" : $"{item.PurchaseAmount:N0}").SemiBold();

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.SupplyQty == 0 ? "-" : $"{item.SupplyQty:N0}");
                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.SupplyAvgRate == 0 ? "-" : $"{item.SupplyAvgRate:N1}");
                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.SupplyAmount == 0 ? "-" : $"{item.SupplyAmount:N0}").SemiBold();

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.RegularSaleQty == 0 ? "-" : $"{item.RegularSaleQty:N0}");
                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.TotalDispatchedQty == 0 ? "-" : $"{item.TotalDispatchedQty:N0}").Bold();

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text(item.NetDiffQty == 0 ? "0" : $"{item.NetDiffQty:N0}").Bold()
                    .FontColor(item.NetDiffQty < 0 ? Colors.Red.Darken2 : (item.NetDiffQty > 0 ? Colors.Purple.Darken2 : Colors.Green.Darken2));

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignCenter().Text(item.Status).SemiBold()
                    .FontColor(item.Status == "Balanced" ? Colors.Green.Darken2 : (item.Status == "Surplus" ? Colors.Purple.Darken2 : Colors.Red.Darken2));
            }

            // Summary Totals Row
            var s = _header.Summary;
            table.Cell().Element(BlockTotal).Text("Total").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.TotalPurchaseQty:N0}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.AvgPurchaseRate:N1}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.TotalPurchaseAmount:N0}").Bold().FontColor(Colors.Blue.Darken3);

            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.TotalSupplyQty:N0}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.AvgSupplyRate:N1}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.TotalSupplyAmount:N0}").Bold().FontColor(Colors.Green.Darken3);

            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.TotalRegularSaleQty:N0}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.TotalDispatchedQty:N0}").Bold();

            table.Cell().Element(BlockTotal).AlignRight().Text($"{s.TotalNetDiffQty:N0}").Bold()
                .FontColor(s.TotalNetDiffQty < 0 ? Colors.Red.Darken3 : Colors.Grey.Darken4);
            table.Cell().Element(BlockTotal).AlignCenter().Text("");
        });
    }

    private static IContainer BlockHeader(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(5)
            .PaddingHorizontal(4);
    }

    private static IContainer BlockCell(IContainer container, bool isAlt)
    {
        var result = container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(4);

        if (isAlt)
            result = result.Background(Colors.Grey.Lighten4);

        return result;
    }

    private static IContainer BlockTotal(IContainer container)
    {
        return container
            .BorderTop(1.5f)
            .BorderBottom(2f)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(6)
            .PaddingHorizontal(4);
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(x =>
            {
                x.Span("RetailSuite").SemiBold().FontColor(Colors.Grey.Darken1);
                x.Span(" • Purchase vs Supply Comparison (Milk)").FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        });
    }
}
