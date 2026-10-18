namespace Retailer.Application.Legacy.CustomerCategories;

public class CustomerCategoryUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; }
}
