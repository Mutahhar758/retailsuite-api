using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Vendor Bill & Statement.
/// Supports both A4 Sheet layout and 80mm Thermal continuous roll receipt layout.
/// </summary>
public class VendorBillDocument : IDocument
{
    private readonly VendorBillHeader _header;
    private readonly List<VendorBillLineResponse> _items;

    public VendorBillDocument(VendorBillHeader header, List<VendorBillLineResponse> items)
    {
        _header = header ?? new VendorBillHeader();
        _items = items ?? new List<VendorBillLineResponse>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        if (_header.Layout == VendorBillPrintLayout.Thermal80mm)
        {
            ComposeThermal80(container);
        }
        else
        {
            ComposeA4(container);
        }
    }

    #region 80mm Thermal Receipt Layout

    private void ComposeThermal80(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(80, Unit.Millimetre);
            page.MarginVertical(2, Unit.Millimetre);
            page.MarginLeft(2.5f, Unit.Millimetre);
            page.MarginRight(8.5f, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily(Fonts.Arial).FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                // 1. Store Header
                col.Item().AlignCenter().Text(_header.CompanyName).FontSize(12f).Bold();

                if (!string.IsNullOrWhiteSpace(_header.CompanyAddress))
                {
                    col.Item().AlignCenter().PaddingTop(1).Text(_header.CompanyAddress).FontSize(8f).SemiBold();
                }

                if (!string.IsNullOrWhiteSpace(_header.CompanyPhone))
                {
                    col.Item().AlignCenter().PaddingTop(1).Text($"Tel: {_header.CompanyPhone}").FontSize(8f).SemiBold();
                }

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // 2. Receipt Title
                col.Item().AlignCenter().Text("VENDOR BILL / STATEMENT").FontSize(9f).Bold();

                // 3. Vendor & Meta
                col.Item().PaddingTop(2).Row(r =>
                {
                    r.AutoItem().Text("Vendor: ").Bold().FontSize(8.5f);
                    r.RelativeItem().Text(_header.VendorTitle).Bold().FontSize(8.5f);
                });

                col.Item().Row(r =>
                {
                    r.AutoItem().Text("Period: ").FontSize(8f).SemiBold();
                    r.RelativeItem().Text($"{_header.FromDate:dd/MM/yy} to {_header.ToDate:dd/MM/yy}").FontSize(8f).SemiBold();
                });

                col.Item().Row(r =>
                {
                    r.AutoItem().Text("Printed: ").FontSize(8f).SemiBold();
                    r.RelativeItem().Text($"{_header.GeneratedAt:dd-MMM-yy HH:mm}").FontSize(8f).SemiBold();
                });

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // 4. Line Items Table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.4f); // Item & Date
                        columns.RelativeColumn(1.1f); // Qty & Unit
                        columns.RelativeColumn(1.0f); // Rate
                        if (_header.EnableCarriage)
                            columns.RelativeColumn(1.0f);
                        columns.RelativeColumn(0.9f); // Adj
                        columns.RelativeColumn(1.4f); // Total
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ITEM").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("QTY").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("RATE").Bold().FontSize(8f);
                        if (_header.EnableCarriage)
                            header.Cell().AlignRight().Text("CARRIAGE").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("ADJ").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("TOTAL").Bold().FontSize(8.5f);

                        header.Cell().ColumnSpan((uint)(_header.EnableCarriage ? 6 : 5)).PaddingVertical(1).LineHorizontal(0.75f).LineColor(Colors.Black);
                    });

                    if (_items.Count > 0)
                    {
                        foreach (var line in _items)
                        {
                            table.Cell().PaddingVertical(1.2f).Text(t =>
                            {
                                t.Span($"{line.Date:dd/MM} ").FontSize(8.5f).SemiBold().FontColor(Colors.Black);
                                t.Span(line.Item).FontSize(8.5f).SemiBold();
                                if (!string.IsNullOrWhiteSpace(line.Imei))
                                {
                                    t.EmptyLine();
                                    t.Span($"  IMEI: {line.Imei}").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                                }
                            });
                            var qtyText = string.IsNullOrWhiteSpace(line.UnitTitle)
                                ? line.Qty.ToString("#,##0.##")
                                : $"{line.Qty:#,##0.##} {line.UnitTitle}";
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(qtyText).FontSize(8.5f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text($"{line.Rate:N0}").FontSize(9f).SemiBold();
                            if (_header.EnableCarriage)
                                table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.Carriage != 0 ? $"{line.Carriage:N0}" : "-").FontSize(9f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.AddLess != 0 ? $"{line.AddLess:N0}" : "-").FontSize(9f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.Amount.ToString("#,##0;(#,##0);0")).FontSize(9.5f).SemiBold();
                        }
                    }
                    else
                    {
                        table.Cell().ColumnSpan((uint)(_header.EnableCarriage ? 6 : 5)).AlignCenter().PaddingVertical(3).Text("No line transactions in period.").FontSize(8.5f).SemiBold();
                    }
                });

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // 5. Financial Summary
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Previous Balance:").FontSize(9f).SemiBold();
                    r.AutoItem().Text($"{_header.PreviousBalance:N0}").FontSize(9.5f).SemiBold();
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Current Purchases:").FontSize(9f).SemiBold();
                    r.AutoItem().Text($"{_header.TotalBilling:N0}").FontSize(9.5f).SemiBold();
                });

                if (_header.Payment != 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Payments Made:").FontSize(9f).SemiBold();
                        r.AutoItem().Text($"({_header.Payment:N0})").FontSize(9.5f).SemiBold();
                    });
                }

                col.Item().PaddingTop(1).LineHorizontal(0.75f).LineColor(Colors.Black);

                // Major focal point: NET PAYABLE BALANCE in Bold
                col.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text("NET PAYABLE:").Bold().FontSize(11f);
                    r.AutoItem().Text($"Rs. {_header.ClosingBalance:N0}").Bold().FontSize(12f);
                });

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);
                col.Item().AlignCenter().PaddingTop(1).Text("Software powered by Bizgrip Solutions").FontSize(6f).SemiBold().FontColor(Colors.Black);
            });
        });
    }

    #endregion

    #region A4 Sheet Layout

    private void ComposeA4(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily(Fonts.Lato).FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeA4Header);
            page.Content().Element(ComposeA4Content);
            page.Footer().Element(ComposeA4Footer);
        });
    }

    private void ComposeA4Header(IContainer container)
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

                    if (!string.IsNullOrWhiteSpace(_header.CompanyAddress))
                    {
                        brandCol.Item().PaddingTop(2).Text(_header.CompanyAddress)
                            .FontSize(8f)
                            .FontColor(Colors.Grey.Darken1);
                    }

                    if (!string.IsNullOrWhiteSpace(_header.CompanyPhone))
                    {
                        brandCol.Item().PaddingTop(1).Text($"Contact: {_header.CompanyPhone}")
                            .FontSize(8f)
                            .FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(260).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text("VENDOR BILL & STATEMENT")
                        .FontSize(13)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    metaCol.Item().PaddingTop(3).Text($"Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}")
                        .FontSize(8.5f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken2);

                    metaCol.Item().PaddingTop(2).Text($"Basis: {_header.DateBasis}")
                        .FontSize(8f)
                        .FontColor(Colors.Grey.Darken2);

                    metaCol.Item().PaddingTop(1).Text($"Issue Date: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

            // Vendor Info Bar
            col.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(8).Row(custRow =>
            {
                custRow.RelativeItem().Column(ci =>
                {
                    ci.Item().Text(t =>
                    {
                        t.Span("Vendor: ").Bold().FontSize(9f).FontColor(Colors.Grey.Darken3);
                        t.Span(_header.VendorTitle).Bold().FontSize(9.5f).FontColor(Colors.Blue.Darken3);
                        t.Span($"  [Code: {_header.VendorAccount}]").FontSize(8f).FontColor(Colors.Grey.Darken1);
                    });

                    if (!string.IsNullOrWhiteSpace(_header.VendorAddress))
                    {
                        ci.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("Address: ").SemiBold().FontSize(8f).FontColor(Colors.Grey.Darken2);
                            t.Span(_header.VendorAddress).FontSize(8f).FontColor(Colors.Grey.Darken2);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(_header.VendorPhone))
                    {
                        ci.Item().PaddingTop(1).Text(t =>
                        {
                            t.Span("Phone: ").SemiBold().FontSize(8f).FontColor(Colors.Grey.Darken2);
                            t.Span(_header.VendorPhone).FontSize(8f).FontColor(Colors.Grey.Darken2);
                        });
                    }
                });

                custRow.ConstantItem(180).AlignRight().Column(balCol =>
                {
                    balCol.Item().Text("Previous Balance:").FontSize(8f).FontColor(Colors.Grey.Darken2);
                    balCol.Item().Text($"Rs. {_header.PreviousBalance:N0}").Bold().FontSize(11f).FontColor(Colors.Grey.Darken3);
                });
            });
        });
    }

    private void ComposeA4Content(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(32);   // SR#
                    columns.ConstantColumn(68);   // Date
                    columns.ConstantColumn(75);   // VNo
                    columns.RelativeColumn(3f);   // Description
                    columns.ConstantColumn(45);   // Unit
                    columns.ConstantColumn(55);   // Qty
                    columns.ConstantColumn(60);   // Rate
                    if (_header.EnableCarriage)
                        columns.ConstantColumn(50); // Carriage
                    columns.ConstantColumn(50);   // Add/Less
                    columns.ConstantColumn(75);   // Amount
                });

                table.Header(header =>
                {
                    IContainer HeaderCell(IContainer c) =>
                        c.Background(Colors.Blue.Darken3).PaddingVertical(4).PaddingHorizontal(3);

                    header.Cell().Element(HeaderCell).AlignCenter().Text("#").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("DATE").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("VOUCHER #").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("ITEM / DESCRIPTION").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).AlignCenter().Text("UNIT").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).AlignRight().Text("QTY").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).AlignRight().Text("RATE").Bold().FontSize(7.5f).FontColor(Colors.White);
                    if (_header.EnableCarriage)
                        header.Cell().Element(HeaderCell).AlignRight().Text("CARRIAGE").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).AlignRight().Text("ADJ").Bold().FontSize(7.5f).FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).AlignRight().Text("AMOUNT").Bold().FontSize(7.5f).FontColor(Colors.White);
                });

                if (_items.Count > 0)
                {
                    int sr = 1;
                    foreach (var line in _items)
                    {
                        var bg = (sr % 2 == 0) ? Colors.Grey.Lighten4 : Colors.White;
                        IContainer BodyCell(IContainer c) =>
                            c.Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3);

                        table.Cell().Element(BodyCell).AlignCenter().Text(sr.ToString()).FontSize(7.5f);
                        table.Cell().Element(BodyCell).Text($"{line.Date:dd-MMM-yyyy}").FontSize(7.5f);
                        table.Cell().Element(BodyCell).Text(line.VNo).FontSize(7.5f).SemiBold();
                        table.Cell().Element(BodyCell).Text(t =>
                        {
                            t.Span(line.Item).FontSize(8f).SemiBold();
                            if (!string.IsNullOrWhiteSpace(line.Imei))
                                t.Span($" [IMEI: {line.Imei}]").FontSize(7f).FontColor(Colors.Grey.Darken2);
                        });
                        table.Cell().Element(BodyCell).AlignCenter().Text(line.UnitTitle).FontSize(7.5f);
                        table.Cell().Element(BodyCell).AlignRight().Text(line.Qty.ToString("#,##0.##")).FontSize(7.5f);
                        table.Cell().Element(BodyCell).AlignRight().Text($"{line.Rate:N0}").FontSize(7.5f);
                        if (_header.EnableCarriage)
                            table.Cell().Element(BodyCell).AlignRight().Text(line.Carriage != 0 ? $"{line.Carriage:N0}" : "-").FontSize(7.5f);
                        table.Cell().Element(BodyCell).AlignRight().Text(line.AddLess != 0 ? $"{line.AddLess:N0}" : "-").FontSize(7.5f);
                        table.Cell().Element(BodyCell).AlignRight().Text(line.Amount.ToString("#,##0;(#,##0);0")).Bold().FontSize(8f);
                        sr++;
                    }
                }
                else
                {
                    table.Cell().ColumnSpan((uint)(_header.EnableCarriage ? 10 : 9)).AlignCenter().PaddingVertical(15)
                        .Text("No transactions found for this period.").Italic().FontSize(9f).FontColor(Colors.Grey.Darken1);
                }
            });

            // Summary Footer Card
            col.Item().PaddingTop(10).Row(summaryRow =>
            {
                summaryRow.RelativeItem().PaddingRight(20).Column(notesCol =>
                {
                    notesCol.Item().Text("IMPORTANT NOTES:").Bold().FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                    notesCol.Item().PaddingTop(2).Text("1. Please review this statement and report any discrepancy within 7 days.")
                        .FontSize(7f).FontColor(Colors.Grey.Darken1);
                    notesCol.Item().PaddingTop(1).Text("2. Computer generated statement; valid without physical signature.")
                        .FontSize(7f).FontColor(Colors.Grey.Darken1);
                });

                summaryRow.ConstantItem(260).Background(Colors.Grey.Lighten4).Padding(8).Column(totalsCol =>
                {
                    totalsCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Previous Balance:").FontSize(8f).FontColor(Colors.Grey.Darken2);
                        r.AutoItem().Text($"Rs. {_header.PreviousBalance:N0}").FontSize(8.5f).SemiBold();
                    });

                    totalsCol.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Text("Current Purchases:").FontSize(8f).FontColor(Colors.Grey.Darken2);
                        r.AutoItem().Text($"Rs. {_header.TotalBilling:N0}").FontSize(8.5f).SemiBold();
                    });

                    if (_header.Payment != 0)
                    {
                        totalsCol.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Payments Made:").FontSize(8f).FontColor(Colors.Green.Darken2);
                            r.AutoItem().Text($"Rs. ({_header.Payment:N0})").FontSize(8.5f).SemiBold().FontColor(Colors.Green.Darken2);
                        });
                    }

                    totalsCol.Item().PaddingTop(3).LineHorizontal(1f).LineColor(Colors.Grey.Lighten1);

                    totalsCol.Item().PaddingTop(3).Row(r =>
                    {
                        r.RelativeItem().Text("NET PAYABLE BALANCE:").Bold().FontSize(9f).FontColor(Colors.Blue.Darken3);
                        r.AutoItem().Text($"Rs. {_header.ClosingBalance:N0}").Bold().FontSize(10f).FontColor(Colors.Blue.Darken3);
                    });
                });
            });
        });
    }

    private void ComposeA4Footer(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(4).Row(r =>
            {
                r.RelativeItem().Text("Software Powered by Bizgrip Solutions").FontSize(7f).FontColor(Colors.Grey.Darken1);
                r.AutoItem().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });
    }

    #endregion
}
