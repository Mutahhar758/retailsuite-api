using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Documents;

public class PayslipDocument : IDocument
{
    private readonly List<PayslipModel> _payslips;

    public PayslipDocument(List<PayslipModel> payslips)
    {
        _payslips = payslips ?? [];
    }

    public PayslipDocument(PayslipModel singlePayslip)
        : this([singlePayslip])
    {
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        foreach (var slip in _payslips)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32, Unit.Point);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily(Fonts.Lato).FontColor(Colors.Grey.Darken4));

                page.Header().Element(c => ComposeHeader(c, slip));
                page.Content().Element(c => ComposeContent(c, slip));
                page.Footer().Element(c => ComposeFooter(c, slip));
            });
        }
    }

    private static void ComposeHeader(IContainer container, PayslipModel slip)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(brand =>
                {
                    brand.Item().Text(slip.CompanyName).FontSize(17).Bold().FontColor(Colors.Blue.Darken4);
                    if (!string.IsNullOrWhiteSpace(slip.CompanyAddress))
                        brand.Item().Text(slip.CompanyAddress).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(slip.CompanyPhone))
                        brand.Item().Text($"Tel: {slip.CompanyPhone}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(200).AlignRight().Column(meta =>
                {
                    meta.Item().Text("SALARY PAYSLIP").FontSize(13).Bold().FontColor(Colors.Grey.Darken4);
                    meta.Item().PaddingTop(2).Text(slip.PayPeriodLabel).FontSize(10).SemiBold().FontColor(Colors.Blue.Darken3);
                    meta.Item().PaddingTop(2).Text($"Run: {slip.RunNo}  |  Pay Date: {slip.PayDate:dd-MMM-yyyy}")
                        .FontSize(8f).FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1.2f).LineColor(Colors.Blue.Darken3);
        });
    }

    private static void ComposeContent(IContainer container, PayslipModel slip)
    {
        container.PaddingTop(12).Column(col =>
        {
            // Employee Info Card
            col.Item().Border(0.75f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    InfoRow(left, "Employee Name:", $"{slip.EmployeeName} ({slip.EmployeeId})");
                    InfoRow(left, "Designation:", slip.Designation ?? "-");
                    InfoRow(left, "Department:", slip.Department ?? "General");
                    InfoRow(left, "CNIC / ID:", slip.Cnic ?? "-");
                });

                row.ConstantItem(16);

                row.RelativeItem().Column(right =>
                {
                    InfoRow(right, "Payment Mode:", slip.PaymentMethod);
                    InfoRow(right, "Bank:", string.IsNullOrWhiteSpace(slip.BankName) ? "-" : slip.BankName);
                    InfoRow(right, "Account / IBAN:", string.IsNullOrWhiteSpace(slip.Iban) ? (slip.BankAccountNo ?? "-") : slip.Iban);
                    InfoRow(right, "Attendance:", $"{slip.PresentDays:0.##}/{slip.WorkingDays:0.##} days  |  Leaves: {slip.UnpaidLeaveDays:0.##}  |  OT: {slip.OvertimeHours:0.##}h");
                });
            });

            // Earnings & Deductions Side-by-Side Table
            col.Item().PaddingTop(14).Row(row =>
            {
                // Earnings Table
                row.RelativeItem().Border(0.75f).BorderColor(Colors.Grey.Lighten2).Column(eCol =>
                {
                    eCol.Item().Background(Colors.Green.Lighten5).Padding(6).Text("EARNINGS")
                        .FontSize(9.5f).Bold().FontColor(Colors.Green.Darken4);

                    eCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });

                        foreach (var e in slip.Earnings)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(e.Name);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"Rs. {e.Amount:#,##0.00}");
                        }

                        table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Gross Earnings").Bold();
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(6).AlignRight().Text($"Rs. {slip.GrossEarnings:#,##0.00}").Bold().FontColor(Colors.Green.Darken3);
                    });
                });

                row.ConstantItem(12);

                // Deductions Table
                row.RelativeItem().Border(0.75f).BorderColor(Colors.Grey.Lighten2).Column(dCol =>
                {
                    dCol.Item().Background(Colors.Red.Lighten5).Padding(6).Text("DEDUCTIONS & RECOVERIES")
                        .FontSize(9.5f).Bold().FontColor(Colors.Red.Darken4);

                    dCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });

                        if (slip.Deductions.Count == 0)
                        {
                            table.Cell().ColumnSpan(2).Padding(8).Text("No deductions for this period.")
                                .Italic().FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            foreach (var d in slip.Deductions)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(d.Name);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"Rs. {d.Amount:#,##0.00}");
                            }
                        }

                        var totalCombinedDeductions = slip.TotalDeductions + slip.LoanRecoveryAmount;
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(6).Text("Total Deductions").Bold();
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(6).AlignRight().Text($"Rs. {totalCombinedDeductions:#,##0.00}").Bold().FontColor(Colors.Red.Darken3);
                    });
                });
            });

            // Net Salary Highlight Strip
            col.Item().PaddingTop(14).Border(1f).BorderColor(Colors.Blue.Darken3).Background(Colors.Blue.Lighten5).Padding(12).Row(netRow =>
            {
                netRow.RelativeItem().Column(c =>
                {
                    c.Item().Text("NET PAYABLE SALARY").FontSize(10).Bold().FontColor(Colors.Blue.Darken4);
                    if (!string.IsNullOrWhiteSpace(slip.Remarks))
                        c.Item().PaddingTop(2).Text($"Remarks: {slip.Remarks}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });

                netRow.ConstantItem(180).AlignRight().AlignMiddle().Text($"Rs. {slip.NetSalary:#,##0.00}")
                    .FontSize(15).Bold().FontColor(Colors.Blue.Darken4);
            });

            // Active Loan Balance Summary
            if (slip.ActiveLoans.Count > 0)
            {
                col.Item().PaddingTop(14).Column(lCol =>
                {
                    lCol.Item().Text("LOAN / ADVANCE SUMMARY").FontSize(9.5f).Bold().FontColor(Colors.Grey.Darken3);
                    lCol.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Loan No").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Principal").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Deducted").Bold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Remaining").Bold();
                        });

                        foreach (var loan in slip.ActiveLoans)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(loan.LoanNo);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"Rs. {loan.Principal:#,##0.00}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"Rs. {loan.DeductedThisPeriod:#,##0.00}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"Rs. {loan.RemainingBalance:#,##0.00}");
                        }
                    });
                });
            }

            // Signature Block
            col.Item().PaddingTop(36).Row(sigRow =>
            {
                sigRow.RelativeItem().Column(s =>
                {
                    s.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Darken1);
                    s.Item().PaddingTop(4).AlignCenter().Text("Prepared By").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });
                sigRow.ConstantItem(32);
                sigRow.RelativeItem().Column(s =>
                {
                    s.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Darken1);
                    s.Item().PaddingTop(4).AlignCenter().Text("Authorized Signatory").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });
                sigRow.ConstantItem(32);
                sigRow.RelativeItem().Column(s =>
                {
                    s.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Darken1);
                    s.Item().PaddingTop(4).AlignCenter().Text("Employee Signature").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, PayslipModel slip)
    {
        container.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text($"System-generated payslip | Generated on {slip.GeneratedAt:dd-MMM-yyyy HH:mm}")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            row.ConstantItem(120).AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                x.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                x.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                x.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingVertical(1.5f).Row(r =>
        {
            r.ConstantItem(92).Text(label).FontSize(8.5f).SemiBold().FontColor(Colors.Grey.Darken2);
            r.RelativeItem().Text(value).FontSize(8.5f).FontColor(Colors.Grey.Darken4);
        });
    }
}
