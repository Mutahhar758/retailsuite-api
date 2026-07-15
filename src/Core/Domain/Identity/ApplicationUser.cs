using Retailer.Domain.Common.Enums;
using Microsoft.AspNetCore.Identity;

namespace Retailer.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ImageUrl { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public string? ObjectId { get; set; }
    public UserStatus Status { get; set; }
    public string? BiometricPublicKey { get; set; }
    public bool IsOwner { get; set; }
    public virtual List<UserSession>? UserSessions { get; set; }
}