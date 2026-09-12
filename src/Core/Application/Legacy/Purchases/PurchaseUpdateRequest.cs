namespace Retailer.Application.Legacy.Purchases;

public class PurchaseUpdateRequest
{
    public DateOnly Date { get; set; }
    public string Account { get; set; } = default!;
    public string? Description { get; set; }
    public string? Narration { get; set; }
    public decimal CashPaid { get; set; }
    public decimal CashBack { get; set; }
    public List<PurchaseLineRequest> Lines { get; set; } = [];
}
