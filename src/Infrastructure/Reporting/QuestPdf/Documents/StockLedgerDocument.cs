using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Item Ledger / Stock Movement.
/// Generated in-memory using QuestPDF and Lato font.
/// </summary>
public class StockLedgerDocument : IDocument
{
    private readonly StockLedgerHeader _header;
    private readonly List<StockLedgerReportItem> _items;

    public StockLedgerDocument(StockLedgerHeader header, List<StockLedgerReportItem> items)
    {
        _header = header ?? new StockLedgerHeader();
        _items = items ?? new List<StockLedgerReportItem>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30, Unit.Point);
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

                    brandCol.Item().PaddingTop(2).Text("ITEM LEDGER / STOCK MOVEMENT")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);

                    brandCol.Item().PaddingTop(3).Row(tagRow =>
                    {
                        tagRow.AutoItem().Text("Item: ")
                            .FontSize(9)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken2);

                        tagRow.AutoItem().Text(_header.ItemTitle ?? "All Items")
                            .FontSize(9.5f)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);
                    });
                });

                row.ConstantItem(240).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}")
                        .FontSize(8.5f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    metaCol.Item().PaddingTop(2).Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);


                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // KPI Metric Summary Strip
            col.Item().PaddingTop(6).PaddingBottom(4).Row(kpiRow =>
            {
                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Opening Stock: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span(_header.OpeningBalance.ToString("#,##0.00")).FontSize(9f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Inward (+): ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span(_header.TotalIn.ToString("#,##0.00")).FontSize(9f).Bold().FontColor(Colors.Green.Darken3);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Outward (-): ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span(_header.TotalOut.ToString("#,##0.00")).FontSize(9f).Bold().FontColor(Colors.Red.Darken2);
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Closing Balance: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span(_header.ClosingBalance.ToString("#,##0.00")).FontSize(9f).Bold().FontColor(Colors.Blue.Darken3);
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(68);   // Date
                columns.ConstantColumn(75);   // Voucher #
                columns.RelativeColumn(3.5f); // Particular
                columns.ConstantColumn(60);   // Rate
                columns.ConstantColumn(65);   // Qty In
                columns.ConstantColumn(65);   // Qty Out
                columns.ConstantColumn(75);   // Balance Qty
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).AlignCenter().Text("Date");
                header.Cell().Element(HeaderCell).Text("Voucher #");
                header.Cell().Element(HeaderCell).Text("Particular / Narrative");
                header.Cell().Element(HeaderCell).AlignRight().Text("Rate");
                header.Cell().Element(HeaderCell).AlignRight().Text("Inward (+)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Outward (-)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Balance");
            });

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var isEven = (i % 2 == 0);
                var bg = isEven ? Colors.White : Colors.Grey.Lighten5;

                bool isOpeningRow = item.VoucherNo == "-";

                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text(item.Date.ToString("dd-MMM-yyyy")).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                table.Cell().Element(c => BodyCell(c, bg)).Text(item.VoucherNo ?? string.Empty).Bold();
                table.Cell().Element(c => BodyCell(c, bg)).Text(item.Particular ?? string.Empty).SemiBold();

                // Rate
                string rateStr = item.Rate.HasValue && item.Rate.Value > 0 ? item.Rate.Value.ToString("N2") : "-";
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(rateStr).FontColor(Colors.Grey.Darken3);

                // Qty In
                if (item.QtyIn > 0 && !isOpeningRow)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.QtyIn.ToString("#,##0.00")).FontColor(Colors.Green.Darken3);
                }
                else if (item.QtyIn > 0 && isOpeningRow)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.QtyIn.ToString("#,##0.00")).FontColor(Colors.Grey.Darken4);
                }
                else
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);
                }

                // Qty Out
                if (item.QtyOut > 0)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.QtyOut.ToString("#,##0.00")).FontColor(Colors.Red.Darken2);
                }
                else
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);
                }

                // Running Balance
                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Balance.ToString("#,##0.00")).Bold().FontColor(Colors.Grey.Darken4);
            }

            // Summary Totals Row
            table.Cell().ColumnSpan(4).Element(FooterTotalCell).Text("PERIOD MOVEMENT TOTALS").Bold().FontColor(Colors.Grey.Darken4);
            table.Cell().Element(FooterTotalCell).AlignRight().Text(_header.TotalIn.ToString("#,##0.00")).Bold().FontColor(Colors.Green.Darken3);
            table.Cell().Element(FooterTotalCell).AlignRight().Text(_header.TotalOut.ToString("#,##0.00")).Bold().FontColor(Colors.Red.Darken2);
            table.Cell().Element(FooterTotalCell).AlignRight().Text(_header.ClosingBalance.ToString("#,##0.00")).Bold().FontColor(Colors.Blue.Darken3);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("Software powered by Bizgrip Solutions contact 03228258734")
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

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.SemiBold().FontSize(8).FontColor(Colors.Grey.Darken3));
    }

    private static IContainer BodyCell(IContainer container, string backgroundColor)
    {
        return container
            .Background(backgroundColor)
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(3.5f)
            .PaddingHorizontal(4);
    }

    private static IContainer FooterTotalCell(IContainer container)
    {
        return container
            .BorderTop(1.5f)
            .BorderColor(Colors.Grey.Darken3)
            .BorderBottom(2.5f)
            .BorderColor(Colors.Grey.Darken3)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(8.5f));
    }
}
