namespace Retailer.Application.Legacy.Reports;

public class PurchaseSupplyComparisonFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? ItemId { get; set; }
}
