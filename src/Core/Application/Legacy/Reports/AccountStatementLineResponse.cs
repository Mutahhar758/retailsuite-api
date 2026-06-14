namespace Retailer.Application.Legacy.Reports;

public class AccountStatementLineResponse
{
    public DateOnly VDate { get; set; }
    public string? VNo { get; set; }
    public int VSeq { get; set; }
    public string Particular { get; set; } = string.Empty;
    public decimal Dr { get; set; }
    public decimal Cr { get; set; }
}
