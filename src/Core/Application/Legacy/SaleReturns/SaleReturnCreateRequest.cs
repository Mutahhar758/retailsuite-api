namespace Retailer.Application.Legacy.SaleReturns;

public class SaleReturnCreateRequest
{
    public DateOnly Date { get; set; }
    public string Account { get; set; } = default!;
    public string? Description { get; set; }
    public string? Narration { get; set; }
    public decimal CashReceipt { get; set; }
    public decimal CashBack { get; set; }
    public List<SaleReturnLineRequest> Lines { get; set; } = [];
}
