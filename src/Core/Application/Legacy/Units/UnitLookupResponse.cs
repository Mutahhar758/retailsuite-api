namespace Retailer.Application.Legacy.Units;

public class UnitLookupResponse
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ShortTitle { get; set; }
    public string? ParentUnitId { get; set; }
    public string? ParentUnitTitle { get; set; }
    public decimal? ConversionRatio { get; set; }
}
