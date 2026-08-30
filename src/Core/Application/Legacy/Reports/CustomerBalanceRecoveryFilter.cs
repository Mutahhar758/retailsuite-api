namespace Retailer.Application.Legacy.Reports;

public class CustomerBalanceRecoveryFilter
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string? CustomerAccountId { get; set; }
    public string? DateBasis { get; set; } = "ClearingDate"; // "ClearingDate" or "VoucherDate"
    public string? BalanceFilter { get; set; } = "All"; // "All", "OutstandingOnly", "ClearedOnly", "UnpaidOnly"
}
