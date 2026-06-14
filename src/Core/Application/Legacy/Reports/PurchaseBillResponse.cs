namespace Retailer.Application.Legacy.Reports;

public class PurchaseBillResponse
{
    public List<PurchaseBillLineResponse> Lines { get; set; } = new List<PurchaseBillLineResponse>();
    public PurchaseBillHeaderResponse Header { get; set; } = new PurchaseBillHeaderResponse();
    public string AccAddress { get; set; } = string.Empty;
}
