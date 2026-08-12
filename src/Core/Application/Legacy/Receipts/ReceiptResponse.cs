namespace Retailer.Application.Legacy.Receipts;

public class ReceiptResponse
{
    public DateOnly Date { get; set; }
    public DateOnly? ClearingDate { get; set; }
    public string VoucherNo { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? Narration { get; set; }
    public string? NarrationId { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}

public class ReceiptBalanceResponse
{
    public decimal Balance { get; set; }
}
