using Retailer.Domain.Common.Enums;

namespace Retailer.Application.Legacy.Manufacturing;

public class ProductionPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public ProductionPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? FinishedItemId { get; set; }
    public string? VoucherNo { get; set; }
    public ProductionStatus? Status { get; set; }
    public string? VType { get; set; }
}
