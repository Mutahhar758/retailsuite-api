namespace Retailer.Application.Legacy.ChartOfAccounts;

public class ChartOfAccountResponse
{
    public string Account { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ParentId { get; set; } = string.Empty;
    public string AccType { get; set; } = string.Empty;
    public int AccLevel { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
