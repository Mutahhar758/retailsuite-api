using Retailer.Application.Legacy.Units;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class UnitsController : VersionNeutralApiController
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    [OpenApiOperation("Get active units.", "")]
    public async Task<HttpResponseDto<List<UnitLookupResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var units = await _unitService.GetActiveAsync(cancellationToken);
        return units.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create unit.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(UnitCreateRequest request, CancellationToken cancellationToken)
    {
        await _unitService.CreateAsync(request, cancellationToken);
        return "Unit created.".ToInformationResponse("Unit created.");
    }

    [HttpPut("{code}")]
    [OpenApiOperation("Update unit.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string code, UnitUpdateRequest request, CancellationToken cancellationToken)
    {
        await _unitService.UpdateAsync(code, request, cancellationToken);
        return "Unit updated.".ToInformationResponse("Unit updated.");
    }

    [HttpDelete("{code}")]
    [OpenApiOperation("Delete unit.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string code, CancellationToken cancellationToken)
    {
        await _unitService.DeleteAsync(code, cancellationToken);
        return "Unit deleted.".ToInformationResponse("Unit deleted.");
    }
}
