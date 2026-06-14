namespace Retailer.Application.Legacy.StockAdjustments;

public class StockAdjustmentListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? ItemCategoryCode { get; set; }
    public string? VoucherNo { get; set; }
}
