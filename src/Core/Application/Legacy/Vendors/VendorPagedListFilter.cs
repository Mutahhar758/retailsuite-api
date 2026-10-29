using Retailer.Application.Common.Models;

namespace Retailer.Application.Legacy.Vendors;

public class VendorPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public VendorPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public string? Search { get; set; }
    public bool? Active { get; set; }
    public bool? ShowInSales { get; set; }
}
