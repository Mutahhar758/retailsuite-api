namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyLineRequest
{
    public int Seq { get; set; }
    public string CustomerId { get; set; } = default!;
    public string? Unit { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal AddLess { get; set; }
}
