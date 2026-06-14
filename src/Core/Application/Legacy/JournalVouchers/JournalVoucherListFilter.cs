namespace Retailer.Application.Legacy.JournalVouchers;

public class JournalVoucherListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Account { get; set; }
    public string? Narration { get; set; }
}
