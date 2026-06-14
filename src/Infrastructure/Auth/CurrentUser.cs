using System.Runtime.CompilerServices;
using System.Security.Claims;
using Retailer.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Retailer.Infrastructure.Auth;

public class CurrentUser : ICurrentUser, ICurrentUserInitializer
{
    private ClaimsPrincipal? _user;
    private string? _token;

    public string? Name => IsAuthenticated()
        ? _user?.GetFullName() ?? _user?.Identity?.Name
        : null;

    public string? Username => IsAuthenticated()
        ? _user?.GetUserName()
        : null;

    private string _userId = string.Empty;

    public string GetUserId()
    {
        return IsAuthenticated()
            ? _user?.GetUserId() ?? string.Empty
            : _userId;
    }

    public string? GetUserEmail() =>
        IsAuthenticated()
            ? _user!.GetEmail()
            : string.Empty;

    public bool IsAuthenticated() =>
        _user?.Identity?.IsAuthenticated is true;

    public bool IsInRole(string role) =>
        _user?.IsInRole(role) is true;

    public IEnumerable<Claim>? GetUserClaims() =>
        _user?.Claims;

    public string? GetToken() => _token;

    public void SetCurrentUser(ClaimsPrincipal user, string? token = null)
    {
        if (_user != null)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        _user = user;
        _token = token;
    }

    public void SetCurrentUserId(string userId)
    {
        if (_userId != string.Empty)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        if (!string.IsNullOrEmpty(userId))
        {
            _userId = userId;
        }
    }
}