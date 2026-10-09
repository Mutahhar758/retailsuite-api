namespace Retailer.Application.Legacy.Reports;

public class CustomerBillFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Account { get; set; } = string.Empty;
    public string? DateBasis { get; set; }
    public string? Layout { get; set; } // "A4" or "Thermal" - if null, resolves from tenant Bill.DefaultFormat
    public bool? QrEnabled { get; set; }
    public bool? IsWandaLayout { get; set; }
}
