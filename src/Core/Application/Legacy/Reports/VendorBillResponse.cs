namespace Retailer.Application.Legacy.Reports;

public class VendorBillResponse
{
    public List<VendorBillLineResponse> Lines { get; set; } = new List<VendorBillLineResponse>();
    public VendorBillSummaryResponse Summary { get; set; } = new VendorBillSummaryResponse();
}
