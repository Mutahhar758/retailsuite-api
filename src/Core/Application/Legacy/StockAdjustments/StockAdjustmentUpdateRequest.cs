namespace Retailer.Application.Legacy.StockAdjustments;

public class StockAdjustmentUpdateRequest
{
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public string? Narration { get; set; }
    public List<StockAdjustmentLineRequest> Lines { get; set; } = [];
}
