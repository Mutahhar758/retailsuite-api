namespace Retailer.Application.Legacy.CustomerCategories;

public class CustomerCategoryResponse
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; }
}
