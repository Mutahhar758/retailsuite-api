namespace Retailer.Application.Legacy.Reports;

public class CustomerBillFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Account { get; set; } = string.Empty;
    public string? DateBasis { get; set; }
    public string? Layout { get; set; } = "A4"; // "A4" or "Thermal"
    public bool? QrEnabled { get; set; }
    public string? QrAccountTitle { get; set; }
    public string? QrAccountNumber { get; set; }
    public string? QrBankName { get; set; }
    public string? ThankyouLine { get; set; }
}
