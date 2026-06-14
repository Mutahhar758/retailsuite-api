namespace Retailer.Application.Legacy.PurchaseReturns;

public class PurchaseReturnListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Account { get; set; }
    public string? VoucherNo { get; set; }
}
