namespace Retailer.Application.Legacy.PurchaseReturns;

public class PurchaseReturnLineRequest
{
    public int Seq { get; set; }
    public string ItemId { get; set; } = default!;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public string? SecUnit { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }
    public decimal? QtyInPack { get; set; }
    public decimal? Packing { get; set; }
    public string? Imei { get; set; }
    public string? Imei2 { get; set; }
}
