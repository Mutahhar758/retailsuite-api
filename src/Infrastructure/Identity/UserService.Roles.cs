using Retailer.Application.Common.Exceptions;
using Retailer.Application.Identity.Users;
using Retailer.Domain.Identity;
using Retailer.Shared.Authorization;
using Retailer.Shared.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Retailer.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<List<UserRoleDto>> GetRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var userRoles = new List<UserRoleDto>();

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        if (roles is null) throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.Role]]);

        var userRoleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            userRoles.Add(new UserRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                Enabled = userRoleIds.Contains(role.Id)
            });
        }

        return userRoles;
    }

    public async Task<string> AssignRolesAsync(string userId, UserRolesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var user = await _userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.User]]);

        var adminRole = await _roleManager.FindByNameAsync(AppRoles.Admin);
        bool userWasAdmin = adminRole != null && await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRole.Id, cancellationToken);

        // Check if the user is an admin for which the admin role is getting disabled
        if (userWasAdmin && request.UserRoles.Any(a => !a.Enabled && a.RoleName == AppRoles.Admin))
        {
            // Get count of users in Admin Role
            int adminCount = adminRole != null ? await _db.UserRoles.CountAsync(ur => ur.RoleId == adminRole.Id, cancellationToken) : 0;

            if (adminCount <= 1)
            {
                throw new ConflictException(_localizer[MessageConstants.AppMinimunAdmins]);
            }
        }

        foreach (var userRole in request.UserRoles)
        {
            // Check if Role Exists
            var role = await _roleManager.FindByNameAsync(userRole.RoleName!);
            if (role is not null)
            {
                var existingUserRole = await _db.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id, cancellationToken);

                if (userRole.Enabled)
                {
                    if (existingUserRole == null)
                    {
                        await _db.UserRoles.AddAsync(new IdentityUserRole<string>
                        {
                            UserId = userId,
                            RoleId = role.Id
                        }, cancellationToken);
                    }
                }
                else
                {
                    if (existingUserRole != null)
                    {
                        _db.UserRoles.Remove(existingUserRole);
                    }
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return _localizer[MessageConstants.RecordUpdated, _localizer[EntityConstants.UserRoles]];
    }
}