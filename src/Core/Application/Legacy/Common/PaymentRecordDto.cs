namespace Retailer.Application.Legacy.Common;

public class PaymentRecordRequest
{
    public int Seq { get; set; }
    public string AccountId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? CheckNum { get; set; }
    public DateOnly? CheckDate { get; set; }
}

public class PaymentRecordResponse
{
    public int Seq { get; set; }
    public string AccountId { get; set; } = default!;
    public string? AccountTitle { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? CheckNum { get; set; }
    public DateOnly? CheckDate { get; set; }
}
