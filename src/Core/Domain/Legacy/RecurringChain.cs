using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("RecurringChains")]
public class RecurringChain : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = default!;
    public string VoucherType { get; set; } = default!; // "SP" = SaleSupply, "JV" = JournalVoucher, "PV" = Payment, "RV" = Receipt
    public string Frequency { get; set; } = "Daily";    // "Daily", "Weekly", "Monthly"
    public DayOfWeek? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? LastRunDate { get; set; }
    public string? LastVoucherNo { get; set; }
    public string? Notes { get; set; }
}
