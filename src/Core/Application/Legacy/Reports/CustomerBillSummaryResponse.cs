namespace Retailer.Application.Legacy.Reports;

public class CustomerBillSummaryResponse
{
    public decimal PreviousBalance { get; set; }
    public decimal Payment { get; set; }
    public decimal Balance { get; set; }
}
