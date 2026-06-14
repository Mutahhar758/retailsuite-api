using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

/// <summary>
/// Enriches Serilog log context with the current tenant identifier on each request.
/// </summary>
internal class TenantLogEnricherMiddleware : IMiddleware
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _multiTenantContextAccessor;

    public TenantLogEnricherMiddleware(IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor) =>
        _multiTenantContextAccessor = multiTenantContextAccessor;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantId = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        var tenantIdentifier = _multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Identifier;

        using (LogContext.PushProperty("TenantId", tenantId ?? "unknown"))
        using (LogContext.PushProperty("TenantIdentifier", tenantIdentifier ?? "unknown"))
        {
            await next(context);
        }
    }
}
