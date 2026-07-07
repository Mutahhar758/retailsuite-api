using Retailer.Application.Identity.Roles;
using Retailer.Infrastructure.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Retailer.Host.Controllers.Identity;

public class RolesController : VersionNeutralApiController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("list")]
    [OpenApiOperation("Get list of all roles.", "")]
    public async Task<HttpResponseDto<List<RoleDto>>> GetListAsync(CancellationToken cancellationToken)
    {
        return (await _roleService.GetListAsync(cancellationToken)).ToInformationResponse();
    }

    [HttpGet("{id}")]
    [OpenApiOperation("Get role details by id.", "")]
    public async Task<HttpResponseDto<RoleDto>> GetByIdAsync(string id)
    {
        return (await _roleService.GetByIdAsync(id)).ToInformationResponse();
    }

    [HttpGet("{id}/permissions")]
    [OpenApiOperation("Get role with permissions.", "")]
    public async Task<HttpResponseDto<RoleDto>> GetByIdWithPermissionsAsync(string id, CancellationToken cancellationToken)
    {
        return (await _roleService.GetByIdWithPermissionsAsync(id, cancellationToken)).ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create or update a role.", "")]
    public async Task<HttpResponseDto<string>> CreateOrUpdateAsync(CreateOrUpdateRoleRequest request)
    {
        string msg = await _roleService.CreateOrUpdateAsync(request);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpPut("permissions")]
    [OpenApiOperation("Update role permissions.", "")]
    public async Task<HttpResponseDto<string>> UpdatePermissionsAsync(UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        string msg = await _roleService.UpdatePermissionsAsync(request, cancellationToken);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpDelete("{id}")]
    [OpenApiOperation("Delete a role.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string id)
    {
        string msg = await _roleService.DeleteAsync(id);
        return HttpResponseExtension.InformationResponse(msg);
    }

    [HttpGet("permissions")]
    [OpenApiOperation("Get all system permissions.", "")]
    public async Task<HttpResponseDto<List<PermissionDto>>> GetAllPermissionsAsync(CancellationToken cancellationToken)
    {
        return (await _roleService.GetAllPermissionsAsync(cancellationToken)).ToInformationResponse();
    }
}
