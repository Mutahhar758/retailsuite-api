using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Customer Bill & Statement.
/// Supports both A4 Sheet layout and 80mm Thermal continuous roll receipt layout
/// with embedded EMVCo / Raast QR Code payment integration.
/// </summary>
public class CustomerBillDocument : IDocument
{
    private readonly CustomerBillHeader _header;
    private readonly List<CustomerBillLineResponse> _items;

    public CustomerBillDocument(CustomerBillHeader header, List<CustomerBillLineResponse> items)
    {
        _header = header ?? new CustomerBillHeader();
        _items = items ?? new List<CustomerBillLineResponse>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        if (_header.Layout == CustomerBillPrintLayout.Thermal80mm)
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
            page.MarginHorizontal(4, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Arial").FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                // 1. Store Header (Bold for emphasis)
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
                col.Item().AlignCenter().Text("CUSTOMER BILL / RECEIPT").FontSize(9f).Bold();

                // 3. Customer & Meta
                col.Item().PaddingTop(2).Row(r =>
                {
                    r.AutoItem().Text("Customer: ").Bold().FontSize(8.5f);
                    r.RelativeItem().Text(_header.CustomerTitle).Bold().FontSize(8.5f);
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

                // 4. Line Items Table with Adj Column (Clean, sharp SemiBold weight)
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.6f); // Item & Date
                        columns.RelativeColumn(0.9f); // Qty
                        columns.RelativeColumn(1.0f); // Rate
                        columns.RelativeColumn(1.0f); // Adj
                        columns.RelativeColumn(1.3f); // Total
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ITEM").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("QTY").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("RATE").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("ADJ").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("TOTAL").Bold().FontSize(8.5f);

                        header.Cell().ColumnSpan(5).PaddingVertical(1).LineHorizontal(0.75f).LineColor(Colors.Black);
                    });

                    if (_items.Count > 0)
                    {
                        foreach (var line in _items)
                        {
                            table.Cell().PaddingVertical(1.2f).Text(t =>
                            {
                                t.Span($"{line.Date:dd/MM} ").FontSize(8.5f).SemiBold().FontColor(Colors.Black);
                                t.Span(line.Item).FontSize(8.5f).SemiBold();
                            });
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.Qty.ToString("#,##0.##")).FontSize(9f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text($"{line.Rate:N0}").FontSize(9f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.AddLess != 0 ? $"{line.AddLess:N0}" : "-").FontSize(9f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text($"{line.Amount:N0}").FontSize(9.5f).SemiBold();
                        }
                    }
                    else
                    {
                        table.Cell().ColumnSpan(5).AlignCenter().PaddingVertical(3).Text("No line transactions in period.").FontSize(8.5f).SemiBold();
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
                    r.RelativeItem().Text("Current Invoiced:").FontSize(9f).SemiBold();
                    r.AutoItem().Text($"{_header.TotalBilling:N0}").FontSize(9.5f).SemiBold();
                });

                if (_header.Payment != 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Payment / Recovery:").FontSize(9f).SemiBold();
                        r.AutoItem().Text($"({_header.Payment:N0})").FontSize(9.5f).SemiBold();
                    });
                }

                col.Item().PaddingTop(1).LineHorizontal(0.75f).LineColor(Colors.Black);

                // Major focal point: NET DUE BALANCE in Bold
                col.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text("NET DUE BALANCE:").Bold().FontSize(11f);
                    r.AutoItem().Text($"Rs. {_header.ClosingBalance:N0}").Bold().FontSize(12f);
                });

                // 6. QR Code Payment
                if (_header.ShowQrPayment && _header.QrPayment != null)
                {
                    col.Item().PaddingTop(3).LineHorizontal(0.75f).LineColor(Colors.Black);
                    col.Item().AlignCenter().Text("SCAN TO PAY (ALL BANKS / RAAST)").FontSize(8f).Bold();

                    try
                    {
                        byte[] qrBytes = QrCodeHelper.GeneratePng(_header.QrPayment.BuildEmvCoPayload(_header.ClosingBalance), 4);
                        if (qrBytes != null && qrBytes.Length > 0)
                        {
                            col.Item().AlignCenter().PaddingVertical(2).Width(100).Image(qrBytes);
                        }
                    }
                    catch
                    {
                        // Fallback gracefully
                    }

                    if (!string.IsNullOrWhiteSpace(_header.QrPayment.BankName))
                        col.Item().AlignCenter().Text(_header.QrPayment.BankName).FontSize(7.5f).Bold();
                    if (!string.IsNullOrWhiteSpace(_header.QrPayment.AccountTitle))
                        col.Item().AlignCenter().Text(_header.QrPayment.AccountTitle).FontSize(7.5f).SemiBold();
                    if (!string.IsNullOrWhiteSpace(_header.QrPayment.AccountNumber))
                    {
                        string dispIban = QrPaymentInfo.FormatIban(QrPaymentInfo.NormalizeToIban(_header.QrPayment.AccountNumber, _header.QrPayment.BankName));
                        col.Item().AlignCenter().Text(dispIban).FontSize(7.5f).Bold();
                    }
                    col.Item().AlignCenter().Text($"Amount: PKR {_header.ClosingBalance:N0}").FontSize(9.5f).Bold();
                }

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // 7. Thank You Note
                string thankLine = !string.IsNullOrWhiteSpace(_header.ThankyouLine)
                    ? _header.ThankyouLine
                    : "Thank you for your valued business!";

                col.Item().AlignCenter().PaddingTop(2).Text(thankLine).Italic().FontSize(8f);
                col.Item().AlignCenter().PaddingTop(1).Text("Software powered by Bizgrip Solutions (Contact: 03228258734)").FontSize(5.8f).SemiBold().FontColor(Colors.Black);
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
                    metaCol.Item().Text("CUSTOMER BILL / INVOICE")
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

            col.Item().PaddingTop(8).LineHorizontal(1f).LineColor(Colors.Grey.Lighten2);

            // Customer Info Card
            col.Item().PaddingTop(6).PaddingBottom(6).Border(1f).BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten5).Padding(8).Row(custRow =>
                {
                    custRow.RelativeItem(3).Column(infoCol =>
                    {
                        infoCol.Item().Text("BILL TO CUSTOMER:")
                            .FontSize(7.5f)
                            .Bold()
                            .FontColor(Colors.Grey.Darken1);

                        infoCol.Item().PaddingTop(2).Text(_header.CustomerTitle)
                            .FontSize(11f).Bold().FontColor(Colors.Grey.Darken4);

                        if (!string.IsNullOrWhiteSpace(_header.CustomerAddress))
                        {
                            infoCol.Item().PaddingTop(2).Text(_header.CustomerAddress)
                                .FontSize(8f)
                                .FontColor(Colors.Grey.Darken2);
                        }

                        if (!string.IsNullOrWhiteSpace(_header.CustomerPhone))
                        {
                            infoCol.Item().PaddingTop(1).Text($"Phone: {_header.CustomerPhone}")
                                .FontSize(8f)
                                .FontColor(Colors.Grey.Darken2);
                        }
                    });

                    custRow.ConstantItem(180).AlignRight().Column(codeCol =>
                    {
                        codeCol.Item().Text("Previous Balance (B/F):")
                            .FontSize(7.5f)
                            .FontColor(Colors.Grey.Darken1);

                        codeCol.Item().Text($"Rs. {_header.PreviousBalance:N0}")
                            .FontSize(11f)
                            .Bold()
                            .FontColor(Colors.Grey.Darken4);
                    });
                });
        });
    }

    private void ComposeA4Content(IContainer container)
    {
        container.PaddingTop(4).Column(col =>
        {
            // Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(24);   // #
                    columns.ConstantColumn(68);   // Date
                    columns.ConstantColumn(72);   // Voucher #
                    columns.RelativeColumn();     // Item Description
                    columns.ConstantColumn(40);   // Unit
                    columns.ConstantColumn(48);   // Qty
                    columns.ConstantColumn(65);   // Rate
                    columns.ConstantColumn(58);   // Add / Less
                    columns.ConstantColumn(75);   // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).AlignCenter().Text("#");
                    header.Cell().Element(HeaderCell).Text("Date");
                    header.Cell().Element(HeaderCell).Text("Voucher #");
                    header.Cell().Element(HeaderCell).Text("Item Description");
                    header.Cell().Element(HeaderCell).AlignCenter().Text("Unit");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Rate");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Add / Less");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                });

                if (_items.Count == 0)
                {
                    table.Cell().ColumnSpan(9).Element(c => BodyCell(c, Colors.White))
                        .AlignCenter().PaddingVertical(14).Text("No billing transactions recorded in the selected period.").Italic().FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    for (int i = 0; i < _items.Count; i++)
                    {
                        var line = _items[i];
                        var bg = (i % 2 == 0) ? Colors.White : Colors.Grey.Lighten5;

                        table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text((i + 1).ToString()).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        table.Cell().Element(c => BodyCell(c, bg)).Text($"{line.Date:dd-MMM-yyyy}").FontSize(7.5f);
                        table.Cell().Element(c => BodyCell(c, bg)).Text(line.VNo).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                        table.Cell().Element(c => BodyCell(c, bg)).Text(line.Item).SemiBold();
                        table.Cell().Element(c => BodyCell(c, bg)).AlignCenter().Text(line.UnitTitle).FontSize(7.5f);
                        table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(line.Qty.ToString("#,##0.##"));
                        table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text($"{line.Rate:N0}");
                        table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text(line.AddLess != 0 ? $"{line.AddLess:N0}" : "-");
                        table.Cell().Element(c => BodyCell(c, bg)).AlignRight().Text($"{line.Amount:N0}").SemiBold();
                    }
                }

                // Table Subtotal
                table.Cell().ColumnSpan(8).Element(SubtotalCell).Text("CURRENT PERIOD BILL TOTAL:").Bold().FontColor(Colors.Grey.Darken3);
                table.Cell().Element(SubtotalCell).AlignRight().Text($"{_header.TotalBilling:N0}").Bold().FontColor(Colors.Grey.Darken4);
            });

            col.Item().PaddingTop(10);

            // Financial Summary Breakdown & QR Section Block
            col.Item().Row(summaryRow =>
            {
                summaryRow.RelativeItem(3).Column(notesCol =>
                {
                    string thankLine = !string.IsNullOrWhiteSpace(_header.ThankyouLine)
                        ? _header.ThankyouLine
                        : "Thank you for your valued business!";

                    notesCol.Item().Border(1f).BorderColor(Colors.Blue.Lighten4)
                        .Background(Colors.Blue.Lighten5).Padding(8).Column(msgCol =>
                        {
                            msgCol.Item().Text(thankLine).FontSize(8.5f).Italic().FontColor(Colors.Blue.Darken3);
                            msgCol.Item().PaddingTop(2).Text("Please clear outstanding balances within the agreed credit terms.").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                        });

                    if (_header.ShowQrPayment && _header.QrPayment != null)
                    {
                        notesCol.Item().PaddingTop(6).Element(ComposeA4QrPayment);
                    }
                });

                summaryRow.ConstantItem(260).AlignRight().Column(recCol =>
                {
                    decimal grossTotal = _header.PreviousBalance + _header.TotalBilling;

                    recCol.Item().Table(recTable =>
                    {
                        recTable.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.8f);
                            cols.RelativeColumn(1.4f);
                        });

                        recTable.Cell().Element(RecLabelCell).Text("Previous Balance:");
                        recTable.Cell().Element(RecValueCell).Text($"{_header.PreviousBalance:N0}");

                        recTable.Cell().Element(RecLabelCell).Text("Current Period Bill:");
                        recTable.Cell().Element(RecValueCell).Text($"{_header.TotalBilling:N0}");

                        recTable.Cell().Element(RecLabelBoldCell).Text("Gross Total Payable:");
                        recTable.Cell().Element(RecValueBoldCell).Text($"{grossTotal:N0}");

                        recTable.Cell().Element(RecLabelCell).Text("Less Payments Received:");
                        recTable.Cell().Element(RecValueCell).Text($"({_header.Payment:N0})").FontColor(Colors.Green.Darken3);

                        recTable.Cell().Element(GrandNetLabelCell).Text("NET BALANCE DUE:").Bold();
                        recTable.Cell().Element(GrandNetValueCell).Text($"Rs. {_header.ClosingBalance:N0}").Bold();
                    });
                });
            });
        });
    }

    private void ComposeA4QrPayment(IContainer container)
    {
        byte[]? qrBytes = null;
        try
        {
            if (_header.QrPayment != null)
            {
                qrBytes = QrCodeHelper.GeneratePng(_header.QrPayment.BuildEmvCoPayload(_header.ClosingBalance), 4);
            }
        }
        catch
        {
            qrBytes = null;
        }

        container.Border(1f).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(8)
            .Row(row =>
            {
                if (qrBytes != null && qrBytes.Length > 0)
                {
                    row.ConstantItem(90).AlignCenter().Image(qrBytes);
                }

                row.RelativeItem().PaddingLeft(10).Column(infoCol =>
                {
                    infoCol.Item().Text("SCAN TO PAY VIA ANY BANK APP (RAAST)").FontSize(8.5f).Bold().FontColor(Colors.Blue.Darken3);

                    if (!string.IsNullOrWhiteSpace(_header.QrPayment?.BankName))
                    {
                        infoCol.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("Bank: ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            t.Span(_header.QrPayment.BankName).FontSize(7.5f).Bold();
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(_header.QrPayment?.AccountTitle))
                    {
                        infoCol.Item().Text(t =>
                        {
                            t.Span("Title: ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            t.Span(_header.QrPayment.AccountTitle).FontSize(7.5f).SemiBold();
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(_header.QrPayment?.AccountNumber))
                    {
                        string dispIban = QrPaymentInfo.FormatIban(QrPaymentInfo.NormalizeToIban(_header.QrPayment.AccountNumber, _header.QrPayment.BankName));
                        infoCol.Item().Text(t =>
                        {
                            t.Span("IBAN / Account: ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            t.Span(dispIban).FontSize(7.5f).Bold();
                        });
                    }

                    infoCol.Item().PaddingTop(1).Text(t =>
                    {
                        t.Span("Amount Pre-filled: ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        t.Span($"PKR {_header.ClosingBalance:N0}").FontSize(8f).Bold().FontColor(Colors.Blue.Darken3);
                    });
                });
            });
    }

    private void ComposeA4Footer(IContainer container)
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

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1f)
            .BorderColor(Colors.Grey.Darken2)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken3));
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

    private static IContainer SubtotalCell(IContainer container)
    {
        return container
            .BorderTop(1f)
            .BorderColor(Colors.Grey.Darken2)
            .BorderBottom(1f)
            .BorderColor(Colors.Grey.Darken2)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(8f));
    }

    private static IContainer RecLabelCell(IContainer container)
    {
        return container
            .PaddingVertical(2.5f)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(8f).FontColor(Colors.Grey.Darken2));
    }

    private static IContainer RecValueCell(IContainer container)
    {
        return container
            .AlignRight()
            .PaddingVertical(2.5f)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(8f).FontColor(Colors.Grey.Darken4));
    }

    private static IContainer RecLabelBoldCell(IContainer container)
    {
        return container
            .BorderTop(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3f)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(8f).Bold().FontColor(Colors.Grey.Darken3));
    }

    private static IContainer RecValueBoldCell(IContainer container)
    {
        return container
            .AlignRight()
            .BorderTop(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3f)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(8f).Bold().FontColor(Colors.Grey.Darken4));
    }

    private static IContainer GrandNetLabelCell(IContainer container)
    {
        return container
            .BorderTop(1.5f)
            .BorderColor(Colors.Grey.Darken3)
            .BorderBottom(2.5f)
            .BorderColor(Colors.Grey.Darken3)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(9f).Bold().FontColor(Colors.Grey.Darken4));
    }

    private static IContainer GrandNetValueCell(IContainer container)
    {
        return container
            .AlignRight()
            .BorderTop(1.5f)
            .BorderColor(Colors.Grey.Darken3)
            .BorderBottom(2.5f)
            .BorderColor(Colors.Grey.Darken3)
            .Background(Colors.Grey.Lighten4)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken3));
    }

    #endregion
}
