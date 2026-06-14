namespace Retailer.Application.Legacy.JournalVouchers;

public class JournalVoucherUpdateRequest
{
    public DateOnly Date { get; set; }
    public string? Narration { get; set; }
    public List<JournalVoucherLineRequest> Lines { get; set; } = [];
}
