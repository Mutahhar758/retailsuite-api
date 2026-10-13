using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Wanda / Feed Mill Customer Bill & Statement.
/// Supports both A4 Sheet layout and 80mm Thermal continuous roll receipt layout
/// with multi-unit (Kg / Bags), purchases/returns breakdown, and Raast QR payment.
/// </summary>
public class WandaCustomerBillDocument : IDocument
{
    private readonly CustomerBillHeader _header;
    private readonly List<WandaCustomerBillLineResponse> _items;

    public WandaCustomerBillDocument(CustomerBillHeader header, List<WandaCustomerBillLineResponse> items)
    {
        _header = header ?? new CustomerBillHeader();
        _items = items ?? new List<WandaCustomerBillLineResponse>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        if (_header.Layout == CustomerBillPrintLayout.Thermal80mm)
        {
            ComposeWandaThermal80(container);
        }
        else
        {
            ComposeWandaA4(container);
        }
    }

    private void ComposeWandaA4(IDocumentContainer container)
    {
        decimal totalSaleKg = _items.Where(x => x.TransactionType == "Sale" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.Qty);
        decimal totalSaleBags = _items.Where(x => x.TransactionType == "Sale" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.SecQty ?? (x.QtyInPack.HasValue && x.QtyInPack.Value > 0 ? Math.Round(x.Qty / x.QtyInPack.Value) : 0));
        decimal totalSaleAmount = _header.TotalSales > 0 ? _header.TotalSales : _items.Where(x => x.TransactionType == "Sale" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.Amount);

        decimal totalBill = _header.TotalBilling;
        decimal previousBal = _header.PreviousBalance;
        decimal grandTotal = previousBal + totalBill;
        decimal receipts = _header.Payment;
        decimal payments = 0m;
        decimal netBalance = _header.ClosingBalance;

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(14, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily(Fonts.Arial).FontColor(Colors.Black));

            page.Content().Border(1f).BorderColor(Colors.Black).CornerRadius(6).Padding(10).Column(col =>
            {
                // 1. Company Name
                col.Item().AlignCenter().Text(_header.CompanyName.ToUpper())
                    .FontSize(18).Bold().FontColor(Colors.Black);

                // 2. Report Subtitle
                col.Item().AlignCenter().PaddingTop(2).Text("CUSTOMER BILL")
                    .FontSize(11).Bold().FontColor(Colors.Black);

                // 3. Customer Info, Date Range, Previous Balance
                col.Item().PaddingTop(10).Row(metaRow =>
                {
                    metaRow.RelativeItem().Column(infoCol =>
                    {
                        infoCol.Item().Text(t =>
                        {
                            t.Span("Account: ").Bold();
                            t.Span($"{_header.CustomerTitle} ({_header.CustomerAccount})").SemiBold();
                        });
                        infoCol.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("Date: ").Bold();
                            t.Span($"{_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}").SemiBold();
                        });
                    });

                    metaRow.AutoItem().AlignRight().Text(t =>
                    {
                        t.Span("Previous Balance: ").Bold();
                        t.Span($"Rs. {previousBal:N2}").SemiBold();
                    });
                });

                col.Item().PaddingTop(8);

                // 4. 11-column Table matching exact layout
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(38);   // Date
                        columns.ConstantColumn(46);   // Voucher No
                        columns.RelativeColumn();     // Description (Takes maximum available space)
                        columns.ConstantColumn(46);   // Weight (Kg)
                        columns.ConstantColumn(30);   // Bags
                        columns.ConstantColumn(38);   // Kg Rate
                        columns.ConstantColumn(38);   // Bag Rate
                        if (_header.EnableCarriage)
                            columns.ConstantColumn(36);   // Carriage
                        columns.ConstantColumn(36);   // Add/Less
                        columns.ConstantColumn(50);   // Amount
                        columns.ConstantColumn(44);   // Receipt Date
                        columns.ConstantColumn(48);   // Receipt Amount
                    });

                    table.Header(header =>
                    {
                        static IContainer WandaHeaderCell(IContainer c) =>
                            c.Border(0.75f).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(1.5f);

                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Date").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Voucher No").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Description").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Weight (Kg)").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Bags").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Kg Rate").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Bag Rate").Bold().FontSize(7.5f);
                        if (_header.EnableCarriage)
                            header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Carriage").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Add/Less").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Amount").Bold().FontSize(7.5f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Receipt Date").Bold().FontSize(7f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Receipt Amount").Bold().FontSize(7f);
                    });

                    static IContainer WandaBodyCell(IContainer c) =>
                        c.Border(0.5f).BorderColor(Colors.Black).PaddingVertical(3).PaddingHorizontal(2f);

                    if (_items.Count == 0)
                    {
                        table.Cell().ColumnSpan((uint)(_header.EnableCarriage ? 12 : 11)).Element(WandaBodyCell).AlignCenter().PaddingVertical(12)
                            .Text("No transactions in period").Italic().FontSize(8.5f);
                    }
                    else
                    {
                        foreach (var line in _items)
                        {
                            decimal bagQty = line.SecQty ?? (line.QtyInPack.HasValue && line.QtyInPack.Value > 0 ? Math.Round(line.Qty / line.QtyInPack.Value) : 0);
                            decimal kgRate = line.Rate;
                            decimal bagRate = line.SecRate ?? (line.QtyInPack.HasValue && line.QtyInPack.Value > 0 ? line.Rate * line.QtyInPack.Value : (bagQty > 0 ? Math.Round(line.Amount / bagQty) : 0));

                            table.Cell().Element(WandaBodyCell).AlignCenter().Text($"{line.Date:dd/MM/yy}").FontSize(7.5f);
                            table.Cell().Element(WandaBodyCell).AlignCenter().Text(line.VNo).FontSize(7.5f);
                            table.Cell().Element(WandaBodyCell).AlignLeft().Text(line.Item).SemiBold().FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.Qty.ToString("#,##0")).FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(bagQty > 0 ? bagQty.ToString("#,##0") : "-").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(kgRate > 0 ? kgRate.ToString("#,##0.00") : "-").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(bagRate > 0 ? bagRate.ToString("#,##0") : "-").FontSize(8f);
                            if (_header.EnableCarriage)
                                table.Cell().Element(WandaBodyCell).AlignRight().Text(line.Carriage != 0 ? line.Carriage.ToString("#,##0") : "0").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.AddLess != 0 ? line.AddLess.ToString("#,##0") : "0").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.Amount.ToString("#,##0;(#,##0);0")).Bold().FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignCenter().Text(line.ReceiptDate.HasValue ? line.ReceiptDate.Value.ToString("dd/MM/yy") : string.Empty).FontSize(7.5f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.ReceiptAmount.HasValue && line.ReceiptAmount.Value != 0 ? line.ReceiptAmount.Value.ToString("#,##0") : string.Empty).FontSize(8f);
                        }
                    }
                });

                col.Item().PaddingTop(12);

                // 5. 3-Column Summary Card Box
                col.Item().Border(1f).BorderColor(Colors.Black).CornerRadius(6).Padding(10).Row(summaryRow =>
                {
                    // Column 1: Sales Summary
                    summaryRow.RelativeItem(1.1f).PaddingRight(10).Column(c1 =>
                    {
                        c1.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Sale Weight (Kg):").FontSize(8.5f);
                            r.AutoItem().Text(totalSaleKg.ToString("#,##0")).Bold().FontSize(8.5f);
                        });
                        c1.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Sale Bags:").FontSize(8.5f);
                            r.AutoItem().Text(totalSaleBags.ToString("#,##0")).Bold().FontSize(8.5f);
                        });
                        c1.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Sale Amount:").FontSize(8.5f);
                            r.AutoItem().Text(totalSaleAmount.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });
                    });

                    // Column 2: Bill Breakdown
                    summaryRow.RelativeItem(1.0f).BorderLeft(1f).BorderColor(Colors.Black).PaddingHorizontal(10).Column(c2 =>
                    {
                        c2.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Previous Balance:").FontSize(8.5f);
                            r.AutoItem().Text(previousBal.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });
                        c2.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Total Sales:").FontSize(8.5f);
                            r.AutoItem().Text(_header.TotalSales.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });

                        if (_header.TotalPurchases > 0)
                        {
                            c2.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Less Purchases:").FontSize(8f);
                                r.AutoItem().Text($"({_header.TotalPurchases:#,##0.00})").Bold().FontSize(8f);
                            });
                        }
                        if (_header.TotalSaleReturns > 0)
                        {
                            c2.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Less Sale Ret:").FontSize(8f);
                                r.AutoItem().Text($"({_header.TotalSaleReturns:#,##0.00})").Bold().FontSize(8f);
                            });
                        }
                        if (_header.TotalPurchaseReturns > 0)
                        {
                            c2.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Add Purch Ret:").FontSize(8f);
                                r.AutoItem().Text(_header.TotalPurchaseReturns.ToString("#,##0.00")).Bold().FontSize(8f);
                            });
                        }
                        if (_header.TotalPurchases > 0 || _header.TotalSaleReturns > 0 || _header.TotalPurchaseReturns > 0)
                        {
                            c2.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Net Bill:").FontSize(8.5f);
                                r.AutoItem().Text(totalBill.ToString("#,##0.00")).Bold().FontSize(8.5f);
                            });
                        }

                        c2.Item().PaddingVertical(4).LineHorizontal(1f).LineColor(Colors.Black);

                        c2.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Grand Total:").Bold().FontSize(9f);
                            r.AutoItem().Text(grandTotal.ToString("#,##0.00")).Bold().FontSize(9f);
                        });
                    });

                    // Column 3: Receipts & Net Balance
                    summaryRow.RelativeItem(1.0f).BorderLeft(1f).BorderColor(Colors.Black).PaddingLeft(10).Column(c3 =>
                    {
                        c3.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Receipts:").FontSize(8.5f);
                            r.AutoItem().Text(receipts.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });
                        c3.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Payments:").FontSize(8.5f);
                            r.AutoItem().Text(payments.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });

                        c3.Item().PaddingVertical(5).LineHorizontal(1.5f).LineColor(Colors.Black);

                        c3.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Net Balance:").Bold().FontSize(9.5f);
                            r.AutoItem().Text(netBalance.ToString("#,##0")).Bold().FontSize(10.5f);
                        });
                    });
                });

                // 6. Optional Raast QR Section (if enabled)
                if (_header.ShowQrPayment && _header.QrPayment != null)
                {
                    col.Item().PaddingTop(8).Element(ComposeA4QrPayment);
                }

                // 7. Footer
                col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                col.Item().AlignCenter().PaddingTop(4).Text("Software Powered by Bizgrip Solutions").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private void ComposeWandaThermal80(IDocumentContainer container)
    {
        decimal totalSaleKg = _items.Where(x => x.TransactionType == "Sale" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.Qty);
        decimal totalSaleBags = _items.Where(x => x.TransactionType == "Sale" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.SecQty ?? (x.QtyInPack.HasValue && x.QtyInPack.Value > 0 ? Math.Round(x.Qty / x.QtyInPack.Value) : 0));
        decimal totalSaleAmount = _header.TotalSales > 0 ? _header.TotalSales : _items.Where(x => x.TransactionType == "Sale" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.Amount);
        decimal totalBill = _header.TotalBilling;
        decimal previousBal = _header.PreviousBalance;
        decimal receipts = _header.Payment;
        decimal netBalance = _header.ClosingBalance;

        container.Page(page =>
        {
            page.ContinuousSize(80, Unit.Millimetre);
            page.MarginVertical(2, Unit.Millimetre);
            page.MarginLeft(2.5f, Unit.Millimetre);
            page.MarginRight(8.5f, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Arial").FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                // Header
                col.Item().AlignCenter().Text(_header.CompanyName.ToUpper()).FontSize(11f).Bold();
                col.Item().AlignCenter().Text("FEED MILL CUSTOMER BILL").FontSize(8f).Bold();

                col.Item().PaddingVertical(1).LineHorizontal(0.75f).LineColor(Colors.Black);

                col.Item().Text(t =>
                {
                    t.Span("Customer: ").Bold().FontSize(7.5f);
                    t.Span($"{_header.CustomerTitle}").FontSize(7.5f);
                });
                col.Item().Text(t =>
                {
                    t.Span("Period: ").Bold().FontSize(7.5f);
                    t.Span($"{_header.FromDate:dd/MM} to {_header.ToDate:dd/MM/yy}").FontSize(7.5f);
                });

                col.Item().PaddingVertical(1).LineHorizontal(0.75f).LineColor(Colors.Black);

                // Table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);     // Date
                        cols.RelativeColumn();       // Item
                        cols.ConstantColumn(16);     // Bag
                        cols.ConstantColumn(26);     // Kg
                        cols.ConstantColumn(20);     // BagRate
                        cols.ConstantColumn(30);     // Amount
                    });

                    table.Header(h =>
                    {
                        static IContainer ThCell(IContainer c) => c.PaddingBottom(1).BorderBottom(0.5f).BorderColor(Colors.Black);
                        h.Cell().Element(ThCell).Text("Date").FontSize(6.5f).Bold();
                        h.Cell().Element(ThCell).Text("Item").FontSize(6.5f).Bold();
                        h.Cell().Element(ThCell).AlignRight().Text("Bag").FontSize(6.5f).Bold();
                        h.Cell().Element(ThCell).AlignRight().Text("Kg").FontSize(6.5f).Bold();
                        h.Cell().Element(ThCell).AlignRight().Text("BRate").FontSize(6.5f).Bold();
                        h.Cell().Element(ThCell).AlignRight().Text("Amount").FontSize(6.5f).Bold();
                    });

                    foreach (var line in _items)
                    {
                        decimal bagQty = line.SecQty ?? (line.QtyInPack.HasValue && line.QtyInPack.Value > 0 ? Math.Round(line.Qty / line.QtyInPack.Value) : 0);
                        decimal bagRate = line.SecRate ?? (line.QtyInPack.HasValue && line.QtyInPack.Value > 0 ? line.Rate * line.QtyInPack.Value : (bagQty > 0 ? Math.Round(line.Amount / bagQty) : 0));

                        table.Cell().Text($"{line.Date:dd/MM}").FontSize(6.5f);
                        table.Cell().Text(line.Item).FontSize(6.5f).SemiBold();
                        table.Cell().AlignRight().Text(bagQty > 0 ? bagQty.ToString("#,##0") : "-").FontSize(6.5f);
                        table.Cell().AlignRight().Text(line.Qty.ToString("#,##0")).FontSize(6.5f);
                        table.Cell().AlignRight().Text(bagRate > 0 ? bagRate.ToString("#,##0") : "-").FontSize(6.5f);
                        table.Cell().AlignRight().Text(line.Amount.ToString("#,##0;(#,##0);0")).FontSize(6.5f).Bold();
                    }
                });

                col.Item().PaddingVertical(1).LineHorizontal(0.75f).LineColor(Colors.Black);

                // Summary Breakdown
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Weight:").FontSize(8f);
                    r.AutoItem().Text($"{totalSaleKg:N0} Kg").FontSize(8f).Bold();
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Bags:").FontSize(8f);
                    r.AutoItem().Text($"{totalSaleBags:N0}").FontSize(8f).Bold();
                });

                col.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Previous Balance:").FontSize(8f);
                    r.AutoItem().Text($"{previousBal:N0}").FontSize(8f);
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Sales:").FontSize(8f);
                    r.AutoItem().Text($"{_header.TotalSales:N0}").FontSize(8f).Bold();
                });
                if (_header.TotalPurchases > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Less Purchases:").FontSize(7.5f);
                        r.AutoItem().Text($"({_header.TotalPurchases:N0})").FontSize(7.5f).Bold();
                    });
                }
                if (_header.TotalSaleReturns > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Less Sale Returns:").FontSize(7.5f);
                        r.AutoItem().Text($"({_header.TotalSaleReturns:N0})").FontSize(7.5f).Bold();
                    });
                }
                if (_header.TotalPurchaseReturns > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Add Purchase Returns:").FontSize(7.5f);
                        r.AutoItem().Text($"{_header.TotalPurchaseReturns:N0}").FontSize(7.5f).Bold();
                    });
                }
                if (_header.TotalPurchases > 0 || _header.TotalSaleReturns > 0 || _header.TotalPurchaseReturns > 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Net Bill:").FontSize(8.5f).SemiBold();
                        r.AutoItem().Text($"{totalBill:N0}").FontSize(8.5f).SemiBold();
                    });
                }
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Receipts:").FontSize(8.5f).SemiBold();
                    r.AutoItem().Text($"{receipts:N0}").FontSize(8.5f).SemiBold();
                });

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("NET DUE:").FontSize(10f).Bold();
                    r.AutoItem().Text($"Rs. {netBalance:N0}").FontSize(11f).Bold();
                });

                // QR if enabled
                if (_header.ShowQrPayment && _header.QrPayment != null)
                {
                    byte[]? qrBytes = null;
                    try { qrBytes = QrCodeHelper.GeneratePng(_header.QrPayment.BuildEmvCoPayload(_header.ClosingBalance), 3); } catch { }
                    if (qrBytes != null && qrBytes.Length > 0)
                    {
                        col.Item().PaddingTop(3).AlignCenter().Image(qrBytes).FitWidth();
                        col.Item().AlignCenter().Text("SCAN TO PAY (RAAST)").FontSize(7.5f).Bold();
                    }
                }

                col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Black);
                col.Item().AlignCenter().Text("Software powered by Bizgrip Solutions").FontSize(6f).SemiBold();
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
                            t.Span(_header.QrPayment.BankName).FontSize(7.5f).SemiBold();
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
}
