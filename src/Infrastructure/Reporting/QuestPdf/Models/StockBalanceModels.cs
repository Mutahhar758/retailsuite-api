namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class StockBalanceReportItem
{
    public int Index { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Pcs";
    public decimal OpeningQty { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal ClosingQty { get; set; }
    public string? OpeningQtyDisplay { get; set; }
    public string? QtyInDisplay { get; set; }
    public string? QtyOutDisplay { get; set; }
    public string? ClosingQtyDisplay { get; set; }
    public decimal Rate { get; set; }
    public decimal TotalValue => ClosingQty * Rate;
    public decimal? LowStockQty { get; set; }
    public bool? LowStockAlert { get; set; }
    public string Status { get; set; } = "In Stock";
}

public class StockBalanceHeader
{
    public string CompanyName { get; set; } = "Retail Suite Enterprise";
    public string CategoryName { get; set; } = "All Categories";
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalOpeningQty { get; set; }
    public decimal TotalQtyIn { get; set; }
    public decimal TotalQtyOut { get; set; }
    public decimal TotalClosingQty { get; set; }
    public string? TotalOpeningQtyDisplay { get; set; }
    public string? TotalQtyInDisplay { get; set; }
    public string? TotalQtyOutDisplay { get; set; }
    public string? TotalClosingQtyDisplay { get; set; }
    public decimal TotalStockValue { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
