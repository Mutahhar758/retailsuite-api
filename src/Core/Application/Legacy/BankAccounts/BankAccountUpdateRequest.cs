namespace Retailer.Application.Legacy.BankAccounts;

public class BankAccountUpdateRequest
{
    public string? BankName { get; set; }
    public string? AccountTitle { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
    public bool? Active { get; set; }
}
