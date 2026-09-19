namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class AccountBalanceReportItem
{
    public int Index { get; set; }
    public string AccountTitle { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string Nature => Balance >= 0 ? "Dr" : "Cr";
}

public class AccountBalanceHeader
{
    public string CompanyName { get; set; } = "Retail Suite Enterprise";
    public string AccountHeadTitle { get; set; } = "Account Head";
    public string AccountHeadId { get; set; } = string.Empty;
    public DateOnly AsOnDate { get; set; }
    public int TotalAccounts { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal NetBalance => TotalDebit - TotalCredit;
    public string NetNature => NetBalance >= 0 ? "Dr" : "Cr";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
