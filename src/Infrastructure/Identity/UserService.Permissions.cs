using Retailer.Application.Common.Caching;
using Retailer.Application.Common.Exceptions;
using Retailer.Shared.Authorization;
using Retailer.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<List<string>> GetPermissionsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        userId ??= _currentUser.GetUserId();

        var user = await _userManager.FindByIdAsync(userId);

        _ = user ?? throw new UnauthorizedException(MessageConstants.AuthFailed);

        var userRoles = await _userManager.GetRolesAsync(user);
        var permissions = new List<string>();
        foreach (var role in await _roleManager.Roles
            .Where(r => userRoles.Contains(r.Name!))
            .ToListAsync(cancellationToken))
        {
            permissions.AddRange(await _db.RoleClaims
                .Where(rc => rc.RoleId == role.Id && rc.ClaimType == AppClaims.Permission)
                .Select(rc => rc.ClaimValue!)
                .ToListAsync(cancellationToken));
        }

        return permissions.Distinct().ToList();
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken)
    {
        var permissions = await _cache.GetOrSetAsync(
            _cacheKeys.GetCacheKey(AppClaims.Permission, userId),
            () => GetPermissionsAsync(userId, cancellationToken),
            cancellationToken: cancellationToken);

        return permissions?.Contains(permission) ?? false;
    }

    public Task InvalidatePermissionCacheAsync(string userId, CancellationToken cancellationToken) =>
        _cache.RemoveAsync(_cacheKeys.GetCacheKey(AppClaims.Permission, userId), cancellationToken);
}