using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Tokens.Identity;
using Retailer.Shared.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace Retailer.Infrastructure.Middleware;

public class UserSessionMiddleware : IMiddleware
{
    private readonly ISessionService _sessionService;
    private readonly IStringLocalizer<UserSessionMiddleware> _localizer;

    public UserSessionMiddleware(ISessionService sessionService, IStringLocalizer<UserSessionMiddleware> localizer)
    {
        _sessionService = sessionService;
        _localizer = localizer;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Check if the endpoint allows anonymous access
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            // If anonymous access is allowed, bypass token validation
            await next(context);
            return;
        }

        string? token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            bool verified = await _sessionService.VerifyTokenSessionAsync(token!);

            if (!verified)
                throw new UnauthorizedException(_localizer[MessageConstants.InvalidToken]);
        }

        await next(context);
    }
}
