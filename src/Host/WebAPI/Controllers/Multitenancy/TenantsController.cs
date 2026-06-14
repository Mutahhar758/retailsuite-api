using Retailer.Application.Multitenancy;
using Retailer.Infrastructure.Auth.InternalServiceAuthorization;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Multitenancy;

[Route("api/tenants")]
[ApiVersionNeutral]
public class TenantsController : BaseApiController
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService) =>
        _tenantService = tenantService;

    [HttpGet]
    [OpenApiOperation("Get all tenants.", "Requires SuperAdmin role.")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(cancellationToken);
        return Ok(tenants.ToInformationResponse());
    }

    [HttpGet("{id}")]
    [OpenApiOperation("Get tenant by id.", "Requires SuperAdmin role.")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        return Ok(tenant.ToInformationResponse());
    }

    [HttpPost]
    [OpenApiOperation("Create a new tenant and provision its database.", "Requires SuperAdmin role.")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var response = await _tenantService.CreateAsync(request, cancellationToken);
        return Ok(response.ToSuccessResponse($"Tenant created with id '{response.Id}'. License Key: {response.LicenseKey}"));
    }

    [HttpPut("{id}")]
    [OpenApiOperation("Update an existing tenant.", "Requires SuperAdmin role.")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateAsync(string id, UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        request.Id = id;
        await _tenantService.UpdateAsync(request, cancellationToken);
        return Ok(HttpResponseExtension.InformationResponse("Tenant updated successfully."));
    }

    [HttpPost("{id}/activate")]
    [OpenApiOperation("Activate a tenant.", "Requires Admin role.")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ActivateAsync(string id, CancellationToken cancellationToken)
    {
        await _tenantService.ActivateAsync(id, cancellationToken);
        return Ok(HttpResponseExtension.InformationResponse("Tenant activated successfully."));
    }

    [HttpPost("{id}/deactivate")]
    [OpenApiOperation("Deactivate a tenant.", "Requires Admin role.")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeactivateAsync(string id, CancellationToken cancellationToken)
    {
        await _tenantService.DeactivateAsync(id, cancellationToken);
        return Ok(HttpResponseExtension.InformationResponse("Tenant deactivated successfully."));
    }
}
