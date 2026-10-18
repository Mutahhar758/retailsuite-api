namespace Retailer.Application.Legacy.CustomerCategories;

public class CustomerCategoryCreateRequest
{
    public string? Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
}
