namespace Retailer.Application.Legacy.Reports;

public class WandaCustomerBillSummaryResponse
{
    public decimal PreviousBalance { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalSaleReturns { get; set; }
    public decimal TotalPurchaseReturns { get; set; }
    public decimal TotalBill { get; set; }
    public decimal Payment { get; set; }
    public decimal Balance { get; set; }
}
