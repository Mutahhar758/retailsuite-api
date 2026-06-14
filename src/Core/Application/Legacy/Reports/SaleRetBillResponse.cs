namespace Retailer.Application.Legacy.Reports;

public class SaleRetBillResponse
{
    public List<SaleRetBillLineResponse> Lines { get; set; } = new List<SaleRetBillLineResponse>();
    public SaleRetBillHeaderResponse Header { get; set; } = new SaleRetBillHeaderResponse();
    public string AccAddress { get; set; } = string.Empty;
}
