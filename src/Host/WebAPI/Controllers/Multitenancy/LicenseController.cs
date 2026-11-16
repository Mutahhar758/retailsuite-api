using Retailer.Application.Multitenancy;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Retailer.Host.Controllers.Multitenancy;

[Route("api/license")]
[ApiVersionNeutral]
public class LicenseController : BaseApiController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentTenant _currentTenant;

    public LicenseController(IServiceProvider serviceProvider, ICurrentTenant currentTenant)
    {
        _serviceProvider = serviceProvider;
        _currentTenant = currentTenant;
    }

    [HttpGet("verify/{licenseKey}")]
    [AllowAnonymous]
    [OpenApiOperation("Verify license key and return tenant id.", "")]
    public async Task<IActionResult> VerifyAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var tenantService = _serviceProvider.GetService<ITenantService>();
        if (tenantService is null)
        {
            return Ok(_currentTenant.Id.ToInformationResponse());
        }

        var tenantId = await tenantService.GetTenantIdByLicenseKeyAsync(licenseKey, cancellationToken);
        return Ok(tenantId.ToInformationResponse());
    }

    [HttpGet("features")]
    [AllowAnonymous]
    [OpenApiOperation("Get current tenant features.", "")]
    public IActionResult GetFeatures([FromServices] ICurrentTenant currentTenant)
    {
        return Ok(new
        {
            Name = currentTenant.Name,
            HasSupplyFeature = currentTenant.HasSupplyFeature,
            HasSecondaryQty = currentTenant.HasSecondaryQty,
            HasKotFeature = currentTenant.HasKotFeature,
            HasVariablePackFeature = currentTenant.HasVariablePackFeature,
            HasMobileShopFeature = currentTenant.HasMobileShopFeature
        }.ToInformationResponse());
    }
}
