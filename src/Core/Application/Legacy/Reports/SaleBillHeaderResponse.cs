namespace Retailer.Application.Legacy.Reports;

public class SaleBillHeaderResponse
{
    public DateOnly VDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CashReceipt { get; set; }
    public decimal CashBack { get; set; }
    public string Descr { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
