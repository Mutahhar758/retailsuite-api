namespace Retailer.Application.Legacy.ItemCategories;

public class ItemCategoryLookupResponse
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? MediaId { get; set; }
    public string? MediaUrl { get; set; }
}
