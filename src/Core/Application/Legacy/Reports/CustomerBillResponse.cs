namespace Retailer.Application.Legacy.Reports;

public class CustomerBillResponse
{
    public List<CustomerBillLineResponse> Lines { get; set; } = new List<CustomerBillLineResponse>();
    public CustomerBillSummaryResponse Summary { get; set; } = new CustomerBillSummaryResponse();
}
