namespace Retailer.Application.Legacy.Settings;

public class SettingResponse
{
    public string Key { get; set; } = default!;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
}
