namespace Retailer.Application.Legacy.StockAdjustments;

public class StockAdjustmentLineResponse
{
    public int Seq { get; set; }
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string? Narration { get; set; }
    public string? NarrationId { get; set; }
    public string? Description { get; set; }
    public string ItemCategoryCode { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string? ItemKey { get; set; }
    public string? Unit { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
