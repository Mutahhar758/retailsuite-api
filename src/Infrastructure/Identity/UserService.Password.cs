using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Mailing;
using Retailer.Application.Identity.Users.Password;
using Retailer.Shared.Localization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, string origin)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Normalize());
        if (user is null)
        {
            // Don't reveal that the user does not exist
            return _localizer["Password Reset Mail has been sent to your authorized Email."];
        }

        // For more information on how to enable account confirmation and password reset please
        // visit https://go.microsoft.com/fwlink/?LinkID=532713
        string code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var mailRequest = new MailRequest(
            new List<string> { request.Email },
            _localizer["Reset Password"],
            _localizer[$"Your Password Reset Token is '{code}'."]);

        await _mailService.SendAsync(mailRequest, CancellationToken.None);

        return _localizer[MessageConstants.PasswordResetMailSent];
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email?.Normalize()!);

        // Don't reveal that the user does not exist'\
        _ = user ?? throw new InternalServerException(_localizer[MessageConstants.ErrorOccurred]);

        var result = await _userManager.ResetPasswordAsync(user, request.Token!, request.Password!);

        return result.Succeeded
            ? _localizer[MessageConstants.PasswordResetSuccessful]
            : throw new BadRequestException(_localizer[string.Join(",", result.GetErrors(_localizer))]);
    }

    public async Task VerifyPasswordOtpAsync(VerifyPasswordOtpRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException(_localizer["User not found"]);
        var result = await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", request.Otp);
        if (!result) throw new BadRequestException(_localizer["Invalid OTP"]);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest model)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.GetUserId().ToString());

        _ = user ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        var result = await _userManager.ChangePasswordAsync(user, currentPassword: model.OldPassword, newPassword: model.NewPassword);

        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(", ", result.GetErrors(_localizer)));
        }

        if(model.LogOutOfAllAccounts)
        {
            await _sessionService.LogOutAllSessionsExceptCurrentUserAsync(user.Id);
        }
    }
}