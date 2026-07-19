using Retailer.Application.Legacy.Kots;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Retailer.Host.Controllers.Legacy;

public class DiningTablesController : VersionNeutralApiController
{
    private readonly IDiningTableService _diningTableService;

    public DiningTablesController(IDiningTableService diningTableService)
    {
        _diningTableService = diningTableService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.DiningTables)]
    [OpenApiOperation("Get dining tables.", "")]
    public async Task<HttpResponseDto<List<DiningTableDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var result = await _diningTableService.GetDiningTablesAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.DiningTables)]
    [OpenApiOperation("Create dining table.", "")]
    public async Task<HttpResponseDto<DiningTableDto>> CreateAsync(DiningTableCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _diningTableService.CreateDiningTableAsync(request, cancellationToken);
        return result.ToInformationResponse("Dining table created.");
    }

    [HttpPut("{id}")]
    [MustHavePermission(AppAction.Update, AppResource.DiningTables)]
    [OpenApiOperation("Update dining table.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(int id, DiningTableUpdateRequest request, CancellationToken cancellationToken)
    {
        await _diningTableService.UpdateDiningTableAsync(id, request, cancellationToken);
        return "Dining table updated.".ToInformationResponse("Dining table updated.");
    }

    [HttpDelete("{id}")]
    [MustHavePermission(AppAction.Delete, AppResource.DiningTables)]
    [OpenApiOperation("Delete dining table.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _diningTableService.DeleteDiningTableAsync(id, cancellationToken);
        return "Dining table deleted.".ToInformationResponse("Dining table deleted.");
    }
}
