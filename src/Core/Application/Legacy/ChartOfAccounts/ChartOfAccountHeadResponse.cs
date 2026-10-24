namespace Retailer.Application.Legacy.ChartOfAccounts;

public class ChartOfAccountHeadResponse
{
    public string Account { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CustomerCategoryId { get; set; }
    public string? ParentCustomerAccountId { get; set; }
    public string? ParentCustomerTitle { get; set; }
    public bool IsSubAccount => !string.IsNullOrEmpty(ParentCustomerAccountId);
}
