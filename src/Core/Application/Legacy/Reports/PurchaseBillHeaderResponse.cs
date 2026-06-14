namespace Retailer.Application.Legacy.Reports;

public class PurchaseBillHeaderResponse
{
    public DateOnly VDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
}
