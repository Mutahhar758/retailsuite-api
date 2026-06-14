using Retailer.Domain.Common.Enums;

namespace Retailer.Application.Identity.Tokens;

public class TokenResponseDto
{
    public string? Token { get; set; }
    public string? RefreshToken{ get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public UserStatus? Status { get; set; }
}