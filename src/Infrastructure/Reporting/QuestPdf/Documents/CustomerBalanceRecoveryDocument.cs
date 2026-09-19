using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Application.Legacy.Reports;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

/// <summary>
/// Server-side vector PDF document for Customer Balance & Recovery Report.
/// Landscape A4 layout generated in-memory using QuestPDF and Lato font.
/// </summary>
public class CustomerBalanceRecoveryDocument : IDocument
{
    private readonly CustomerBalanceRecoveryHeader _header;
    private readonly List<CustomerBalanceRecoveryLineResponse> _items;

    public CustomerBalanceRecoveryDocument(CustomerBalanceRecoveryHeader header, List<CustomerBalanceRecoveryLineResponse> items)
    {
        _header = header ?? new CustomerBalanceRecoveryHeader();
        _items = items ?? new List<CustomerBalanceRecoveryLineResponse>();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(26, Unit.Point);
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

                    brandCol.Item().PaddingTop(2).Text("CUSTOMER BALANCE & RECOVERY REPORT")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(280).AlignRight().Column(metaCol =>
                {
                    metaCol.Item().Text($"Period: {_header.FromDate:dd-MMM-yyyy} to {_header.ToDate:dd-MMM-yyyy}")
                        .FontSize(8.5f)
                        .SemiBold()
                        .FontColor(Colors.Grey.Darken3);

                    metaCol.Item().PaddingTop(2).Text($"Basis: {_header.DateBasis} | Filter: {_header.BalanceFilter}")
                        .FontSize(8f)
                        .FontColor(Colors.Grey.Darken2);

                    metaCol.Item().PaddingTop(2).Text($"Generated: {_header.GeneratedAt:dd MMM yyyy, HH:mm}")
                        .FontSize(7.5f)
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

            // Financial Summary KPI Strip
            col.Item().PaddingTop(6).PaddingBottom(6).Row(kpiRow =>
            {
                var summary = _header.Summary;

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Customers: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span(summary.TotalCustomers.ToString()).FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken4);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Total Due: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {summary.TotalDue:N0}").FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken2);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Recovered: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {summary.TotalRecovery:N0}").FontSize(9.5f).Bold().FontColor(Colors.Green.Darken2);
                });

                kpiRow.RelativeItem().Text(x =>
                {
                    x.Span("Closing Bal: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"Rs. {summary.TotalClosingBalance:N0}").FontSize(9.5f).Bold().FontColor(Colors.Red.Darken2);
                });

                kpiRow.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Recovery Rate: ").FontSize(8.5f).Bold().FontColor(Colors.Grey.Darken2);
                    x.Span($"{summary.OverallRecoveryRate:N1}%").FontSize(9.5f).Bold().FontColor(Colors.Teal.Darken2);
                });
            });

            col.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3.2f); // Customer & Contact
                columns.RelativeColumn(1.3f); // Prev Bal
                columns.RelativeColumn(1.3f); // Current Billing
                columns.RelativeColumn(1.4f); // Total Due
                columns.RelativeColumn(1.3f); // Recovered
                columns.RelativeColumn(1.1f); // Discount
                columns.RelativeColumn(1.5f); // Closing Bal
                columns.RelativeColumn(1.0f); // Recv %
                columns.RelativeColumn(1.1f); // Status
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Element(BlockHeader).Text("Customer Account / Name").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Prev Bal").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Billing").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Total Due").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Recovered").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Discount").SemiBold();
                header.Cell().Element(BlockHeader).AlignRight().Text("Closing Bal").SemiBold();
                header.Cell().Element(BlockHeader).AlignCenter().Text("Recv %").SemiBold();
                header.Cell().Element(BlockHeader).AlignCenter().Text("Status").SemiBold();
            });

            // Table Rows
            int index = 0;
            foreach (var item in _items)
            {
                index++;
                bool isAlt = index % 2 == 0;

                table.Cell().Element(c => BlockCell(c, isAlt)).Column(custCol =>
                {
                    custCol.Item().Text(x =>
                    {
                        x.Span(item.CustomerTitle).SemiBold().FontColor(Colors.Grey.Darken4);
                        if (!string.IsNullOrWhiteSpace(item.CustomerAccountId))
                        {
                            x.Span($" ({item.CustomerAccountId})").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(item.Phone) || !string.IsNullOrWhiteSpace(item.Address))
                    {
                        string sub = string.Join(" • ", new[] { item.Phone, item.Address }.Where(s => !string.IsNullOrWhiteSpace(s)));
                        custCol.Item().PaddingTop(1).Text(sub).FontSize(7f).FontColor(Colors.Grey.Darken1);
                    }
                });

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight()
                    .Text(item.PreviousBalance == 0 ? "-" : $"{item.PreviousBalance:N0}");

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight()
                    .Text(item.CurrentBilling == 0 ? "-" : $"{item.CurrentBilling:N0}");

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight()
                    .Text(item.TotalDue == 0 ? "-" : $"{item.TotalDue:N0}").SemiBold();

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight()
                    .Text(item.RecoveryAmount == 0 ? "-" : $"{item.RecoveryAmount:N0}").FontColor(Colors.Green.Darken2);

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight()
                    .Text(item.Discount == 0 ? "-" : $"{item.Discount:N0}");

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignRight()
                    .Text(item.ClosingBalance == 0 ? "0" : $"{item.ClosingBalance:N0}").Bold()
                    .FontColor(item.ClosingBalance > 0 ? Colors.Red.Darken2 : (item.ClosingBalance < 0 ? Colors.Purple.Darken2 : Colors.Grey.Darken3));

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignCenter()
                    .Text($"{item.RecoveryPercentage:N0}%");

                table.Cell().Element(c => BlockCell(c, isAlt)).AlignCenter()
                    .Text(item.Status).SemiBold().FontColor(GetStatusColor(item.Status));
            }

            // Summary Footer Row
            var sum = _header.Summary;
            table.Cell().Element(BlockTotal).Text($"Total ({_items.Count} Customers)").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{sum.TotalPreviousBalance:N0}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{sum.TotalCurrentBilling:N0}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{sum.TotalDue:N0}").Bold().FontColor(Colors.Blue.Darken3);
            table.Cell().Element(BlockTotal).AlignRight().Text($"{sum.TotalRecovery:N0}").Bold().FontColor(Colors.Green.Darken3);
            table.Cell().Element(BlockTotal).AlignRight().Text($"{sum.TotalDiscount:N0}").Bold();
            table.Cell().Element(BlockTotal).AlignRight().Text($"{sum.TotalClosingBalance:N0}").Bold().FontColor(Colors.Red.Darken3);
            table.Cell().Element(BlockTotal).AlignCenter().Text($"{sum.OverallRecoveryRate:N1}%").Bold();
            table.Cell().Element(BlockTotal).AlignCenter().Text("");
        });
    }

    private static string GetStatusColor(string status)
    {
        return status switch
        {
            "Cleared" => Colors.Green.Darken2,
            "Partial" => Colors.Orange.Darken2,
            "Advance" => Colors.Purple.Darken2,
            _ => Colors.Red.Darken2
        };
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
                x.Span(" • Customer Balance & Recovery").FontColor(Colors.Grey.Darken1);
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
