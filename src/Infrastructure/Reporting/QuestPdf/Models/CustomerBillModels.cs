using Retailer.Application.Legacy.Reports;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public enum CustomerBillPrintLayout
{
    A4Sheet,
    Thermal80mm
}

public class CustomerBillHeader
{
    public string CompanyName { get; set; } = "Company";
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string CustomerAccount { get; set; } = string.Empty;
    public string CustomerTitle { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string DateBasis { get; set; } = "Clearing Date";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public decimal PreviousBalance { get; set; }
    public decimal TotalBilling { get; set; }
    public decimal Payment { get; set; }
    public decimal ClosingBalance { get; set; }
    public string? ThankyouLine { get; set; }
    public CustomerBillPrintLayout Layout { get; set; } = CustomerBillPrintLayout.A4Sheet;
    public QrPaymentInfo? QrPayment { get; set; }
    public bool ShowQrPayment => QrPayment != null && QrPayment.IsEnabled && ClosingBalance > 0;
    public bool IsWandaLayout { get; set; }
}
