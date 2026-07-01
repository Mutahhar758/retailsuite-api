using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Mailing;
using Retailer.Application.Identity.Users;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Identity;
using Retailer.Shared.Authorization;
using Retailer.Shared.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace Retailer.Infrastructure.Identity;

internal partial class UserService
{
    /// <summary>
    /// This is used when authenticating with AzureAd.
    /// The local user is retrieved using the objectidentifier claim present in the ClaimsPrincipal.
    /// If no such claim is found, an InternalServerException is thrown.
    /// If no user is found with that ObjectId, a new one is created and populated with the values from the ClaimsPrincipal.
    /// If a role claim is present in the principal, and the user is not yet in that roll, then the user is added to that role.
    /// </summary>
    public async Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
    {
        string? objectId = principal.GetObjectId();
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new InternalServerException(_localizer["Invalid objectId"]);
        }

        var user = await _userManager.Users.Where(u => u.ObjectId == objectId).FirstOrDefaultAsync()
            ?? await CreateOrUpdateFromPrincipalAsync(principal);

        if (principal.FindFirstValue(ClaimTypes.Role) is string roleName &&
            await _roleManager.RoleExistsAsync(roleName))
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                bool isInRole = await _db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                if (!isInRole)
                {
                    await _db.UserRoles.AddAsync(new IdentityUserRole<string>
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    });
                    await _db.SaveChangesAsync();
                }
            }
        }

        return user.Id;
    }

    private async Task<ApplicationUser> CreateOrUpdateFromPrincipalAsync(ClaimsPrincipal principal)
    {
        string? email = principal.FindFirstValue(ClaimTypes.Upn);
        string? username = principal.GetDisplayName();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username))
        {
            throw new InternalServerException(string.Format(_localizer["Username or Email not valid."]));
        }

        var user = await _userManager.FindByNameAsync(username);
        if (user is not null && !string.IsNullOrWhiteSpace(user.ObjectId))
        {
            throw new InternalServerException(string.Format(_localizer["Username {0} is already taken."], username));
        }

        if (user is null)
        {
            user = await _userManager.FindByEmailAsync(email);
            if (user is not null && !string.IsNullOrWhiteSpace(user.ObjectId))
            {
                throw new InternalServerException(string.Format(_localizer["Email {0} is already taken."], email));
            }
        }

        IdentityResult? result;
        if (user is not null)
        {
            user.ObjectId = principal.GetObjectId();
            result = await _userManager.UpdateAsync(user);
        }
        else
        {
            user = new ApplicationUser
            {
                ObjectId = principal.GetObjectId(),
                FirstName = principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = principal.FindFirstValue(ClaimTypes.Surname),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                UserName = username,
                NormalizedUserName = username.ToUpperInvariant(),
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Status = UserStatus.Active
            };
            result = await _userManager.CreateAsync(user);
        }

        if (!result.Succeeded)
        {
            throw new InternalServerException(_localizer[MessageConstants.ValidationErrorsOccurred], result.GetErrors(_localizer));
        }

        return user;
    }

    public async Task<string> CreateAsync(CreateUserRequest request, string origin)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = string.IsNullOrWhiteSpace(request.UserName) ? request.Email : request.UserName,
            PhoneNumber = request.PhoneNumber,
            Status = UserStatus.Unconfirmed
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new BadRequestException(_localizer[MessageConstants.ValidationErrorsOccurred] + string.Join('\n', result.GetErrors(_localizer)));
        }

        var basicRole = await _roleManager.FindByNameAsync(AppRoles.Basic);
        if (basicRole != null)
        {
            await _db.UserRoles.AddAsync(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = basicRole.Id
            });
            await _db.SaveChangesAsync();
        }

        var messages = new List<string> { _localizer[MessageConstants.UserRegistered, user.Email] };

        string otp = string.Empty;

        if (_securitySettings.RequireConfirmedAccount && !string.IsNullOrEmpty(user.Email))
        {
            otp = await this._userManager.GenerateEmailConfirmationTokenAsync(user);

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

            // To-do: Mailing
            messages.Add(_localizer[MessageConstants.VerifyUserEmail, user.Email]);
        }

        return string.Join(Environment.NewLine, messages);
    }

    public async Task UpdateAsync(UpdateUserRequest request, string? userId = null)
    {
        userId ??= _currentUser.GetUserId();

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        _ = user ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        string currentImage = user.ImageUrl ?? string.Empty;
        if (request.Image != null || request.DeleteCurrentImage)
        {
            user.ImageUrl = await _fileStorage.UploadAsync<ApplicationUser>(request.Image, FileType.Image);
            if (request.DeleteCurrentImage && !string.IsNullOrEmpty(currentImage))
            {
                string root = Directory.GetCurrentDirectory();
                _fileStorage.Remove(Path.Combine(root, currentImage));
            }
        }

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.UserName = request.UserName ?? user.UserName;
        user.Email = request.Email ?? user.Email;

        string? phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (request.PhoneNumber != phoneNumber && request.PhoneNumber != null)
        {
            await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
        }

        var result = await _userManager.UpdateAsync(user);

        await _signInManager.RefreshSignInAsync(user);

        if (!result.Succeeded)
        {
            throw new InternalServerException(_localizer[MessageConstants.UpdateProfileFailed], result.GetErrors(_localizer));
        }
    }

    public async Task ToggleBiometricsAsync(SetBiometricsRequest request)
    {
        string currentUserId = _currentUser.GetUserId().ToString();
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == currentUserId) ?? throw new NotFoundException(_localizer["User not found"]);

        user.BiometricPublicKey = request.PublicKey;

        await _userManager.UpdateAsync(user);
    }
}
