namespace Retailer.Application.Legacy.Reports;

public class StockBalanceLineResponse
{
    public string? ItemId { get; set; }
    public string Item { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal PriQty { get; set; }
    public decimal Qty { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal QtyBal { get; set; }
    public decimal Rate { get; set; }

    public string? SecUnit { get; set; }
    public decimal SecPriQty { get; set; }
    public decimal SecQtyIn { get; set; }
    public decimal SecQtyOut { get; set; }
    public decimal SecQtyBal { get; set; }

    public string? PriQtyDisplay { get; set; }
    public string? QtyInDisplay { get; set; }
    public string? QtyOutDisplay { get; set; }
    public string? QtyBalDisplay { get; set; }

    public bool? LowStockAlert { get; set; }
    public decimal? LowStockQty { get; set; }
    public string Status { get; set; } = "In Stock";
}
