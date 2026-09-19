using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Trial Balance (General Ledger).
/// Generated in-memory using QuestPDF and Lato font.
/// </summary>
public class TrialBalanceDocument : IDocument
{
    private readonly TrialBalanceHeader _header;
    private readonly List<TrialBalanceReportItem> _items;

    public TrialBalanceDocument(TrialBalanceHeader header, List<TrialBalanceReportItem> items)
    {
        _header = header ?? new TrialBalanceHeader();
        _items = items ?? new List<TrialBalanceReportItem>();
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

                    brandCol.Item().PaddingTop(2).Text("TRIAL BALANCE (GENERAL LEDGER)")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);
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

                    // Audit Balancing Indicator Badge
                    decimal diff = Math.Abs(_header.TotalDebit - _header.TotalCredit);
                    if (diff < 0.01m)
                    {
                        metaCol.Item().PaddingTop(4).Text("✓ BALANCED (Diff: 0.00)")
                            .FontSize(8f)
                            .Bold()
                            .FontColor(Colors.Green.Darken3);
                    }
                    else
                    {
                        metaCol.Item().PaddingTop(4).Text($"⚠ OUT OF BALANCE: {diff:#,##0.00}")
                            .FontSize(8f)
                            .Bold()
                            .FontColor(Colors.Red.Darken2);
                    }
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // Financial Summary KPI Strip
            col.Item().PaddingTop(6).PaddingBottom(4).Row(kpiRow =>
            {
                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Accounts: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span(_items.Count.ToString()).FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Debits: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalDebit.ToString("#,##0.00")).FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Credits: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span("Rs. " + _header.TotalCredit.ToString("#,##0.00")).FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken4);
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
                columns.ConstantColumn(75);   // Account Code
                columns.RelativeColumn(3.5f); // Account Title
                columns.ConstantColumn(70);   // Opening Balance
                columns.ConstantColumn(75);   // Period Debit
                columns.ConstantColumn(75);   // Period Credit
                columns.ConstantColumn(80);   // Closing Balance
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Code");
                header.Cell().Element(HeaderCell).Text("Account Title / Head");
                header.Cell().Element(HeaderCell).AlignRight().Text("Opening");
                header.Cell().Element(HeaderCell).AlignRight().Text("Debit (Dr)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Credit (Cr)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Closing Balance");
            });

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var isEven = (i % 2 == 0);
                var bg = isEven ? Colors.White : Colors.Grey.Lighten5;

                table.Cell().Element(c => BodyCell(c, bg)).Text(item.AccountCode ?? string.Empty).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                table.Cell().Element(c => BodyCell(c, bg)).Text(item.AccountTitle ?? string.Empty).SemiBold();

                // Opening
                if (item.OpeningBalance != 0)
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.OpeningBalance.ToString("#,##0.00"));
                else
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);

                // Period Debit
                if (item.Debit > 0)
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Debit.ToString("#,##0.00"));
                else
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);

                // Period Credit
                if (item.Credit > 0)
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Credit.ToString("#,##0.00"));
                else
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);

                // Closing Balance
                string balFormatted = item.ClosingBalance.ToString("#,##0.00");
                if (item.ClosingBalance < 0)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text($"({Math.Abs(item.ClosingBalance):#,##0.00})").SemiBold().FontColor(Colors.Grey.Darken3);
                }
                else if (item.ClosingBalance > 0)
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(balFormatted).SemiBold();
                }
                else
                {
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);
                }
            }

            // Table Summary Totals Row
            decimal totalOpening = _items.Sum(x => x.OpeningBalance);
            decimal totalDr = _items.Sum(x => x.Debit);
            decimal totalCr = _items.Sum(x => x.Credit);
            decimal totalClosing = _items.Sum(x => x.ClosingBalance);

            table.Cell().ColumnSpan(2).Element(TotalCell).Text("TOTAL SUMMARY & RECONCILIATION").Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(totalOpening.ToString("#,##0.00")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(totalDr.ToString("#,##0.00")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(totalCr.ToString("#,##0.00")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(totalClosing.ToString("#,##0.00")).Bold();
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
            .PaddingHorizontal(5)
            .DefaultTextStyle(x => x.Bold().FontColor(Colors.Grey.Darken3).FontSize(7.5f));
    }

    private static IContainer BodyCell(IContainer cell, string bg)
    {
        return cell.Background(bg)
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(4)
            .PaddingHorizontal(5)
            .DefaultTextStyle(x => x.FontSize(8f));
    }

    private static IContainer TotalCell(IContainer cell)
    {
        return cell.Background(Colors.Grey.Lighten5)
            .BorderTop(1f)
            .BorderColor(Colors.Grey.Medium)
            .BorderBottom(2f)
            .BorderColor(Colors.Grey.Medium)
            .PaddingVertical(6)
            .PaddingHorizontal(5)
            .DefaultTextStyle(x => x.Bold().FontSize(8f).FontColor(Colors.Grey.Darken3));
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(x =>
                {
                    x.Span("Confidential • RetailSuite General Ledger • Computer-Generated Financial Statement")
                        .FontSize(7f)
                        .FontColor(Colors.Grey.Darken1);
                });

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
