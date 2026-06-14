namespace Retailer.Application.Legacy.StockAdjustments;

public class StockAdjustmentLineRequest
{
    public int Seq { get; set; }
    public string ItemCategoryCode { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string? Unit { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal Rate { get; set; }
}
