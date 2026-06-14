using Microsoft.AspNetCore.Http;

namespace Retailer.Infrastructure.Auth;

public class CurrentUserMiddleware : IMiddleware
{
    private readonly ICurrentUserInitializer _currentUserInitializer;

    public CurrentUserMiddleware(ICurrentUserInitializer currentUserInitializer) =>
        _currentUserInitializer = currentUserInitializer;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _currentUserInitializer.SetCurrentUser(context.User, context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());

        await next(context);
    }
}