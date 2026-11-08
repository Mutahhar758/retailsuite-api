using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Wanda / Feed Mill Vendor Bill & Statement.
/// Dual-unit support (Kg / Bags, Bag Rate, Payments Made).
/// </summary>
public class WandaVendorBillDocument : IDocument
{
    private readonly VendorBillHeader _header;
    private readonly List<WandaVendorBillLineResponse> _items;

    public WandaVendorBillDocument(VendorBillHeader header, List<WandaVendorBillLineResponse> items)
    {
        _header = header ?? new VendorBillHeader();
        _items = items ?? new List<WandaVendorBillLineResponse>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        if (_header.Layout == VendorBillPrintLayout.Thermal80mm)
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
        decimal totalPurchKg = _items.Where(x => x.TransactionType == "Purchase" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.Qty);
        decimal totalPurchBags = _items.Where(x => x.TransactionType == "Purchase" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.SecQty ?? (x.QtyInPack.HasValue && x.QtyInPack.Value > 0 ? Math.Round(x.Qty / x.QtyInPack.Value) : 0));
        decimal totalPurchAmount = _header.TotalPurchases > 0 ? _header.TotalPurchases : _items.Where(x => x.TransactionType == "Purchase" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => Math.Abs(x.Amount));

        decimal totalBill = _header.TotalBilling;
        decimal previousBal = _header.PreviousBalance;
        decimal grandTotal = previousBal + totalBill;
        decimal payments = _header.Payment;
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
                col.Item().AlignCenter().PaddingTop(2).Text("VENDOR BILL & STATEMENT")
                    .FontSize(11).Bold().FontColor(Colors.Black);

                // 3. Vendor Info, Date Range, Previous Balance
                col.Item().PaddingTop(10).Row(metaRow =>
                {
                    metaRow.RelativeItem().Column(infoCol =>
                    {
                        infoCol.Item().Text(t =>
                        {
                            t.Span("Vendor / Supplier: ").Bold();
                            t.Span($"{_header.VendorTitle} ({_header.VendorAccount})").SemiBold();
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

                // 4. 11/12-column Table matching exact layout
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(38);   // Date
                        columns.ConstantColumn(46);   // Voucher No
                        columns.RelativeColumn();     // Description
                        columns.ConstantColumn(46);   // Weight (Kg)
                        columns.ConstantColumn(30);   // Bags
                        columns.ConstantColumn(38);   // Kg Rate
                        columns.ConstantColumn(38);   // Bag Rate
                        if (_header.EnableCarriage)
                            columns.ConstantColumn(36); // Carriage
                        columns.ConstantColumn(36);   // Add/Less
                        columns.ConstantColumn(50);   // Amount
                        columns.ConstantColumn(44);   // Payment Date
                        columns.ConstantColumn(48);   // Payment Amount
                    });

                    table.Header(header =>
                    {
                        IContainer WandaHeaderCell(IContainer c) =>
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
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Payment Date").Bold().FontSize(6.8f);
                        header.Cell().Element(WandaHeaderCell).AlignCenter().Text("Payment Amt").Bold().FontSize(6.8f);
                    });

                    IContainer WandaBodyCell(IContainer c) =>
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
                            decimal bagRate = line.SecRate ?? (line.QtyInPack.HasValue && line.QtyInPack.Value > 0 ? line.Rate * line.QtyInPack.Value : (bagQty > 0 ? Math.Round(Math.Abs(line.Amount) / bagQty) : 0));

                            table.Cell().Element(WandaBodyCell).AlignCenter().Text($"{line.Date:dd/MM/yy}").FontSize(7.5f);
                            table.Cell().Element(WandaBodyCell).AlignCenter().Text(line.VNo).FontSize(7.5f);
                            table.Cell().Element(WandaBodyCell).AlignLeft().Text(line.Item).SemiBold().FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.Qty.ToString("#,##0")).FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(bagQty > 0 ? bagQty.ToString("#,##0") : "-").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(kgRate > 0 ? kgRate.ToString("#,##0.00") : "-").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(bagRate > 0 ? bagRate.ToString("#,##0") : "-").FontSize(8f);
                            if (_header.EnableCarriage)
                                table.Cell().Element(WandaBodyCell).AlignRight().Text(line.Carriage != 0 ? $"{line.Carriage:N0}" : "0").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.AddLess != 0 ? $"{line.AddLess:N0}" : "0").FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.Amount.ToString("#,##0;(#,##0);0")).Bold().FontSize(8f);
                            table.Cell().Element(WandaBodyCell).AlignCenter().Text(line.ReceiptDate.HasValue ? $"{line.ReceiptDate.Value:dd/MM/yy}" : string.Empty).FontSize(7.5f);
                            table.Cell().Element(WandaBodyCell).AlignRight().Text(line.ReceiptAmount.HasValue && line.ReceiptAmount.Value != 0 ? $"{line.ReceiptAmount.Value:N0}" : string.Empty).FontSize(8f);
                        }
                    }
                });

                col.Item().PaddingTop(12);

                // 5. 3-Column Summary Card Box
                col.Item().Border(1f).BorderColor(Colors.Black).CornerRadius(6).Padding(10).Row(summaryRow =>
                {
                    // Column 1: Purchases Summary
                    summaryRow.RelativeItem(1.1f).PaddingRight(10).Column(c1 =>
                    {
                        c1.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Purchase Weight (Kg):").FontSize(8.5f);
                            r.AutoItem().Text(totalPurchKg.ToString("#,##0")).Bold().FontSize(8.5f);
                        });
                        c1.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Purchase Bags:").FontSize(8.5f);
                            r.AutoItem().Text(totalPurchBags.ToString("#,##0")).Bold().FontSize(8.5f);
                        });
                        c1.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("Purchase Amount:").FontSize(8.5f);
                            r.AutoItem().Text(totalPurchAmount.ToString("#,##0.00")).Bold().FontSize(8.5f);
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
                            r.RelativeItem().Text("Total Purchases:").FontSize(8.5f);
                            r.AutoItem().Text(_header.TotalPurchases.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });

                        if (_header.TotalPurchaseReturns > 0)
                        {
                            c2.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Less Purch Ret:").FontSize(8f);
                                r.AutoItem().Text($"({_header.TotalPurchaseReturns:#,##0.00})").Bold().FontSize(8f);
                            });
                        }
                        if (_header.TotalSales > 0)
                        {
                            c2.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Less Sales:").FontSize(8f);
                                r.AutoItem().Text($"({_header.TotalSales:#,##0.00})").Bold().FontSize(8f);
                            });
                        }
                        if (_header.TotalSaleReturns > 0)
                        {
                            c2.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Add Sale Ret:").FontSize(8f);
                                r.AutoItem().Text(_header.TotalSaleReturns.ToString("#,##0.00")).Bold().FontSize(8f);
                            });
                        }

                        c2.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text("Net Bill:").FontSize(8.5f);
                            r.AutoItem().Text(totalBill.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });

                        c2.Item().PaddingVertical(4).LineHorizontal(1f).LineColor(Colors.Black);

                        c2.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Grand Total:").Bold().FontSize(9f);
                            r.AutoItem().Text(grandTotal.ToString("#,##0.00")).Bold().FontSize(9f);
                        });
                    });

                    // Column 3: Payments & Net Balance
                    summaryRow.RelativeItem(1.0f).BorderLeft(1f).BorderColor(Colors.Black).PaddingLeft(10).Column(c3 =>
                    {
                        c3.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Payments Made:").FontSize(8.5f);
                            r.AutoItem().Text(payments.ToString("#,##0.00")).Bold().FontSize(8.5f);
                        });

                        c3.Item().PaddingVertical(5).LineHorizontal(1.5f).LineColor(Colors.Black);

                        c3.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Net Payable:").Bold().FontSize(9.5f);
                            r.AutoItem().Text(netBalance.ToString("#,##0")).Bold().FontSize(10.5f);
                        });
                    });
                });

                // 6. Footer
                col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                col.Item().AlignCenter().PaddingTop(4).Text("Software Powered by Bizgrip Solutions").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private void ComposeWandaThermal80(IDocumentContainer container)
    {
        decimal totalPurchKg = _items.Where(x => x.TransactionType == "Purchase" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.Qty);
        decimal totalPurchBags = _items.Where(x => x.TransactionType == "Purchase" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => x.SecQty ?? (x.QtyInPack.HasValue && x.QtyInPack.Value > 0 ? Math.Round(x.Qty / x.QtyInPack.Value) : 0));
        decimal totalPurchAmount = _header.TotalPurchases > 0 ? _header.TotalPurchases : _items.Where(x => x.TransactionType == "Purchase" || string.IsNullOrEmpty(x.TransactionType)).Sum(x => Math.Abs(x.Amount));
        decimal totalBill = _header.TotalBilling;
        decimal previousBal = _header.PreviousBalance;
        decimal payments = _header.Payment;
        decimal netBalance = _header.ClosingBalance;

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
                // Store Header
                col.Item().AlignCenter().Text(_header.CompanyName).FontSize(12f).Bold();
                if (!string.IsNullOrWhiteSpace(_header.CompanyAddress))
                    col.Item().AlignCenter().PaddingTop(1).Text(_header.CompanyAddress).FontSize(8f).SemiBold();
                if (!string.IsNullOrWhiteSpace(_header.CompanyPhone))
                    col.Item().AlignCenter().PaddingTop(1).Text($"Tel: {_header.CompanyPhone}").FontSize(8f).SemiBold();

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);
                col.Item().AlignCenter().Text("VENDOR BILL & STATEMENT").FontSize(9f).Bold();

                // Vendor Meta
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

                // Table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.2f); // Item
                        columns.RelativeColumn(1.0f); // Kg
                        columns.RelativeColumn(0.8f); // Bags
                        columns.RelativeColumn(0.9f); // Rate
                        columns.RelativeColumn(1.3f); // Amount
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("ITEM").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("KG").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("BAG").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("RATE").Bold().FontSize(8f);
                        header.Cell().AlignRight().Text("TOTAL").Bold().FontSize(8.5f);
                        header.Cell().ColumnSpan(5).PaddingVertical(1).LineHorizontal(0.75f).LineColor(Colors.Black);
                    });

                    if (_items.Count > 0)
                    {
                        foreach (var line in _items)
                        {
                            decimal bagQty = line.SecQty ?? (line.QtyInPack.HasValue && line.QtyInPack.Value > 0 ? Math.Round(line.Qty / line.QtyInPack.Value) : 0);
                            table.Cell().PaddingVertical(1f).Text(t =>
                            {
                                t.Span($"{line.Date:dd/MM} ").FontSize(7.5f).SemiBold();
                                t.Span(line.Item).FontSize(8f).SemiBold();
                            });
                            table.Cell().AlignRight().PaddingVertical(1f).Text(line.Qty.ToString("#,##0")).FontSize(8f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1f).Text(bagQty > 0 ? bagQty.ToString("#,##0") : "-").FontSize(8f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1f).Text($"{line.Rate:N0}").FontSize(8f).SemiBold();
                            table.Cell().AlignRight().PaddingVertical(1f).Text(line.Amount.ToString("#,##0;(#,##0);0")).FontSize(8.5f).SemiBold();
                        }
                    }
                    else
                    {
                        table.Cell().ColumnSpan(5).AlignCenter().PaddingVertical(3).Text("No line transactions in period.").FontSize(8.5f).SemiBold();
                    }
                });

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);

                // Summary
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Purchases:").FontSize(8.5f).SemiBold();
                    r.AutoItem().Text($"{totalPurchAmount:N0}").FontSize(8.5f).SemiBold();
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Qty (Kg / Bags):").FontSize(8f).SemiBold();
                    r.AutoItem().Text($"{totalPurchKg:N0} Kg / {totalPurchBags:N0} Bags").FontSize(8f).SemiBold();
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("Previous Balance:").FontSize(8.5f).SemiBold();
                    r.AutoItem().Text($"{previousBal:N0}").FontSize(8.5f).SemiBold();
                });

                if (payments != 0)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Payments Made:").FontSize(8.5f).SemiBold();
                        r.AutoItem().Text($"({payments:N0})").FontSize(8.5f).SemiBold();
                    });
                }

                col.Item().PaddingTop(1).LineHorizontal(0.75f).LineColor(Colors.Black);

                col.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text("NET PAYABLE:").Bold().FontSize(10.5f);
                    r.AutoItem().Text($"Rs. {netBalance:N0}").Bold().FontSize(11.5f);
                });

                col.Item().PaddingVertical(2).LineHorizontal(1f).LineColor(Colors.Black);
                col.Item().AlignCenter().PaddingTop(1).Text("Software powered by Bizgrip Solutions").FontSize(6f).SemiBold().FontColor(Colors.Black);
            });
        });
    }
}
