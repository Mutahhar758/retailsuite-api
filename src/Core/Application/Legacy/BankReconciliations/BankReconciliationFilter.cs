namespace Retailer.Application.Legacy.BankReconciliations;

public class BankReconciliationFilter
{
    public string BankAccount { get; set; } = default!;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}
