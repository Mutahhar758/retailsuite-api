namespace Retailer.Application.Legacy.Reports;

public class AccountStatementFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Account { get; set; } = string.Empty;
    public string? DateBasis { get; set; }
}
