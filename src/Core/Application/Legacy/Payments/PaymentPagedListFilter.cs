namespace Retailer.Application.Legacy.Payments;

public class PaymentPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public PaymentPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? CashBankAccount { get; set; }
    public string? Account { get; set; }
    public string? Narration { get; set; }
    public string? VoucherNo { get; set; }
}
