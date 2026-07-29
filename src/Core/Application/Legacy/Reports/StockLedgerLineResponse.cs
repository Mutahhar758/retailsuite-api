namespace Retailer.Application.Legacy.Reports;

public class StockLedgerLineResponse
{
    public DateOnly Vdate { get; set; }
    public string? Vno { get; set; }
    public string Particular { get; set; } = string.Empty;
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal? Rate { get; set; }

    public string? SecUnit { get; set; }
    public decimal SecQtyIn { get; set; }
    public decimal SecQtyOut { get; set; }
    public decimal SecQtyBal { get; set; }
}
