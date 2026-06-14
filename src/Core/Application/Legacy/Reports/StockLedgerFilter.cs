namespace Retailer.Application.Legacy.Reports;

public class StockLedgerFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string FkItem { get; set; } = string.Empty;
}
