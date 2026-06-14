namespace Retailer.Application.Legacy.Reports;

public class CustomerBillFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Account { get; set; } = string.Empty;
}
