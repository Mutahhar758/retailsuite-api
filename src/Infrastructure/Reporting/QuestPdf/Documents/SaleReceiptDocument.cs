using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for unified Sale Receipts and Invoices.
/// Supports 80mm Continuous Thermal Receipt, A5 Landscape Voucher, and A4 Full Invoice formats
/// with embedded EMVCo / Raast QR Code payment integration.
/// </summary>
public class SaleReceiptDocument : IDocument
{
    private readonly SaleReceiptHeader _header;
    private readonly List<SaleReceiptLine> _lines;

    public SaleReceiptDocument(SaleReceiptHeader header, List<SaleReceiptLine> lines)
    {
        _header = header ?? new SaleReceiptHeader();
        _lines = lines ?? new List<SaleReceiptLine>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        if (_header.Layout == SaleReceiptPrintLayout.Thermal80mm)
        {
            ComposeThermal80(container);
        }
        else if (_header.Layout == SaleReceiptPrintLayout.A5Sheet)
        {
            ComposeA5(container);
        }
        else
        {
            ComposeA4(container);
        }
    }

    #region 80mm Continuous Thermal Receipt

    private void ComposeThermal80(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(80, Unit.Millimetre);
            page.MarginVertical(2, Unit.Millimetre);
            page.MarginLeft(2.5f, Unit.Millimetre);
            page.MarginRight(6.5f, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Arial").FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                // 1. Company Header
                string compName = !string.IsNullOrWhiteSpace(_header.CompanyName) ? _header.CompanyName : "Retail Suite Enterprise";
                col.Item().AlignCenter().Text(compName).FontSize(11.5f).Bold();

                if (!string.IsNullOrWhiteSpace(_header.CompanyAddress))
                {
                    col.Item().AlignCenter().PaddingTop(1).Text(_header.CompanyAddress).FontSize(7.5f).SemiBold();
                }

                if (!string.IsNullOrWhiteSpace(_header.CompanyPhone))
                {
                    col.Item().AlignCenter().PaddingTop(1).Text("Tel: " + _header.CompanyPhone).FontSize(7.5f).SemiBold();
                }

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // 2. Receipt Title
                string receiptTitle = _header.IsDuplicate ? "SALE RECEIPT (DUPLICATE)" : "SALE RECEIPT";
                col.Item().AlignCenter().Text(receiptTitle).FontSize(9f).Bold();

                // 3. Metadata
                col.Item().PaddingTop(1).Row(r =>
                {
                    r.AutoItem().Text("Voucher: ").Bold().FontSize(8f);
                    r.RelativeItem().Text(_header.VoucherNo ?? "").Bold().FontSize(8f);
                    r.AutoItem().Text($"{_header.Date:dd-MMM-yy HH:mm}").FontSize(7.5f);
                });

                col.Item().Row(r =>
                {
                    r.AutoItem().Text("Customer: ").Bold().FontSize(8f);
                    r.RelativeItem().Text(!string.IsNullOrWhiteSpace(_header.CustomerName) ? _header.CustomerName : "Walk-in Customer").FontSize(8f).SemiBold();
                });

                if (!string.IsNullOrWhiteSpace(_header.CustomerPhone))
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Text("Contact: ").FontSize(7.5f);
                        r.RelativeItem().Text(_header.CustomerPhone).FontSize(7.5f);
                    });
                }

                if (!string.IsNullOrWhiteSpace(_header.Cashier))
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Text("Cashier: ").FontSize(7.5f);
                        r.RelativeItem().Text(_header.Cashier).FontSize(7.5f);
                    });
                }

                if (!string.IsNullOrWhiteSpace(_header.Counter))
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Text("Counter: ").FontSize(7.5f);
                        r.RelativeItem().Text(_header.Counter).FontSize(7.5f);
                    });
                }

                if (!string.IsNullOrWhiteSpace(_header.Narration))
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Text("Narration: ").FontSize(7.5f);
                        r.RelativeItem().Text(_header.Narration).FontSize(7.5f);
                    });
                }

                if (!string.IsNullOrWhiteSpace(_header.Description))
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Text("Remarks: ").FontSize(7.5f);
                        r.RelativeItem().Text(_header.Description).FontSize(7.5f);
                    });
                }

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // 4. Line Items Table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.6f); // Item
                        columns.RelativeColumn(1.1f); // Qty
                        columns.RelativeColumn(1.0f); // Rate
                        columns.RelativeColumn(1.3f); // Total
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ITEM").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("QTY").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("RATE").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("TOTAL").Bold().FontSize(8f);

                        header.Cell().ColumnSpan(4).PaddingVertical(1).LineHorizontal(0.75f).LineColor(Colors.Black);
                    });

                    if (_lines != null && _lines.Count > 0)
                    {
                        for (int i = 0; i < _lines.Count; i++)
                        {
                            var line = _lines[i];

                            table.Cell().PaddingVertical(1.2f).Text(t =>
                            {
                                t.Span(line.Item).FontSize(8f).SemiBold();
                                if (!string.IsNullOrWhiteSpace(line.Imei))
                                {
                                    t.Element().PaddingTop(0.5f).Text("IMEI: " + line.Imei).FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                }
                            });

                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.FormattedQtyWithUnit).FontSize(8f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.FormattedRate).FontSize(8f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1.2f).Text(line.FormattedAmount).FontSize(8.5f).Bold();
                        }
                    }
                    else
                    {
                        table.Cell().ColumnSpan(4).AlignCenter().PaddingVertical(3).Text("No line items.").FontSize(8f);
                    }
                });

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // 5. Financial Summary
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Gross Total:").FontSize(8f).SemiBold();
                    r.AutoItem().Text($"Rs. {_header.GrossTotal:N2}").FontSize(8.5f).SemiBold();
                });

                if (_header.TotalDiscount > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Discount:").FontSize(8f).SemiBold();
                        r.AutoItem().Text($"-Rs. {_header.TotalDiscount:N2}").FontSize(8.5f).SemiBold();
                    });
                }

                col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("NET TOTAL:").FontSize(9.5f).Bold();
                    r.AutoItem().Text($"Rs. {_header.NetAmount:N2}").FontSize(10f).Bold();
                });

                col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);

                if (_header.CashReceipt > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Cash Received:").FontSize(8f).SemiBold();
                        r.AutoItem().Text($"Rs. {_header.CashReceipt:N2}").FontSize(8.5f).SemiBold();
                    });
                }

                if (_header.CashBack > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Change Return:").FontSize(8f).SemiBold();
                        r.AutoItem().Text($"Rs. {_header.CashBack:N2}").FontSize(8.5f).SemiBold();
                    });
                }

                if (_header.Balance > 0)
                {
                    col.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Text("Remaining Due:").FontSize(8.5f).Bold();
                        r.AutoItem().Text($"Rs. {_header.Balance:N2}").FontSize(9f).Bold();
                    });
                }

                // 6. QR Code Payment (Raast / EMVCo)
                if (_header.ShowQrPayment && _header.QrPayment != null)
                {
                    decimal payable = _header.Balance > 0 ? _header.Balance : _header.NetAmount;
                    if (payable > 0)
                    {
                        col.Item().PaddingTop(2).LineHorizontal(0.75f).LineColor(Colors.Black);
                        col.Item().AlignCenter().Text("SCAN TO PAY (RAAST / ALL BANKS)").FontSize(7.5f).Bold();

                        try
                        {
                            byte[]? qrBytes = QrCodeHelper.GeneratePng(_header.QrPayment.BuildEmvCoPayload(payable), 4);
                            if (qrBytes != null && qrBytes.Length > 0)
                            {
                                col.Item().AlignCenter().PaddingVertical(2).Width(90).Image(qrBytes);
                            }
                        }
                        catch
                        {
                            // Ignore QR image failure if any
                        }

                        if (!string.IsNullOrWhiteSpace(_header.QrPayment.BankName))
                        {
                            col.Item().AlignCenter().Text(_header.QrPayment.BankName).FontSize(7f).Bold();
                        }
                        if (!string.IsNullOrWhiteSpace(_header.QrPayment.AccountTitle))
                        {
                            col.Item().AlignCenter().Text(_header.QrPayment.AccountTitle).FontSize(7f).SemiBold();
                        }
                    }
                }

                // 7. Thank you & Footer
                col.Item().PaddingTop(3).AlignCenter().Text(_header.ThankyouLine ?? "Thank you for your business!").FontSize(8f).Italic();
                col.Item().PaddingTop(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                col.Item().AlignCenter().PaddingTop(1).Text("Powered by RetailSuite Enterprise").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    #endregion

    #region A5 / A4 Composition

    private void ComposeA5(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A5.Landscape());
            page.Margin(16, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Segoe UI").FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeStandardHeader);
            page.Content().Element(ComposeStandardContent);
            page.Footer().Element(ComposeStandardFooter);
        });
    }

    private void ComposeA4(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(24, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Segoe UI").FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeStandardHeader);
            page.Content().Element(ComposeStandardContent);
            page.Footer().Element(ComposeStandardFooter);
        });
    }

    private void ComposeStandardHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(!string.IsNullOrWhiteSpace(_header.CompanyName) ? _header.CompanyName : "Retail Suite Enterprise")
                        .FontSize(14f).Bold().FontColor(Colors.Blue.Darken3);
                    if (!string.IsNullOrWhiteSpace(_header.CompanyAddress))
                        c.Item().Text(_header.CompanyAddress).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(_header.CompanyPhone))
                        c.Item().Text("Tel: " + _header.CompanyPhone).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });

                row.AutoItem().Column(c =>
                {
                    string title = _header.IsDuplicate ? "SALE INVOICE (DUPLICATE)" : "SALE INVOICE";
                    c.Item().AlignRight().Text(title).FontSize(13f).Bold().FontColor(Colors.Grey.Darken4);
                    c.Item().AlignRight().Text("Voucher #: " + (_header.VoucherNo ?? "")).FontSize(9f).Bold();
                    c.Item().AlignRight().Text($"{_header.Date:dd-MMM-yyyy HH:mm}").FontSize(8.5f);
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken3);

            // Customer Info Banner
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Billed To: ").Bold();
                    t.Span(!string.IsNullOrWhiteSpace(_header.CustomerName) ? _header.CustomerName : "Walk-in Customer").SemiBold();
                    if (!string.IsNullOrWhiteSpace(_header.CustomerAddress))
                    {
                        t.Span(" | " + _header.CustomerAddress);
                    }
                    if (!string.IsNullOrWhiteSpace(_header.CustomerPhone))
                    {
                        t.Span(" | Tel: " + _header.CustomerPhone);
                    }
                });

                if (!string.IsNullOrWhiteSpace(_header.Cashier))
                {
                    row.AutoItem().Text(t =>
                    {
                        t.Span("Cashier: ").Bold();
                        t.Span(_header.Cashier);
                    });
                }
            });

            col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeStandardContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Seq
                    columns.RelativeColumn(3.5f); // Item
                    columns.RelativeColumn(1.2f); // Unit
                    columns.RelativeColumn(1.2f); // Qty
                    columns.RelativeColumn(1.3f); // Rate
                    columns.RelativeColumn(1.1f); // Discount
                    columns.RelativeColumn(1.5f); // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(4).Text("Item Description").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignCenter().Text("Unit").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("Qty").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("Rate").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("Disc").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text("Amount").Bold();

                    header.Cell().ColumnSpan(7).LineHorizontal(1f).LineColor(Colors.Grey.Darken2);
                });

                for (int i = 0; i < _lines.Count; i++)
                {
                    var line = _lines[i];
                    var bg = (i % 2 == 1) ? Colors.Grey.Lighten5 : Colors.White;

                    table.Cell().Background(bg).Padding(4).Text((i + 1).ToString());
                    table.Cell().Background(bg).Padding(4).Text(t =>
                    {
                        t.Span(line.Item).SemiBold();
                        if (!string.IsNullOrWhiteSpace(line.Imei))
                        {
                            t.Element().Text(" [IMEI: " + line.Imei + "]").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                        }
                    });
                    table.Cell().Background(bg).Padding(4).AlignCenter().Text(!string.IsNullOrWhiteSpace(line.Unit) ? line.Unit : "-");
                    table.Cell().Background(bg).Padding(4).AlignRight().Text(line.FormattedQty);
                    table.Cell().Background(bg).Padding(4).AlignRight().Text(line.FormattedRate);
                    table.Cell().Background(bg).Padding(4).AlignRight().Text(line.FormattedDiscount);
                    table.Cell().Background(bg).Padding(4).AlignRight().Text(line.FormattedAmount).SemiBold();
                }
            });

            col.Item().PaddingTop(6).LineHorizontal(1f).LineColor(Colors.Grey.Darken2);

            // Financial Summary Block
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem(2).Column(c =>
                {
                    if (!string.IsNullOrWhiteSpace(_header.Description))
                    {
                        c.Item().Text("Remarks: " + _header.Description).FontSize(8.5f).Italic();
                    }
                    if (!string.IsNullOrWhiteSpace(_header.Narration))
                    {
                        c.Item().Text("Narration: " + _header.Narration).FontSize(8.5f).Italic();
                    }
                    c.Item().PaddingTop(8).Text(_header.ThankyouLine ?? "Thank you for your business!").FontSize(9f).Bold().FontColor(Colors.Blue.Darken3);

                    // QR code on A4/A5
                    if (_header.ShowQrPayment && _header.QrPayment != null)
                    {
                        decimal payable = _header.Balance > 0 ? _header.Balance : _header.NetAmount;
                        if (payable > 0)
                        {
                            try
                            {
                                byte[]? qrBytes = QrCodeHelper.GeneratePng(_header.QrPayment.BuildEmvCoPayload(payable), 4);
                                if (qrBytes != null && qrBytes.Length > 0)
                                {
                                    c.Item().PaddingTop(6).Row(qrRow =>
                                    {
                                        qrRow.AutoItem().Width(75).Image(qrBytes);
                                        qrRow.RelativeItem().PaddingLeft(6).Column(qrc =>
                                        {
                                            qrc.Item().Text("Scan to Pay via Raast / Bank App").FontSize(8f).Bold();
                                            if (!string.IsNullOrWhiteSpace(_header.QrPayment.BankName))
                                                qrc.Item().Text("Bank: " + _header.QrPayment.BankName).FontSize(7.5f);
                                            if (!string.IsNullOrWhiteSpace(_header.QrPayment.AccountTitle))
                                                qrc.Item().Text("Title: " + _header.QrPayment.AccountTitle).FontSize(7.5f);
                                            if (!string.IsNullOrWhiteSpace(_header.QrPayment.AccountNumber))
                                                qrc.Item().Text("Account: " + _header.QrPayment.AccountNumber).FontSize(7.5f).Bold();
                                        });
                                    });
                                }
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    }
                });

                row.RelativeItem(1.5f).Border(1f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Gross Total:").FontSize(8.5f);
                        r.AutoItem().Text($"Rs. {_header.GrossTotal:N2}").FontSize(8.5f).SemiBold();
                    });

                    if (_header.TotalDiscount > 0)
                    {
                        c.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Discount:").FontSize(8.5f);
                            r.AutoItem().Text($"-Rs. {_header.TotalDiscount:N2}").FontSize(8.5f).SemiBold();
                        });
                    }

                    c.Item().PaddingVertical(3).LineHorizontal(1f).LineColor(Colors.Grey.Darken1);

                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Net Amount:").Bold().FontSize(9.5f);
                        r.AutoItem().Text($"Rs. {_header.NetAmount:N2}").Bold().FontSize(10f);
                    });

                    if (_header.CashReceipt > 0)
                    {
                        c.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Cash Received:").FontSize(8.5f);
                            r.AutoItem().Text($"Rs. {_header.CashReceipt:N2}").FontSize(8.5f).SemiBold();
                        });
                    }

                    if (_header.CashBack > 0)
                    {
                        c.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Change:").FontSize(8.5f);
                            r.AutoItem().Text($"Rs. {_header.CashBack:N2}").FontSize(8.5f).SemiBold();
                        });
                    }

                    if (_header.Balance > 0)
                    {
                        c.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Balance Due:").Bold().FontSize(9f);
                            r.AutoItem().Text($"Rs. {_header.Balance:N2}").Bold().FontSize(9.5f);
                        });
                    }
                });
            });
        });
    }

    private void ComposeStandardFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(2).Row(r =>
            {
                r.RelativeItem().Text("Powered by RetailSuite Enterprise").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
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
