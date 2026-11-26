using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("AccountingPeriodCloses")]
public class AccountingPeriodClose : AuditableEntity, IAggregateRoot
{
    public string PeriodName { get; set; } = default!;
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly ClosingDate { get; set; }

    /// <summary>
    /// "Closed" or "Reopened"
    /// </summary>
    public string Status { get; set; } = "Closed";

    public string ClosingVoucherNo { get; set; } = default!;
    public string TargetEquityAccountId { get; set; } = default!;
    public bool ClosedDrawings { get; set; }

    // Frozen financial snapshots at the time of close
    public decimal OpeningStockValue { get; set; }
    public decimal ClosingStockValue { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfitLoss { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public bool IsBalanced { get; set; }

    public string? Notes { get; set; }

    public string? ReopenedBy { get; set; }
    public DateTime? ReopenedOn { get; set; }
    public string? ReopenReason { get; set; }
}
