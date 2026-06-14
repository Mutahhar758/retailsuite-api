namespace Retailer.Application.Legacy.Reports;

public class StockLedgerLineResponse
{
    public DateOnly Vdate { get; set; }
    public string? Vno { get; set; }
    public string Particular { get; set; } = string.Empty;
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal? Rate { get; set; }
}
