using Retailer.Application.Legacy.Reports;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public enum VendorBillPrintLayout
{
    A4Sheet,
    Thermal80mm
}

public class VendorBillHeader
{
    public string CompanyName { get; set; } = "Company";
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string VendorAccount { get; set; } = string.Empty;
    public string VendorTitle { get; set; } = string.Empty;
    public string? VendorPhone { get; set; }
    public string? VendorAddress { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string DateBasis { get; set; } = "Voucher Date";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public decimal PreviousBalance { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalPurchaseReturns { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalSaleReturns { get; set; }
    public decimal TotalBilling { get; set; }
    public decimal Payment { get; set; }
    public decimal ClosingBalance { get; set; }
    public string? ThankyouLine { get; set; }
    public VendorBillPrintLayout Layout { get; set; } = VendorBillPrintLayout.A4Sheet;
    public bool IsWandaLayout { get; set; }
    public bool EnableCarriage { get; set; }
}
