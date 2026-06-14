namespace Retailer.Application.Legacy.Receipts;

public class ReceiptLineResponse
{
    public int Seq { get; set; }
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string CashBankAccountId { get; set; } = default!;
    public string AccountId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? Narration { get; set; }
    public string? NarrationId { get; set; }
    public string? CheckNum { get; set; }
    public DateOnly? CheckDate { get; set; }
    public string? Remarks { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
