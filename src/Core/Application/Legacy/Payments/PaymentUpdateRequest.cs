namespace Retailer.Application.Legacy.Payments;

public class PaymentUpdateRequest
{
    public DateOnly Date { get; set; }
    public DateOnly? ClearingDate { get; set; }
    public string CashBankAccount { get; set; } = default!;
    public string? Narration { get; set; }
    public List<PaymentLineRequest> Lines { get; set; } = [];
}
