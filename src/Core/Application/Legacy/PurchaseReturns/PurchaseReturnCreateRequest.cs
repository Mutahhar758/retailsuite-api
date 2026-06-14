namespace Retailer.Application.Legacy.PurchaseReturns;

public class PurchaseReturnCreateRequest
{
    public DateOnly Date { get; set; }
    public string Account { get; set; } = default!;
    public string? Description { get; set; }
    public string? Narration { get; set; }
    public List<PurchaseReturnLineRequest> Lines { get; set; } = [];
}
