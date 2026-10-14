using Retailer.Application.Legacy.Manufacturing;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class BomsController : VersionNeutralApiController
{
    private readonly IBomService _bomService;

    public BomsController(IBomService bomService)
    {
        _bomService = bomService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.BillOfMaterials)]
    [OpenApiOperation("Get bill of materials list.", "")]
    public async Task<HttpResponseDto<List<BomResponse>>> GetListAsync(
        [FromQuery] BomListFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _bomService.GetListAsync(filter, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id:int}")]
    [MustHavePermission(AppAction.View, AppResource.BillOfMaterials)]
    [OpenApiOperation("Get bill of materials by ID.", "")]
    public async Task<HttpResponseDto<BomResponse>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _bomService.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("default/{finishedItemId}")]
    [MustHavePermission(AppAction.View, AppResource.BillOfMaterials)]
    [OpenApiOperation("Get default bill of materials for a finished product.", "")]
    public async Task<HttpResponseDto<BomResponse?>> GetDefaultByFinishedItemIdAsync(
        string finishedItemId,
        CancellationToken cancellationToken)
    {
        var result = await _bomService.GetDefaultByFinishedItemIdAsync(finishedItemId, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.BillOfMaterials)]
    [OpenApiOperation("Create a new bill of materials.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(
        BomCreateRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _bomService.CreateAsync(request, cancellationToken);
        return id.ToString().ToInformationResponse("Bill of materials created.");
    }

    [HttpPut("{id:int}")]
    [MustHavePermission(AppAction.Update, AppResource.BillOfMaterials)]
    [OpenApiOperation("Update an existing bill of materials.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(
        int id,
        BomUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _bomService.UpdateAsync(id, request, cancellationToken);
        return "Bill of materials updated.".ToInformationResponse("Bill of materials updated.");
    }

    [HttpDelete("{id:int}")]
    [MustHavePermission(AppAction.Delete, AppResource.BillOfMaterials)]
    [OpenApiOperation("Delete a bill of materials.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        await _bomService.DeleteAsync(id, cancellationToken);
        return "Bill of materials deleted.".ToInformationResponse("Bill of materials deleted.");
    }
}
