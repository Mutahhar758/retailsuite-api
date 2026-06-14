namespace Retailer.Application.Legacy.Sales;

public class SaleCreateRequest
{
    public DateOnly Date { get; set; }
    public string Account { get; set; } = default!;
    public string? Description { get; set; }
    public string? Narration { get; set; }
    public decimal CashReceipt { get; set; }
    public decimal CashBack { get; set; }
    public List<SaleLineRequest> Lines { get; set; } = [];
}
