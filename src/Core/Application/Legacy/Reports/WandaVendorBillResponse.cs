namespace Retailer.Application.Legacy.Reports;

public class WandaVendorBillResponse
{
    public List<WandaVendorBillLineResponse> Lines { get; set; } = new List<WandaVendorBillLineResponse>();
    public WandaVendorBillSummaryResponse Summary { get; set; } = new WandaVendorBillSummaryResponse();
    public string? LinkedMainAccountId { get; set; }
    public string? LinkedMainAccountTitle { get; set; }
    public decimal? LinkedMainAccountBalance { get; set; }
    public bool IsSubAccount => !string.IsNullOrEmpty(LinkedMainAccountId);
}
