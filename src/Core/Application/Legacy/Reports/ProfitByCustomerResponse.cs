namespace Retailer.Application.Legacy.Reports;

public class ProfitByCustomerResponse
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit => TotalSales - TotalCost;
    public decimal GrossMarginPct => TotalSales > 0 ? Math.Round((GrossProfit / TotalSales) * 100m, 2) : 0m;
    public decimal TotalQtySold { get; set; }
    public int CustomerCount => Lines.Count;
    public List<ProfitByCustomerLineResponse> Lines { get; set; } = [];
}

public class ProfitByCustomerLineResponse
{
    public string AccountId { get; set; } = default!;
    public string AccountTitle { get; set; } = default!;
    public string? City { get; set; }
    public string? Phone { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalQty { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit => TotalSales - TotalCost;
    public decimal GrossMarginPct => TotalSales > 0 ? Math.Round((GrossProfit / TotalSales) * 100m, 2) : 0m;
    public List<ProfitByCustomerDetailLineResponse>? Details { get; set; }
}

public class ProfitByCustomerDetailLineResponse
{
    public DateOnly VDate { get; set; }
    public string VNo { get; set; } = default!;
    public string VType { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string ItemTitle { get; set; } = default!;
    public string? Unit { get; set; }
    public decimal Qty { get; set; }
    public decimal SaleRate { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal CostPrice { get; set; }
    public decimal CostAmount { get; set; }
    public decimal Profit => SaleAmount - CostAmount;
    public decimal MarginPct => SaleAmount > 0 ? Math.Round((Profit / SaleAmount) * 100m, 2) : 0m;
}
