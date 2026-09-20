namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class StockLedgerReportItem
{
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public string Particular { get; set; } = string.Empty;
    public decimal? Rate { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? CostAmount { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal Balance { get; set; }
}

public class StockLedgerHeader
{
    public string CompanyName { get; set; } = "Retail Suite Enterprise";
    public string ItemId { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = "Inventory Item";
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal ClosingBalance { get; set; }
    public bool ShowCostPrice { get; set; } = false;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
