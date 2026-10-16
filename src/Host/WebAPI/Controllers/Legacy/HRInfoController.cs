using Retailer.Application.Legacy.HumanResources;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class HRInfoController : VersionNeutralApiController
{
    private readonly IHRInfoService _hrInfoService;

    public HRInfoController(IHRInfoService hrInfoService)
    {
        _hrInfoService = hrInfoService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.HRInfo)]
    [OpenApiOperation("Get all employee records.", "")]
    public async Task<HttpResponseDto<List<HRInfoResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var employees = await _hrInfoService.GetAsync(cancellationToken);
        return employees.ToInformationResponse();
    }

    [HttpGet("{id}")]
    [MustHavePermission(AppAction.View, AppResource.HRInfo)]
    [OpenApiOperation("Get employee by ID.", "")]
    public async Task<HttpResponseDto<HRInfoResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoService.GetByIdAsync(id, cancellationToken);
        if (employee == null)
            throw new Exception($"Employee with ID {id} not found.");

        return employee.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.HRInfo)]
    [OpenApiOperation("Create employee.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        await _hrInfoService.CreateAsync(request, cancellationToken);
        return "Employee created successfully.".ToInformationResponse("Employee created successfully.");
    }

    [HttpPut("{id}")]
    [MustHavePermission(AppAction.Update, AppResource.HRInfo)]
    [OpenApiOperation("Update employee.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string id, HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        await _hrInfoService.UpdateAsync(id, request, cancellationToken);
        return "Employee updated successfully.".ToInformationResponse("Employee updated successfully.");
    }

    [HttpDelete("{id}")]
    [MustHavePermission(AppAction.Delete, AppResource.HRInfo)]
    [OpenApiOperation("Delete employee.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _hrInfoService.DeleteAsync(id, cancellationToken);
        return "Employee deleted successfully.".ToInformationResponse("Employee deleted successfully.");
    }

    [HttpPost("presigned-upload-url")]
    [MustHavePermission(new[] { AppAction.Create, AppAction.Update }, AppResource.HRInfo)]
    [OpenApiOperation("Generate pre-signed upload URL for employee photo.", "")]
    public async Task<HttpResponseDto<PresignedUploadUrlResponse?>> GetPresignedUploadUrlAsync([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        var response = await _hrInfoService.GetPresignedUploadUrlAsync(fileName, cancellationToken);
        return response.ToInformationResponse();
    }

    [HttpPost("bulk")]
    [MustHavePermission(new[] { AppAction.Create, AppAction.Update }, AppResource.HRInfo)]
    [OpenApiOperation("Bulk create and update employees.", "")]
    public async Task<HttpResponseDto<BulkOperationResult>> BulkUpsertAsync(HRInfoBulkUpsertRequest request, CancellationToken cancellationToken)
    {
        var response = await _hrInfoService.BulkUpsertAsync(request, cancellationToken);
        return response.ToInformationResponse($"Bulk operation completed: {response.CreatedCount} created, {response.UpdatedCount} updated.");
    }
}
