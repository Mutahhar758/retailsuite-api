namespace Retailer.Application.Legacy.Reports;

public class VendorBillFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Account { get; set; } = string.Empty;
    public string? DateBasis { get; set; }
    public string? Layout { get; set; }
    public bool? QrEnabled { get; set; }
    public bool? IsWandaLayout { get; set; }
}
