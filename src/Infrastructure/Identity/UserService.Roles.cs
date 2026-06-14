using Retailer.Application.Common.Exceptions;
using Retailer.Application.Identity.Users;
using Retailer.Domain.Identity;
using Retailer.Shared.Authorization;
using Retailer.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<List<UserRoleDto>> GetRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var userRoles = new List<UserRoleDto>();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        if (roles is null) throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.Role]]);
        foreach (var role in roles)
        {
            userRoles.Add(new UserRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                Enabled = await _userManager.IsInRoleAsync(user, role.Name!)
            });
        }

        return userRoles;
    }

    public async Task<string> AssignRolesAsync(string userId, UserRolesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var user = await _userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        // Check if the user is an admin for which the admin role is getting disabled
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin)
            && request.UserRoles.Any(a => !a.Enabled && a.RoleName == AppRoles.Admin))
        {
            // Get count of users in Admin Role
            int adminCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count;

            if (adminCount <= 1)
            {
                throw new ConflictException(_localizer[MessageConstants.AppMinimunAdmins]);
            }
        }

        foreach (var userRole in request.UserRoles)
        {
            // Check if Role Exists
            if (await _roleManager.FindByNameAsync(userRole.RoleName!) is not null)
            {
                if (userRole.Enabled)
                {
                    if (!await _userManager.IsInRoleAsync(user, userRole.RoleName!))
                    {
                        await _userManager.AddToRoleAsync(user, userRole.RoleName!);
                    }
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, userRole.RoleName!);
                }
            }
        }

        return _localizer[MessageConstants.RecordUpdated, _localizer[EntityConstants.UserRoles]];
    }
}