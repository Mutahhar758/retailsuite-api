using Retailer.Application.Multitenancy;
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
    private readonly ITenantService _tenantService;

    public LicenseController(ITenantService tenantService) =>
        _tenantService = tenantService;

    [HttpGet("verify/{licenseKey}")]
    [AllowAnonymous]
    [OpenApiOperation("Verify license key and return tenant id.", "")]
    public async Task<IActionResult> VerifyAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var tenantId = await _tenantService.GetTenantIdByLicenseKeyAsync(licenseKey, cancellationToken);
        return Ok(tenantId.ToInformationResponse());
    }
}
