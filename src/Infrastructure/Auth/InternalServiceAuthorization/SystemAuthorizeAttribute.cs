using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Retailer.Application.Common.Exceptions;
using Retailer.Shared.Authorization;
using Retailer.Shared.Common.Constants;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Retailer.Infrastructure.Auth.InternalServiceAuthorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SystemAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IOptions<InternalServicesKeySettings>>().Value;

        StringValues apiKey;
        context.HttpContext.Request.Headers.TryGetValue(ApiKeyConstants.ApiKeyHeaderName, out apiKey);

        if (apiKey.FirstOrDefault() != configuration.ApiKey)
        {
            throw new ForbiddenException("You are not authorized to access this resource.");
        }
    }
}
