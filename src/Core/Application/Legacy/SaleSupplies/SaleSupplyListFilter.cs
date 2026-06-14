namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? ItemId { get; set; }
    public string? VoucherNo { get; set; }
}
