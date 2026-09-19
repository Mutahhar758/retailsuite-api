namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class IncomeSummaryLineItem
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Amount { get; set; }
}

public class IncomeSummaryHeader
{
    public string CompanyName { get; set; } = "Retail Suite Enterprise";
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal GrossProfit => TotalSales - TotalCogs;
    public decimal GrossMarginPct => TotalSales > 0 ? (GrossProfit / TotalSales) * 100m : 0m;
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome => GrossProfit - TotalExpenses;
    public decimal NetMarginPct => TotalSales > 0 ? (NetIncome / TotalSales) * 100m : 0m;
    public bool IsProfitable => NetIncome >= 0;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
