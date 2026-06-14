namespace Retailer.Application.Legacy.Reports;

public class IncomeSummaryLineResponse
{
    public string VType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Dr { get; set; }
    public decimal Cr { get; set; }
    public decimal Bal { get; set; }
}
