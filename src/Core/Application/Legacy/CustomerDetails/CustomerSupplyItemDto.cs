namespace Retailer.Application.Legacy.CustomerDetails;

public class CustomerSupplyItemDto
{
    public string? CustomerAccountId { get; set; }
    public string ItemId { get; set; } = default!;
    public string? ItemTitle { get; set; }
    public decimal Qty { get; set; } = 1;
    public decimal? SecQty { get; set; }
}

