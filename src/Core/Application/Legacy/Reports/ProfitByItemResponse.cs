namespace Retailer.Application.Legacy.Reports;

public class ProfitByItemResponse
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit => TotalSales - TotalCost;
    public decimal GrossMarginPct => TotalSales > 0 ? Math.Round((GrossProfit / TotalSales) * 100m, 2) : 0m;
    public decimal TotalQtySold { get; set; }
    public int ItemCount => Lines.Count;
    public List<ProfitByItemLineResponse> Lines { get; set; } = [];
}

public class ProfitByItemLineResponse
{
    public string ItemId { get; set; } = default!;
    public string ItemTitle { get; set; } = default!;
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public decimal TotalQty { get; set; }
    public decimal AvgSaleRate => TotalQty > 0 ? Math.Round(TotalSales / TotalQty, 2) : 0m;
    public decimal TotalSales { get; set; }
    public decimal AvgCostRate => TotalQty > 0 ? Math.Round(TotalCost / TotalQty, 2) : 0m;
    public decimal TotalCost { get; set; }
    public decimal GrossProfit => TotalSales - TotalCost;
    public decimal GrossMarginPct => TotalSales > 0 ? Math.Round((GrossProfit / TotalSales) * 100m, 2) : 0m;
}
