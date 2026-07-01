using System.Text;
using Retailer.Application.Common.Exceptions;
using Retailer.Infrastructure.Common;
using Retailer.Domain.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Identity.Tokens;
using Retailer.Application.Common.Mailing;
using System.Threading;
using Retailer.Application.Identity.Users;
using Retailer.Shared.Localization;
using Retailer.Domain.Common.Enums;

namespace Retailer.Infrastructure.Identity;

internal partial class UserService
{
    private async Task<string> GetEmailVerificationUriAsync(ApplicationUser user, string origin)
    {
        string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        const string route = "api/users/confirm-email/";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));
        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), QueryStringKeys.UserId, user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, QueryStringKeys.Code, code);
        return verificationUri;
    }

    public async Task<string> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == userId && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new InternalServerException(_localizer[MessageConstants.ErrorOccurredWhileConfirming, _localizer[EntityConstants.Email]]);

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, code);

        return result.Succeeded
            ? string.Format(_localizer[MessageConstants.EmailAccountConfirmed], user.Email)
            : throw new InternalServerException(_localizer[MessageConstants.ErrorOccurredWhileConfirming, _localizer[EntityConstants.Email]]);
    }

    public async Task<string> ConfirmPhoneNumberAsync(string userId, string code)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        _ = user ?? throw new InternalServerException(_localizer[MessageConstants.ErrorOccurredWhileConfirming, _localizer[EntityConstants.Phone]]);
        if (string.IsNullOrEmpty(user.PhoneNumber)) throw new InternalServerException(_localizer[MessageConstants.ErrorOccurredWhileConfirming, _localizer[EntityConstants.Phone]]);

        var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, code);

        return result.Succeeded
            ? user.EmailConfirmed
                ? _localizer[MessageConstants.PhoneAccountConfirmed, user.PhoneNumber]
                : _localizer[MessageConstants.PhoneAccountConfirmedButNotEmail, user.PhoneNumber]
            : throw new InternalServerException(_localizer[MessageConstants.ErrorOccurredWhileConfirming, _localizer[EntityConstants.Phone]]);
    }

    public async Task<TokenResponseDto> VerifyOTPAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .Where(u => u.NormalizedEmail == request.Email.ToUpper() && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        var result = await _userManager.ConfirmEmailAsync(user, request.Otp);

        if(!result.Succeeded)
            throw new InternalServerException(_localizer[MessageConstants.EmailVerificationFailed], result.GetErrors(_localizer));

        user.Status = UserStatus.Active;

        var response = await this._tokenService.GenerateTokensAndUpdateUserAsync(user, request.DeviceId, request.FcmToken, request.AppVersion, request.DeviceName);

        return response;
    }

    public async Task<string> RequestOTPAsync(string email)
    {
        var user = await _userManager.Users
            .Where(u => u.NormalizedEmail == email.ToUpper() && !u.EmailConfirmed)
            .FirstOrDefaultAsync() ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        string otp = await this._userManager.GenerateEmailConfirmationTokenAsync(user);

        var emailModel = new RegisterUserOtpModel
        {
            UserName = user.FirstName,
            Otp = otp,
        };

        var mailRequest = new MailRequest(
            new List<string> { user.Email },
            _localizer["Confirm Registration"],
            _templateService.GenerateEmailTemplate("otp-verification", emailModel),
            isHtml: true);

        await _mailService.SendAsync(mailRequest, CancellationToken.None);

        return _localizer[MessageConstants.OTPSentToEmail, user.Email];
    }
}
