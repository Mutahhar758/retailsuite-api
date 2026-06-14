namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyCreateRequest
{
    public DateOnly Date { get; set; }
    public string ItemId { get; set; } = default!;
    public string? Description { get; set; }
    public string? Narration { get; set; }
    public List<SaleSupplyLineRequest> Lines { get; set; } = [];
}
