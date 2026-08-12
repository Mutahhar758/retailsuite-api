namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyCustomerLineUpdateRequest
{
    public string VoucherNo { get; set; } = default!;
    public int Seq { get; set; }
    public SaleSupplyLineRequest Line { get; set; } = default!;
}
