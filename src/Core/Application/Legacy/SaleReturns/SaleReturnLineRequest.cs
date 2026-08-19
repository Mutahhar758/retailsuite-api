namespace Retailer.Application.Legacy.SaleReturns;

public class SaleReturnLineRequest
{
    public int Seq { get; set; }
    public string ItemId { get; set; } = default!;
    public string? Unit { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public string? SecUnit { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }
    public decimal? QtyInPack { get; set; }
    public decimal? Packing { get; set; }
}
