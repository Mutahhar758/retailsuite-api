namespace Retailer.Application.Legacy.Receipts;

public class ReceiptCreateRequest
{
    public DateOnly Date { get; set; }
    public string CashBankAccount { get; set; } = default!;
    public string? Narration { get; set; }
    public List<ReceiptLineRequest> Lines { get; set; } = [];
}
