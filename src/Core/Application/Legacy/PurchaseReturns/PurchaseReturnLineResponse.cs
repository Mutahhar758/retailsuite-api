namespace Retailer.Application.Legacy.PurchaseReturns;

public class PurchaseReturnLineResponse
{
    public int Seq { get; set; }
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string AccountId { get; set; } = default!;
    public string? Narration { get; set; }
    public string? NarrationId { get; set; }
    public string? Description { get; set; }
    public string ItemId { get; set; } = default!;
    public string? ItemKey { get; set; }
    public string ItemCategoryCode { get; set; } = default!;
    public string? Unit { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string? SecUnit { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }
    public decimal? QtyInPack { get; set; }
    public decimal? Packing { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
