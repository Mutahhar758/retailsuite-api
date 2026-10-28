namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public SaleSupplyPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? ItemId { get; set; }
    public string? VoucherNo { get; set; }
}
