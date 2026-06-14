using System.ComponentModel.DataAnnotations;

namespace Retailer.Application.Identity.Tokens;

public enum LoginType
{
    Username = 1,
    Email = 2
}

public class TokenRequest : DeviceInfoRequest
{
    [Required]
    public string Login { get; set; } = default!;
    public LoginType LoginType { get; set; } = LoginType.Email;
    [Required]
    public string Password { get; set; } = default!;
};