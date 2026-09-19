using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Code-first QuestPDF document implementing an institutional, modern financial Account Statement.
/// Follows Tier-1 corporate banking and accounting presentation standards.
/// </summary>
public class AccountStatementDocument : IDocument
{
    private readonly AccountStatementHeader _header;
    private readonly List<AccountStatementReportItem> _items;

    public AccountStatementDocument(AccountStatementHeader header, List<AccountStatementReportItem> items)
    {
        _header = header ?? new AccountStatementHeader();
        _items = items ?? new List<AccountStatementReportItem>();
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
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Top Identity & Period Header (Clean, open layout)
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(brandCol =>
                {
                    string compName = !string.IsNullOrWhiteSpace(_header?.CompanyName)
                        ? _header.CompanyName
                        : "Retail Suite Enterprise";

                    brandCol.Item().Text(compName)
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    brandCol.Item().PaddingTop(2).Text("ACCOUNT STATEMENT")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(240).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text(string.Format("Period: {0:dd-MMM-yyyy} to {1:dd-MMM-yyyy}", _header.FromDate, _header.ToDate))
                        .FontSize(8.5f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    metaCol.Item().PaddingTop(2).Text("Basis: " + (_header.DateBasis ?? "Voucher Date"))
                        .FontSize(8f)
                        .FontColor(Colors.Grey.Darken2);

                    metaCol.Item().PaddingTop(2).Text(string.Format("Generated: {0:dd MMM yyyy, HH:mm}", _header.GeneratedAt))
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // Account Title
            col.Item().PaddingTop(6).PaddingBottom(4).Row(accRow =>
            {
                accRow.RelativeItem().Text(x =>
                {
                    x.Span("Account: ").FontSize(10f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span(_header.AccountTitle ?? "N/A").FontSize(11f).Bold().FontColor(Colors.Grey.Darken4);
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(66);  // Date
                columns.ConstantColumn(62);  // Voucher No
                columns.RelativeColumn(3);   // Particulars
                columns.RelativeColumn(1);   // Debit
                columns.RelativeColumn(1);   // Credit
                columns.RelativeColumn(1);   // Balance
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Date");
                header.Cell().Element(HeaderCell).Text("Voucher #");
                header.Cell().Element(HeaderCell).Text("Particulars / Narration");
                header.Cell().Element(HeaderCell).AlignRight().Text("Debit (Dr)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Credit (Cr)");
                header.Cell().Element(HeaderCell).AlignRight().Text("Balance");
            });

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var isEven = (i % 2 == 0);
                var bg = isEven ? Colors.White : Colors.Grey.Lighten5;

                table.Cell().Element(c => BodyCell(c, bg)).Text(item.Date.ToString("dd-MMM-yyyy"));
                table.Cell().Element(c => BodyCell(c, bg)).Text(item.VoucherNo ?? string.Empty).SemiBold();
                table.Cell().Element(c => BodyCell(c, bg)).Text(item.Particular ?? string.Empty);

                if (item.Debit > 0)
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Debit.ToString("#,##0.00"));
                else
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);

                if (item.Credit > 0)
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Credit.ToString("#,##0.00"));
                else
                    table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text("-").FontColor(Colors.Grey.Lighten1);

                table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(item.Balance.ToString("#,##0.00")).SemiBold();
            }

            // Table Summary Totals Row
            decimal totalDr = _items.Sum(x => x.Debit);
            decimal totalCr = _items.Sum(x => x.Credit);
            decimal finalBal = _items.Count > 0 ? _items.Last().Balance : _header.ClosingBalance;

            table.Cell().ColumnSpan(3).Element(TotalCell).Text("TOTALS & NET MOVEMENT").Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(totalDr.ToString("#,##0.00")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(totalCr.ToString("#,##0.00")).Bold();
            table.Cell().Element(TotalCell).AlignRight().Text(finalBal.ToString("#,##0.00")).Bold();
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
                    x.Span("Confidential • RetailSuite ERP Reporting System • Computer-Generated Document")
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
