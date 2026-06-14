namespace Retailer.Application.Legacy.Payments;

public class PaymentLineRequest
{
    public int Seq { get; set; }
    public string Account { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? CheckNum { get; set; }
    public DateOnly? CheckDate { get; set; }
    public string? Remarks { get; set; }
}
