namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class BalanceSheetReportItem
{
    public string Level1 { get; set; } = string.Empty;
    public string Level2 { get; set; } = string.Empty;
    public string Level3 { get; set; } = string.Empty;
    public string Level4 { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal RawBalance { get; set; }
    public decimal Amount { get; set; }
}

public class BalanceSheetHeader
{
    public string CompanyName { get; set; } = "Retail Suite Enterprise";
    public DateOnly AsOnDate { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public decimal Variance => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity);
    public bool IsBalanced => Variance < 0.01m;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
