namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class ProfitByCustomerReportItem
{
    public string AccountId { get; set; } = default!;
    public string AccountTitle { get; set; } = default!;
    public string? City { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalQty { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit => TotalSales - TotalCost;
    public decimal GrossMarginPct => TotalSales > 0 ? Math.Round((GrossProfit / TotalSales) * 100m, 2) : 0m;
}

public class ProfitByCustomerReportHeader
{
    public string CompanyName { get; set; } = "RETAIL SUITE";
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? CustomerFilter { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit => TotalSales - TotalCost;
    public decimal GrossMarginPct => TotalSales > 0 ? Math.Round((GrossProfit / TotalSales) * 100m, 2) : 0m;
    public decimal TotalQtySold { get; set; }
    public int TotalCustomers { get; set; }
    public bool IsProfitable => GrossProfit >= 0;
}
