namespace Retailer.Application.Legacy.BankAccounts;

public class BankAccountCreateRequest
{
    public string AccountType { get; set; } = "Bank"; // "Cash" or "Bank"
    public string? BankName { get; set; }
    public string AccountTitle { get; set; } = default!;
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public string? Currency { get; set; } = "PKR";
    public string? Notes { get; set; }
    public bool Active { get; set; } = true;
    public decimal? OpeningBalance { get; set; }
}
