namespace Retailer.Application.Legacy.StockAdjustments;

public class StockAdjustmentCreateRequest
{
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public string? Narration { get; set; }
    public List<StockAdjustmentLineRequest> Lines { get; set; } = [];
}
