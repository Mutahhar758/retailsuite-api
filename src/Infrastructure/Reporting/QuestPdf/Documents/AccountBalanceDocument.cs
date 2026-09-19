using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Account Balance (Balance Detail Schedule).
/// Generated in-memory using QuestPDF and Lato font.
/// </summary>
public class AccountBalanceDocument : IDocument
{
    private readonly AccountBalanceHeader _header;
    private readonly List<AccountBalanceReportItem> _items;

    public AccountBalanceDocument(AccountBalanceHeader header, List<AccountBalanceReportItem> items)
    {
        _header = header ?? new AccountBalanceHeader();
        _items = items ?? new List<AccountBalanceReportItem>();
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

                    brandCol.Item().PaddingTop(2).Text("ACCOUNT BALANCE (DETAIL SCHEDULE)")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);

                    brandCol.Item().PaddingTop(3).Row(tagRow =>
                    {
                        tagRow.AutoItem().Text("Account Head: ")
                            .FontSize(9)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken2);

                        tagRow.AutoItem().Text(_header.AccountHeadTitle ?? "All Heads")
                            .FontSize(9.5f)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);
                    });
                });

                row.ConstantItem(240).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"As On: {_header.AsOnDate:dd-MMM-yyyy}")
                        .FontSize(9.5f)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    metaCol.Item().PaddingTop(2).Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);

                    metaCol.Item().PaddingTop(4).Text($"Active Accounts: {_items.Count:N0}")
                        .FontSize(8f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // KPI Metric Summary Strip
            col.Item().PaddingTop(6).PaddingBottom(4).Row(kpiRow =>
            {
                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Accounts: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span(_items.Count.ToString()).FontSize(9f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Debit (Dr): ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalDebit.ToString("#,##0.00")).FontSize(9f).Bold().FontColor(Colors.Green.Darken3);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Credit (Cr): ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalCredit.ToString("#,##0.00")).FontSize(9f).Bold().FontColor(Colors.Red.Darken2);
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Net Position: ").FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                    string sign = _header.NetNature;
                    decimal netAbs = Math.Abs(_header.NetBalance);
                    x.Span("Rs. " + netAbs.ToString("#,##0.00") + " " + sign).FontSize(9f).Bold().FontColor(Colors.Blue.Darken3);
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
                columns.ConstantColumn(35);   // #
                columns.RelativeColumn(4.5f); // Account / Customer / Vendor Title
                columns.ConstantColumn(95);   // Debit (Dr)
                columns.ConstantColumn(95);   // Credit (Cr)
                columns.ConstantColumn(50);   // Nature (Dr/Cr)
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).AlignCenter().Text("#");
                header.Cell().Element(HeaderCell).Text("Account Title / Subsidiary");
                header.Cell().Element(HeaderCell).AlignRight().Text("Debit (Dr)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Credit (Cr)");
                header.Cell().Element(HeaderCell).AlignCenter().Text("Type");
            });

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var isEven = (i % 2 == 0);
                var bg = isEven ? Colors.White : Colors.Grey.Lighten5;

                table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text((i + 1).ToString()).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                table.Cell().Element(c => BodyCell(c, bg)).Text(item.AccountTitle ?? string.Empty).SemiBold();

                // Debit column
                if (item.Debit > 0)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Debit.ToString("#,##0.00")).FontColor(Colors.Grey.Darken4);
                }
                else
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);
                }

                // Credit column
                if (item.Credit > 0)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Credit.ToString("#,##0.00")).FontColor(Colors.Grey.Darken4);
                }
                else
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);
                }

                // Nature indicator
                if (item.Balance >= 0)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text("Dr")
                        .FontSize(7.5f).Bold().FontColor(Colors.Green.Darken3);
                }
                else
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text("Cr")
                        .FontSize(7.5f).Bold().FontColor(Colors.Red.Darken2);
                }
            }

            // Grand Totals Summary Row
            table.Cell().ColumnSpan(2).Element(FooterTotalCell).Text("GRAND TOTALS").Bold().FontColor(Colors.Grey.Darken4);
            table.Cell().Element(FooterTotalCell).AlignRight().Text("Rs. " + _header.TotalDebit.ToString("#,##0.00")).Bold().FontColor(Colors.Grey.Darken4);
            table.Cell().Element(FooterTotalCell).AlignRight().Text("Rs. " + _header.TotalCredit.ToString("#,##0.00")).Bold().FontColor(Colors.Grey.Darken4);
            table.Cell().Element(FooterTotalCell).AlignCenter().Text(_header.NetNature).Bold().FontColor(Colors.Blue.Darken3);
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
