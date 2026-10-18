namespace Retailer.Application.Legacy.Inventory;

public class ItemCustomerCategoryPriceDto
{
    public int Id { get; set; }
    public string ItemId { get; set; } = default!;
    public string CustomerCategoryId { get; set; } = default!;
    public string CustomerCategoryTitle { get; set; } = default!;
    public decimal PriRate { get; set; }
    public decimal? SecRate { get; set; }
}

public class ItemCustomerCategoryPriceUpsertRequest
{
    public string CustomerCategoryId { get; set; } = default!;
    public decimal PriRate { get; set; }
    public decimal? SecRate { get; set; }
}
