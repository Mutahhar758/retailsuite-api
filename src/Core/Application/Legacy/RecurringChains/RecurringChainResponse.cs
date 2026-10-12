namespace Retailer.Application.Legacy.RecurringChains;

public class RecurringChainResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string VoucherType { get; set; } = default!;
    public string Frequency { get; set; } = default!;
    public DayOfWeek? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? LastRunDate { get; set; }
    public string? LastVoucherNo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedOn { get; set; }
}
