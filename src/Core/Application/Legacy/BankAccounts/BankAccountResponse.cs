namespace Retailer.Application.Legacy.BankAccounts;

public class BankAccountResponse
{
    public string Account { get; set; } = default!;
    public string? BankName { get; set; }
    public string AccountTitle { get; set; } = default!;
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; } = true;
    public decimal CurrentBalance { get; set; }
    public decimal OpeningBalance { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
