namespace Retailer.Application.Legacy.ItemCategories;

public class ItemCategoryResponse
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string? MediaId { get; set; }
    public string? MediaUrl { get; set; }
}
