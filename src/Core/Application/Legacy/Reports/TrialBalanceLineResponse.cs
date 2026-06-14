namespace Retailer.Application.Legacy.Reports;

public class TrialBalanceLineResponse
{
    public string Lvl1 { get; set; } = string.Empty;
    public string Lvl2 { get; set; } = string.Empty;
    public string Lvl3 { get; set; } = string.Empty;
    public string Lvl4 { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal PriBal { get; set; }
    public decimal Dr { get; set; }
    public decimal Cr { get; set; }
    public decimal CurBal { get; set; }
}
