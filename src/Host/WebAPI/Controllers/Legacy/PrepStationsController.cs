using Retailer.Application.Legacy.Kots;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Retailer.Host.Controllers.Legacy;

public class PrepStationsController : VersionNeutralApiController
{
    private readonly IPrepStationService _prepStationService;

    public PrepStationsController(IPrepStationService prepStationService)
    {
        _prepStationService = prepStationService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.PrepStations)]
    [OpenApiOperation("Get prep stations.", "")]
    public async Task<HttpResponseDto<List<PrepStationDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var result = await _prepStationService.GetPrepStationsAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.PrepStations)]
    [OpenApiOperation("Create prep station.", "")]
    public async Task<HttpResponseDto<PrepStationDto>> CreateAsync(PrepStationCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _prepStationService.CreatePrepStationAsync(request, cancellationToken);
        return result.ToInformationResponse("Prep station created.");
    }

    [HttpPut("{id}")]
    [MustHavePermission(AppAction.Update, AppResource.PrepStations)]
    [OpenApiOperation("Update prep station.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string id, PrepStationUpdateRequest request, CancellationToken cancellationToken)
    {
        await _prepStationService.UpdatePrepStationAsync(id, request, cancellationToken);
        return "Prep station updated.".ToInformationResponse("Prep station updated.");
    }

    [HttpDelete("{id}")]
    [MustHavePermission(AppAction.Delete, AppResource.PrepStations)]
    [OpenApiOperation("Delete prep station.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _prepStationService.DeletePrepStationAsync(id, cancellationToken);
        return "Prep station deleted.".ToInformationResponse("Prep station deleted.");
    }
}
