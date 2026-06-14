namespace Retailer.Application.Legacy.JournalVouchers;

public class JournalVoucherLineRequest
{
    public int Seq { get; set; }
    public string DrAccount { get; set; } = default!;
    public string CrAccount { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
}
