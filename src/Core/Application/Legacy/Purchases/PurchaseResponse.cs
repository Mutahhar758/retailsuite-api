namespace Retailer.Application.Legacy.Purchases;

public class PurchaseResponse
{
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string Account { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
