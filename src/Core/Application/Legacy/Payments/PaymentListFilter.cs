namespace Retailer.Application.Legacy.Payments;

public class PaymentListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? CashBankAccount { get; set; }
    public string? Account { get; set; }
    public string? Narration { get; set; }
}
