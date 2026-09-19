using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Customer Bill & Statement.
/// Portrait A4 layout generated in-memory using QuestPDF and Lato font.
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
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Lato).FontColor(Colors.Grey.Darken4));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            // Top Organization & Document Title
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
                        brandCol.Item().Text(_header.CompanyAddress).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    if (!string.IsNullOrWhiteSpace(_header.CompanyPhone))
                    {
                        brandCol.Item().Text($"Tel: {_header.CompanyPhone}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    brandCol.Item().PaddingTop(4).Text("CUSTOMER STATEMENT / BILL")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(220).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}")
                        .FontSize(8.5f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    metaCol.Item().PaddingTop(2).Text($"Basis: {_header.DateBasis}")
                        .FontSize(8f)
                        .FontColor(Colors.Grey.Darken2);

                    metaCol.Item().PaddingTop(2).Text($"Printed: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // Customer Details Card
            col.Item().PaddingTop(6).PaddingBottom(6).Background(Colors.Grey.Lighten4).Padding(8).Row(custRow =>
            {
                custRow.RelativeItem().Column(c =>
                {
                    c.Item().Text(x =>
                    {
                        x.Span("Customer: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                        x.Span(_header.CustomerTitle).FontSize(10f).Bold().FontColor(Colors.Grey.Darken4);
                    });

                    c.Item().PaddingTop(2).Text(x =>
                    {
                        x.Span("Account Code: ").FontSize(8f).FontColor(Colors.Grey.Darken2);
                        x.Span(_header.CustomerAccount).FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken3);
                    });
                });

                custRow.ConstantItem(220).Column(c =>
                {
                    if (!string.IsNullOrWhiteSpace(_header.CustomerPhone))
                    {
                        c.Item().Text(x =>
                        {
                            x.Span("Phone: ").FontSize(8f).FontColor(Colors.Grey.Darken2);
                            x.Span(_header.CustomerPhone).FontSize(8.5f).FontColor(Colors.Grey.Darken3);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(_header.CustomerAddress))
                    {
                        c.Item().PaddingTop(2).Text(x =>
                        {
                            x.Span("Address: ").FontSize(8f).FontColor(Colors.Grey.Darken2);
                            x.Span(_header.CustomerAddress).FontSize(8f).FontColor(Colors.Grey.Darken3);
                        });
                    }
                });
            });

            // KPI Financial Summary Bar
            col.Item().PaddingTop(6).PaddingBottom(6).Row(kpiRow =>
            {
                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Prev Balance: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {_header.PreviousBalance:N0}").FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Current Billing: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {_header.TotalBilling:N0}").FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken2);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Payments Recv: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {_header.Payment:N0}").FontSize(9.5f).Bold().FontColor(Colors.Green.Darken2);
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Net Balance: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {_header.ClosingBalance:N0}").FontSize(10f).Bold()
                        .FontColor(_header.ClosingBalance > 0 ? Colors.Red.Darken2 : (_header.ClosingBalance < 0 ? Colors.Purple.Darken2 : Colors.Green.Darken2));
                });
            });

            col.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(65);   // Date
                    columns.ConstantColumn(75);   // Voucher
                    columns.RelativeColumn(3.2f); // Item Name
                    columns.ConstantColumn(55);   // Unit
                    columns.ConstantColumn(55);   // Qty
                    columns.ConstantColumn(65);   // Rate
                    columns.ConstantColumn(75);   // Amount
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(BlockHeader).Text("Date").SemiBold();
                    header.Cell().Element(BlockHeader).Text("Voucher").SemiBold();
                    header.Cell().Element(BlockHeader).Text("Item Description").SemiBold();
                    header.Cell().Element(BlockHeader).AlignCenter().Text("Unit").SemiBold();
                    header.Cell().Element(BlockHeader).AlignRight().Text("Qty").SemiBold();
                    header.Cell().Element(BlockHeader).AlignRight().Text("Rate").SemiBold();
                    header.Cell().Element(BlockHeader).AlignRight().Text("Amount").SemiBold();
                });

                // Rows
                int index = 0;
                decimal totalQty = 0;
                decimal totalAmount = 0;

                foreach (var line in _items)
                {
                    index++;
                    bool isAlt = index % 2 == 0;
                    totalQty += line.Qty;
                    totalAmount += line.Amount;

                    table.Cell().Element(c => BlockCell(c, isAlt)).Text($"{line.Date:dd-MMM-yy}").FontSize(8f);
                    table.Cell().Element(c => BlockCell(c, isAlt)).Text(line.VNo).FontSize(8f);
                    table.Cell().Element(c => BlockCell(c, isAlt)).Text(line.Item).SemiBold();
                    table.Cell().Element(c => BlockCell(c, isAlt)).AlignCenter().Text(line.UnitTitle).FontSize(8f);
                    table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text($"{line.Qty:N0}");
                    table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text($"{line.Rate:N2}");
                    table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight().Text($"{line.Amount:N0}").SemiBold();
                }

                if (_items.Count == 0)
                {
                    table.Cell().ColumnSpan(7).Element(c => BlockCell(c, false)).Padding(12).AlignCenter()
                        .Text("No billing transactions recorded during this period.").FontColor(Colors.Grey.Darken1);
                }

                // Table Summary Row
                table.Cell().ColumnSpan(4).Element(BlockTotal).Text($"Total ({_items.Count} Items)").Bold();
                table.Cell().Element(BlockTotal).AlignRight().Text($"{totalQty:N0}").Bold();
                table.Cell().Element(BlockTotal).AlignRight().Text("");
                table.Cell().Element(BlockTotal).AlignRight().Text($"{totalAmount:N0}").Bold().FontColor(Colors.Blue.Darken2);
            });

            // Settlement Summary Box
            col.Item().PaddingTop(14).Row(row =>
            {
                row.RelativeItem(); // Spacer

                row.ConstantItem(260).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(settleCol =>
                {
                    settleCol.Item().Text("ACCOUNT SETTLEMENT SUMMARY").FontSize(9).Bold().FontColor(Colors.Grey.Darken3);
                    settleCol.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    settleCol.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("Previous Balance:").FontSize(8.5f);
                        r.ConstantItem(90).AlignRight().Text($"Rs. {_header.PreviousBalance:N0}").FontSize(8.5f);
                    });

                    settleCol.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Text("(+) Current Billing:").FontSize(8.5f);
                        r.ConstantItem(90).AlignRight().Text($"Rs. {_header.TotalBilling:N0}").FontSize(8.5f);
                    });

                    settleCol.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Text("(-) Payments Received:").FontSize(8.5f);
                        r.ConstantItem(90).AlignRight().Text($"Rs. {_header.Payment:N0}").FontSize(8.5f).FontColor(Colors.Green.Darken2);
                    });

                    settleCol.Item().PaddingTop(4).LineHorizontal(1f).LineColor(Colors.Grey.Darken2);

                    settleCol.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("Net Balance Due:").FontSize(9.5f).Bold();
                        r.ConstantItem(90).AlignRight().Text($"Rs. {_header.ClosingBalance:N0}").FontSize(9.5f).Bold()
                            .FontColor(_header.ClosingBalance > 0 ? Colors.Red.Darken2 : (_header.ClosingBalance < 0 ? Colors.Purple.Darken2 : Colors.Green.Darken2));
                    });
                });
            });
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
                x.Span(" • Customer Bill & Statement • Thank you for your business!").FontColor(Colors.Grey.Darken1);
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
