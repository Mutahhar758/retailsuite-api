namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyResponse
{
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string Item { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
