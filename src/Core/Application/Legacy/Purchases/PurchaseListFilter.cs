namespace Retailer.Application.Legacy.Purchases;

public class PurchaseListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Account { get; set; }
    public string? VoucherNo { get; set; }
}
