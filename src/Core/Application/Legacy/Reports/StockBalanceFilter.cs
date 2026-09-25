namespace Retailer.Application.Legacy.Reports;

public class StockBalanceFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? Catagory { get; set; }
    public string Filter { get; set; } = "All";
    public decimal Qty { get; set; }
    public string? Type { get; set; }
    /// <summary>
    /// When true, the Rate and Total Stock Value columns are included in the PDF report.
    /// Defaults to true so the report shows FIFO inventory valuation by default.
    /// </summary>
    public bool ShowStockValue { get; set; } = true;
}
