using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Identity.Roles;
using Retailer.Domain.Identity;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Shared.Authorization;
using Retailer.Domain.Identity;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Retailer.Shared.Localization;

namespace Retailer.Infrastructure.Identity;

internal class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer _localizer;
    private readonly ICurrentUser _currentUser;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IStringLocalizer<RoleService> localizer,
        ICurrentUser currentUser)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
        _localizer = localizer;
        _currentUser = currentUser;
    }

    public async Task<List<RoleDto>> GetListAsync(CancellationToken cancellationToken) =>
        (await _roleManager.Roles.ToListAsync(cancellationToken))
            .Adapt<List<RoleDto>>();

    public async Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        await _roleManager.Roles.CountAsync(cancellationToken);

    public async Task<bool> ExistsAsync(string roleName, string? excludeId) =>
        await _roleManager.FindByNameAsync(roleName)
            is ApplicationRole existingRole
            && existingRole.Id != excludeId;

    public async Task<RoleDto> GetByIdAsync(string id) =>
        await _db.Roles.SingleOrDefaultAsync(x => x.Id == id) is { } role
            ? role.Adapt<RoleDto>()
            : throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.Role]]);

    public async Task<RoleDto> GetByIdWithPermissionsAsync(string roleId, CancellationToken cancellationToken)
    {
        var role = await GetByIdAsync(roleId);

        role.Permissions = await _db.RoleClaims
            .Where(c => c.RoleId == roleId && c.ClaimType == AppClaims.Permission)
            .Select(c => c.ClaimValue!)
            .ToListAsync(cancellationToken);

        return role;
    }

    public async Task<string> CreateOrUpdateAsync(CreateOrUpdateRoleRequest request)
    {
        if (string.IsNullOrEmpty(request.Id))
        {
            // Create a new role.
            var role = new ApplicationRole(request.Name, request.Description);
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                throw new InternalServerException(_localizer[MessageConstants.RegisterRoleFailed], result.GetErrors(_localizer));
            }

            return _localizer[MessageConstants.RecordAdded, _localizer[EntityConstants.Role]];
        }
        else
        {
            // Update an existing role.
            var role = await _roleManager.FindByIdAsync(request.Id);

            _ = role ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.Role]]);

            if (AppRoles.IsDefault(role.Name!))
            {
                throw new ConflictException(string.Format(_localizer[MessageConstants.RoleModificationNotAllowed], role.Name));
            }

            role.Name = request.Name;
            role.NormalizedName = request.Name.ToUpperInvariant();
            role.Description = request.Description;
            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                throw new InternalServerException(_localizer[MessageConstants.UpdateRoleFailed], result.GetErrors(_localizer));
            }

            return _localizer[MessageConstants.RecordUpdated, _localizer[EntityConstants.Role]];
        }
    }

    public async Task<string> UpdatePermissionsAsync(UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(request.RoleId);
        _ = role ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.Role]]);
        if (role.Name == AppRoles.Admin)
        {
            throw new ConflictException(_localizer[MessageConstants.PermissionsModificationNotAllowed]);
        }

        var currentClaims = await _roleManager.GetClaimsAsync(role);

        // Remove permissions that were previously selected
        foreach (var claim in currentClaims.Where(c => !request.Permissions.Any(p => p == c.Value)))
        {
            var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeResult.Succeeded)
            {
                throw new InternalServerException(_localizer[MessageConstants.UpdatePermissionsFailed], removeResult.GetErrors(_localizer));
            }
        }

        // Add all permissions that were not previously selected
        foreach (string permission in request.Permissions.Where(c => !currentClaims.Any(p => p.Value == c)))
        {
            if (!string.IsNullOrEmpty(permission))
            {
                _db.RoleClaims.Add(new ApplicationRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = AppClaims.Permission,
                    ClaimValue = permission,
                    CreatedBy = _currentUser.GetUserId()
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return _localizer[MessageConstants.RecordUpdated, _localizer[EntityConstants.Permissions]];
    }

    public async Task<string> DeleteAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);

        _ = role ?? throw new NotFoundException(_localizer[MessageConstants.RecordNotFound, _localizer[EntityConstants.Role]]);

        if (AppRoles.IsDefault(role.Name!))
        {
            throw new ConflictException(string.Format(_localizer[MessageConstants.RoleDeletionNotAllowed], role.Name));
        }

        if ((await _userManager.GetUsersInRoleAsync(role.Name!)).Count > 0)
        {
            throw new ConflictException(string.Format(_localizer[MessageConstants.RoleDeletionNotAllowedInUse], role.Name));
        }

        await _roleManager.DeleteAsync(role);

        return _localizer[MessageConstants.RecordDeleted, _localizer[EntityConstants.Role]];
    }

    public Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken)
    {
        var allPermissions = AppPermissions.Admin.Select(p => new PermissionDto
        {
            Name = p.Name,
            Description = p.Description,
            Action = p.Action,
            Resource = p.Resource,
            IsBasic = p.IsBasic
        }).ToList();

        return Task.FromResult(allPermissions);
    }
}