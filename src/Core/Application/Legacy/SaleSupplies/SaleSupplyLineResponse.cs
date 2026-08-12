namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyLineResponse
{
    public int Seq { get; set; }
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public string? ItemTitle { get; set; }
    public string? Narration { get; set; }
    public string? NarrationId { get; set; }
    public string? Description { get; set; }
    public int? SupplyOrderMasterId { get; set; }
    public string CustomerId { get; set; } = default!;
    public string? CustomerTitle { get; set; }
    public string? Unit { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal AddLess { get; set; }
    public decimal Amount { get; set; }
    public string? SecUnit { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }
    public decimal? QtyInPack { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
