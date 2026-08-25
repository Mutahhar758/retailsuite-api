namespace Retailer.Application.Legacy.CustomerDetails;

public class CustomerSupplyItemDto
{
    public string? CustomerAccountId { get; set; }
    public string ItemId { get; set; } = default!;
    public string? ItemTitle { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal? SecQty { get; set; }
    /// <summary>Override rate. Null means use the item's default rate.</summary>
    public decimal? Rate { get; set; }
    /// <summary>Override add/less amount. Null means no override (treat as 0).</summary>
    public decimal? AddLess { get; set; }
    /// <summary>Override discount (Rs). Null means no override (treat as 0).</summary>
    public decimal? Discount { get; set; }
}

