namespace Retailer.Application.Legacy.StockAdjustments;

public class StockAdjustmentPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public StockAdjustmentPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? ItemCategoryCode { get; set; }
    public string? VoucherNo { get; set; }
}
