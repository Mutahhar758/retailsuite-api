namespace Retailer.Application.Legacy.Reports;

public class VendorBillSummaryResponse
{
    public decimal PreviousBalance { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal Payment { get; set; }
    public decimal Balance { get; set; }
}
