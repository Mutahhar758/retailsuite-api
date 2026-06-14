namespace Retailer.Application.Legacy.PurchaseReturns;

public class PurchaseReturnLineRequest
{
    public int Seq { get; set; }
    public string ItemId { get; set; } = default!;
    public string? Unit { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
}
