using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Retailer.Infrastructure.OnPremises;
using Retailer.Infrastructure.Tunnel;
using Retailer.Shared.Common;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal class TenantMiddleware : IMiddleware
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;
    private readonly OnPremisesLicenseGuard? _licenseGuard;

    public TenantMiddleware(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        IServiceProvider serviceProvider)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
        _licenseGuard = serviceProvider.GetService<OnPremisesLicenseGuard>();
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Skip tenant validation for health check, swagger, license, and tunnel hub endpoints
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/openapi", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/license", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/local-media", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(TunnelConstants.HubPath, StringComparison.OrdinalIgnoreCase))
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

        if (_licenseGuard is not null)
        {
            var (allowed, reason) = _licenseGuard.EvaluateRequest(context.Request.Method);
            if (!allowed)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { Message = reason });
                return;
            }
        }

        if (!tenantInfo.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Message = $"Tenant '{tenantInfo.Identifier}' is inactive." });
            return;
        }

        if (tenantInfo.ValidFrom > PakistanTime.Now)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Message = $"Tenant '{tenantInfo.Identifier}' subscription has not started yet." });
            return;
        }

        if (tenantInfo.ValidUntil.HasValue && tenantInfo.ValidUntil < PakistanTime.Now)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Message = $"Tenant '{tenantInfo.Identifier}' subscription has expired." });
            return;
        }

        await next(context);
    }
}
