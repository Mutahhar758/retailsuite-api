namespace Retailer.Application.Identity.Tokens;
public class DeviceInfoRequest
{
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? FcmToken { get; set; }
    public string? AppVersion { get; set; }
}
