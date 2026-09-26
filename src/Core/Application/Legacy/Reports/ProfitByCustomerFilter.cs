namespace Retailer.Application.Legacy.Reports;

public class ProfitByCustomerFilter
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? CustomerAccount { get; set; }
}
