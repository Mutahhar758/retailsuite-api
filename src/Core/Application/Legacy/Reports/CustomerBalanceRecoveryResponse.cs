namespace Retailer.Application.Legacy.Reports;

public class CustomerBalanceRecoveryResponse
{
    public List<CustomerBalanceRecoveryLineResponse> Lines { get; set; } = new();
    public CustomerBalanceRecoverySummaryResponse Summary { get; set; } = new();
}

public class CustomerBalanceRecoveryLineResponse
{
    public string CustomerAccountId { get; set; } = string.Empty;
    public string CustomerTitle { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal CurrentBilling { get; set; }
    public decimal TotalDue { get; set; }
    public decimal RecoveryAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal RecoveryPercentage { get; set; }
    public string Status { get; set; } = string.Empty; // "Cleared", "Partial", "Unpaid", "Advance"
}

public class CustomerBalanceRecoverySummaryResponse
{
    public int TotalCustomers { get; set; }
    public decimal TotalPreviousBalance { get; set; }
    public decimal TotalCurrentBilling { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalRecovery { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalClosingBalance { get; set; }
    public decimal OverallRecoveryRate { get; set; }
}
