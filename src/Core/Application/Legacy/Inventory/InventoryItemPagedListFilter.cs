using Retailer.Application.Common.Models;

namespace Retailer.Application.Legacy.Inventory;

public class InventoryItemPagedListFilter : PaginationFilter
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;

    public InventoryItemPagedListFilter()
    {
        PageNumber = 1;
        PageSize = DefaultPageSize;
    }

    public string? Search { get; set; }
    public string? ItemCategoryCode { get; set; }
    public string? BrandId { get; set; }
    public string? ItemType { get; set; }
}
