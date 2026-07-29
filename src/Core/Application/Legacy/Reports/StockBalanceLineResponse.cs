namespace Retailer.Application.Legacy.Reports;

public class StockBalanceLineResponse
{
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
}
