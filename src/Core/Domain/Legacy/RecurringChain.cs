using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("RecurringChains")]
public class RecurringChain : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = default!;
    public string VoucherType { get; set; } = default!; // "SP" = SaleSupply, "JV" = JournalVoucher, "PV" = Payment, "RV" = Receipt
    public string Frequency { get; set; } = "Daily";    // "Daily", "Weekly", "BiWeekly", "Monthly"
    public DayOfWeek? DayOfWeek { get; set; }
    public string? DaysOfWeek { get; set; }             // Comma-separated DayOfWeek ints (0=Sun..6=Sat), e.g. "1,2,3,4,5"
    public int? DayOfMonth { get; set; }
    public int Interval { get; set; } = 1;              // Recurrence interval (e.g. 1=default, 2=bi-weekly/every 2 months)
    public DateOnly? StartDate { get; set; }            // Optional anchor/effective start date for BiWeekly/Interval schedules
    public bool IsActive { get; set; } = true;
    public DateOnly? LastRunDate { get; set; }
    public string? LastVoucherNo { get; set; }
    public string? Notes { get; set; }
}
