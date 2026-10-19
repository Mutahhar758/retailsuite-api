namespace Retailer.Application.Legacy.Units;

public class UnitUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? ShortTitle { get; set; }
    public string? ParentUnitId { get; set; }
    public decimal? ConversionRatio { get; set; }
    public bool Active { get; set; }
}
