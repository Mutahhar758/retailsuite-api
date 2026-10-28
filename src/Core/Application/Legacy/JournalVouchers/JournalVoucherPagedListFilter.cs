namespace Retailer.Application.Legacy.JournalVouchers;

public class JournalVoucherPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public JournalVoucherPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Account { get; set; }
    public string? Narration { get; set; }
    public string? VoucherNo { get; set; }
}
