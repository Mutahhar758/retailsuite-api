using Retailer.Domain.Common.Enums;

namespace Retailer.Domain.Identity;

public class UserSession : AuditableEntity<string>, IAggregateRoot
{
    public UserSession() => Id = Guid.NewGuid().ToString();
    public virtual string? ApplicationUserId { get; set; }

    public string? Token { get; set; }

    public string? DeviceId { get; set; }

    public string? FCMToken { get; set; }

    public string? DeviceName { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public SessionType Type { get; set; }

    public bool IsRemember { get; set; }

    public bool ExplicitLogout { get; set; }

    public string? Version { get; set; }

    public virtual ApplicationUser? ApplicationUser { get; set; }
}
