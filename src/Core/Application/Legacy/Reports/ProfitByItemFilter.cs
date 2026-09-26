namespace Retailer.Application.Legacy.Reports;

public class ProfitByItemFilter
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? ItemId { get; set; }
    public string? CategoryId { get; set; }
}
