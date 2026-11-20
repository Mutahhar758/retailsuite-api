namespace Retailer.Application.Legacy.RecurringChains;

public class RecurringChainUpdateRequest
{
    public string Name { get; set; } = default!;
    public string Frequency { get; set; } = "Daily";
    public DayOfWeek? DayOfWeek { get; set; }
    public List<DayOfWeek>? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public int? Interval { get; set; }
    public DateOnly? StartDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
