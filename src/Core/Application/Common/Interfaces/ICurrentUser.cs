using System.Security.Claims;

namespace Retailer.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? Name { get; }
    string? Username { get; }

    string GetUserId();

    string? GetUserEmail();

    bool IsAuthenticated();

    bool IsInRole(string role);

    string? GetToken();

    IEnumerable<Claim>? GetUserClaims();
}