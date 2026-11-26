using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

public class PayrollRegisterDocument : IDocument
{
    private readonly PayrollRegisterHeader _header;
    private readonly List<PayrollRegisterRow> _rows;

    public PayrollRegisterDocument(PayrollRegisterHeader header, List<PayrollRegisterRow> rows)
    {
        _header = header ?? new PayrollRegisterHeader();
        _rows = rows ?? [];
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8f).FontFamily(Fonts.Lato).FontColor(Colors.Grey.Darken4));

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
                row.RelativeItem().Column(brand =>
                {
                    brand.Item().Text(_header.CompanyName).FontSize(16).Bold().FontColor(Colors.Blue.Darken4);
                    brand.Item().Text($"PAYROLL REGISTER — {_header.PayPeriodLabel} ({_header.SalaryType})")
                        .FontSize(10.5f).SemiBold().FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(220).AlignRight().Column(meta =>
                {
                    meta.Item().Text($"Run No: {_header.RunNo}  |  Status: {_header.Status}").FontSize(9f).Bold();
                    meta.Item().Text($"Pay Date: {_header.PayDate:dd-MMM-yyyy}  |  Employees: {_rows.Count}").FontSize(8f).FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1f).LineColor(Colors.Blue.Darken3);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(38);   // ID
                c.RelativeColumn(2.2f); // Name
                c.RelativeColumn(1.4f); // Dept
                c.RelativeColumn(1.2f); // Mode
                c.RelativeColumn(1.3f); // Basic
                c.RelativeColumn(1.3f); // Allowances
                c.RelativeColumn(1.3f); // Gross
                c.RelativeColumn(1.2f); // Deductions
                c.RelativeColumn(1.2f); // Loan Rec
                c.RelativeColumn(1.4f); // Net
                c.RelativeColumn(1.3f); // Paid
            });

            table.Header(h =>
            {
                HeaderCell(h.Cell(), "ID");
                HeaderCell(h.Cell(), "Employee");
                HeaderCell(h.Cell(), "Department");
                HeaderCell(h.Cell(), "Mode");
                HeaderCell(h.Cell(), "Basic", true);
                HeaderCell(h.Cell(), "Allow/Bonus", true);
                HeaderCell(h.Cell(), "Gross", true);
                HeaderCell(h.Cell(), "Deductions", true);
                HeaderCell(h.Cell(), "Loan Rec.", true);
                HeaderCell(h.Cell(), "Net Pay", true);
                HeaderCell(h.Cell(), "Paid", true);
            });

            foreach (var r in _rows)
            {
                var bg = r.IsOnHold ? Colors.Orange.Lighten5 : Colors.White;
                BodyCell(table.Cell().Background(bg), r.EmployeeId);
                BodyCell(table.Cell().Background(bg), r.IsOnHold ? $"{r.EmployeeName} [HOLD]" : r.EmployeeName);
                BodyCell(table.Cell().Background(bg), r.Department);
                BodyCell(table.Cell().Background(bg), r.PaymentMethod);
                BodyCell(table.Cell().Background(bg), r.BasicSalary.ToString("#,##0.00"), true);
                BodyCell(table.Cell().Background(bg), r.AllowancesAndBonus.ToString("#,##0.00"), true);
                BodyCell(table.Cell().Background(bg), r.GrossEarnings.ToString("#,##0.00"), true);
                BodyCell(table.Cell().Background(bg), r.OtherDeductions.ToString("#,##0.00"), true);
                BodyCell(table.Cell().Background(bg), r.LoanRecovery.ToString("#,##0.00"), true);
                BodyCell(table.Cell().Background(bg), r.NetSalary.ToString("#,##0.00"), true, bold: true);
                BodyCell(table.Cell().Background(bg), r.PaidAmount.ToString("#,##0.00"), true);
            }

            // Totals Row
            table.Cell().ColumnSpan(4).Background(Colors.Blue.Lighten5).Padding(5).Text("GRAND TOTAL").Bold();
            TotalCell(table.Cell(), _rows.Sum(x => x.BasicSalary));
            TotalCell(table.Cell(), _rows.Sum(x => x.AllowancesAndBonus));
            TotalCell(table.Cell(), _rows.Sum(x => x.GrossEarnings));
            TotalCell(table.Cell(), _rows.Sum(x => x.OtherDeductions));
            TotalCell(table.Cell(), _rows.Sum(x => x.LoanRecovery));
            TotalCell(table.Cell(), _rows.Sum(x => x.NetSalary));
            TotalCell(table.Cell(), _rows.Sum(x => x.PaidAmount));
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text($"Generated: {_header.GeneratedAt:dd-MMM-yyyy HH:mm}").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            row.ConstantItem(100).AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7.5f);
                x.CurrentPageNumber().FontSize(7.5f);
                x.Span(" of ").FontSize(7.5f);
                x.TotalPages().FontSize(7.5f);
            });
        });
    }

    private static void HeaderCell(IContainer cell, string text, bool right = false)
    {
        var styled = cell.Background(Colors.Blue.Darken4).Padding(5);
        if (right) styled = styled.AlignRight();
        styled.Text(text).Bold().FontColor(Colors.White);
    }

    private static void BodyCell(IContainer cell, string text, bool right = false, bool bold = false)
    {
        var styled = cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(4);
        if (right) styled = styled.AlignRight();
        var t = styled.Text(text);
        if (bold) t.Bold();
    }

    private static void TotalCell(IContainer cell, decimal amount)
    {
        cell.Background(Colors.Blue.Lighten5).Padding(5).AlignRight().Text(amount.ToString("#,##0.00")).Bold().FontColor(Colors.Blue.Darken4);
    }
}
