namespace Retailer.Application.Legacy.RecurringChains;

public class RecurringChainUpdateRequest
{
    public string Name { get; set; } = default!;
    public string Frequency { get; set; } = "Daily";
    public DayOfWeek? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
