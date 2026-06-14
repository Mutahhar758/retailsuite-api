namespace Retailer.Application.Identity.Users.Password;

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public bool LogOutOfAllAccounts { get; set; }
}