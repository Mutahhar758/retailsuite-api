using Retailer.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace Retailer.Infrastructure.Identity.Settings;
public class FourDigitTokenProvider : PhoneNumberTokenProvider<ApplicationUser>
{
    public static string FourDigitPhone = "4DigitPhone";
    public static string FourDigitEmail = "4DigitEmail";

    public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        return Task.FromResult(false);
    }

    public override async Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        var token = new SecurityToken(await manager.CreateSecurityTokenAsync(user));
        var modifier = await GetUserModifierAsync(purpose, manager, user);
        var code = Rfc6238AuthenticationService.GenerateCode(token, modifier, 4).ToString("D4", CultureInfo.InvariantCulture);

        return code;
    }

    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        int code;
        if (!int.TryParse(token, out code))
        {
            return false;
        }

        var securityToken = new SecurityToken(await manager.CreateSecurityTokenAsync(user));
        var modifier = await GetUserModifierAsync(purpose, manager, user);
        var valid = Rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier, token.Length);
        return valid;
    }

    public override Task<string> GetUserModifierAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        return base.GetUserModifierAsync(purpose, manager, user);
    }
}
