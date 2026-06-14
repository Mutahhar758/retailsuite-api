namespace Retailer.Application.Legacy.Reports;

public class BalanceDetailFilter
{
    public DateOnly ToDate { get; set; }
    public string Account { get; set; } = string.Empty;
}
