namespace Retailer.Application.Legacy.Payments;

public class PaymentResponse
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
    public string? CashBankAccountId { get; set; }
    public string? CashBankAccountTitle { get; set; }
    public List<PaymentAccountSummaryDto> Accounts { get; set; } = new();
}

public class PaymentAccountSummaryDto
{
    public string AccountId { get; set; } = default!;
    public string AccountTitle { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? CheckNum { get; set; }
    public string? Remarks { get; set; }
}

public class PaymentBalanceResponse
{
    public decimal Balance { get; set; }
}
