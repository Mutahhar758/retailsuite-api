namespace Retailer.Application.Legacy.RecurringChains;

public class RecurringChainCreateRequest
{
    public string Name { get; set; } = default!;
    public string VoucherType { get; set; } = default!; // "SP", "JV", etc.
    public string SourceVoucherNo { get; set; } = default!;
    public string Frequency { get; set; } = "Daily";    // "Daily", "Weekly", "Monthly"
    public DayOfWeek? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public string? Notes { get; set; }
}
