namespace Retailer.Application.Legacy.ItemCategories;

public class ItemCategoryUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string? MediaId { get; set; }
}
