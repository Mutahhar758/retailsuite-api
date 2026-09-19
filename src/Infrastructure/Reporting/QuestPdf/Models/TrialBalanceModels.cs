namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class TrialBalanceReportItem
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountTitle { get; set; } = string.Empty;
    public string Level1 { get; set; } = string.Empty;
    public string Level2 { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal ClosingDebit => ClosingBalance > 0 ? ClosingBalance : 0m;
    public decimal ClosingCredit => ClosingBalance < 0 ? Math.Abs(ClosingBalance) : 0m;
}

public class TrialBalanceHeader
{
    public string CompanyName { get; set; } = "Retail Suite Enterprise";
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalAccounts { get; set; }
    public decimal TotalOpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalClosingDebit { get; set; }
    public decimal TotalClosingCredit { get; set; }
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
