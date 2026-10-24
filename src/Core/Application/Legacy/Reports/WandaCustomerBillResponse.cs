namespace Retailer.Application.Legacy.Reports;

public class WandaCustomerBillResponse
{
    public List<WandaCustomerBillLineResponse> Lines { get; set; } = new List<WandaCustomerBillLineResponse>();
    public WandaCustomerBillSummaryResponse Summary { get; set; } = new WandaCustomerBillSummaryResponse();
    public string? LinkedMainAccountId { get; set; }
    public string? LinkedMainAccountTitle { get; set; }
    public decimal? LinkedMainAccountBalance { get; set; }
    public bool IsSubAccount => !string.IsNullOrEmpty(LinkedMainAccountId);
}
