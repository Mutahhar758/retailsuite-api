namespace Retailer.Application.Legacy.Reports;

public class StockBalanceFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? Catagory { get; set; }
    public string Filter { get; set; } = "All";
    public decimal Qty { get; set; }
    public string? Type { get; set; }
}
