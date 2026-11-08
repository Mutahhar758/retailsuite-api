namespace Retailer.Application.Legacy.Reports;

public class VendorBillBatchFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public List<string> Accounts { get; set; } = new List<string>();
    public string? DateBasis { get; set; }
    public string? Layout { get; set; }
    public bool? QrEnabled { get; set; }
    public bool? IsWandaLayout { get; set; }
}
