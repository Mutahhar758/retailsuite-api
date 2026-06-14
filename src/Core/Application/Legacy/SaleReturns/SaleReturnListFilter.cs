namespace Retailer.Application.Legacy.SaleReturns;

public class SaleReturnListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Account { get; set; }
    public string? VoucherNo { get; set; }
}
