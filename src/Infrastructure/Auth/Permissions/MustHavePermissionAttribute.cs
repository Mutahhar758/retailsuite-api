using Retailer.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Retailer.Infrastructure.Auth.Permissions;

public class MustHavePermissionAttribute : AuthorizeAttribute
{
    public MustHavePermissionAttribute(string action, string resource) =>
        Policy = AppPermission.NameFor(action, resource);

    public MustHavePermissionAttribute(string[] actions, string resource) =>
        Policy = string.Join("||", actions.Select(a => AppPermission.NameFor(a, resource)));
}