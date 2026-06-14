using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal class TenantMiddleware : IMiddleware
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public TenantMiddleware(IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor) =>
        _multiTenantContextAccessor = multiTenantContextAccessor;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Skip tenant validation for health check and swagger endpoints
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/openapi", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/license", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var tenantInfo = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo;

        if (tenantInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { Message = "Tenant not found. Please provide a valid X-Tenant-ID header." });
            return;
        }

        if (!tenantInfo.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Message = $"Tenant '{tenantInfo.Identifier}' is inactive." });
            return;
        }

        if (tenantInfo.ValidFrom > DateTime.UtcNow)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Message = $"Tenant '{tenantInfo.Identifier}' subscription has not started yet." });
            return;
        }

        if (tenantInfo.ValidUntil.HasValue && tenantInfo.ValidUntil < DateTime.UtcNow)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Message = $"Tenant '{tenantInfo.Identifier}' subscription has expired." });
            return;
        }

        await next(context);
    }
}
