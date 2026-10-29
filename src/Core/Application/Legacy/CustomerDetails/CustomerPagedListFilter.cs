using Retailer.Application.Common.Models;

namespace Retailer.Application.Legacy.CustomerDetails;

public class CustomerPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public CustomerPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public string? Search { get; set; }
    public string? CustomerCategoryId { get; set; }
    public bool? Active { get; set; }
    public string? ParentCustomerAccountId { get; set; }
}
