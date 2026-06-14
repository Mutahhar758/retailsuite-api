namespace Retailer.Application.Legacy.StockAdjustments;

public class StockAdjustmentResponse
{
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string? Narration { get; set; }
    public string? NarrationId { get; set; }
    public string Description { get; set; } = default!;
    public decimal TotalQty { get; set; }
    public decimal TotalAmount { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
