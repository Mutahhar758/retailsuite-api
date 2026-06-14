namespace Retailer.Application.Legacy.Reports;

public class SaleBillResponse
{
    public List<SaleBillLineResponse> Lines { get; set; } = new List<SaleBillLineResponse>();
    public SaleBillHeaderResponse Header { get; set; } = new SaleBillHeaderResponse();
    public string AccAddress { get; set; } = string.Empty;
}
