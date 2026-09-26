namespace Retailer.Application.Legacy.Reports;

public class CustomerBillBatchFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public List<string> Accounts { get; set; } = new();
    public string? DateBasis { get; set; }
    public string? Layout { get; set; } = "A4"; // "A4" or "Thermal"
    public bool? QrEnabled { get; set; }
    public bool OnlyWithActivity { get; set; } = true;
    public bool? IsWandaLayout { get; set; }
}
