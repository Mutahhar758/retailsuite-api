using System.Security.Claims;
using Retailer.Application.Identity.Users;
using Microsoft.AspNetCore.Authorization;

namespace Retailer.Infrastructure.Auth.Permissions;

internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserService _userService;

    public PermissionAuthorizationHandler(IUserService userService) =>
        _userService = userService;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.GetUserId() is { } userId)
        {
            if (context.User.IsOwner())
            {
                context.Succeed(requirement);
                return;
            }

            var permissions = requirement.Permission.Split("||");
            foreach (var permission in permissions)
            {
                if (await _userService.HasPermissionAsync(userId, permission))
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}