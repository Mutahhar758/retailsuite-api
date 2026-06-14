using Retailer.Application.Legacy.HumanResources;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class HRInfoController : VersionNeutralApiController
{
    private readonly IHRInfoService _hrInfoService;

    public HRInfoController(IHRInfoService hrInfoService)
    {
        _hrInfoService = hrInfoService;
    }

    [HttpGet]
    [OpenApiOperation("Get all HR information records.", "")]
    public async Task<HttpResponseDto<List<HRInfoResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var employees = await _hrInfoService.GetAsync(cancellationToken);
        return employees.ToInformationResponse();
    }

    [HttpGet("{id}")]
    [OpenApiOperation("Get HR information by ID.", "")]
    public async Task<HttpResponseDto<HRInfoResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var employee = await _hrInfoService.GetByIdAsync(id, cancellationToken);
        if (employee == null)
            throw new Exception($"HR Info with ID {id} not found.");

        return employee.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create HR information.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        await _hrInfoService.CreateAsync(request, cancellationToken);
        return "HR Info created successfully.".ToInformationResponse("HR Info created successfully.");
    }

    [HttpPut("{id}")]
    [OpenApiOperation("Update HR information.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string id, HRInfoUpsertRequest request, CancellationToken cancellationToken)
    {
        await _hrInfoService.UpdateAsync(id, request, cancellationToken);
        return "HR Info updated successfully.".ToInformationResponse("HR Info updated successfully.");
    }

    [HttpDelete("{id}")]
    [OpenApiOperation("Delete HR information.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _hrInfoService.DeleteAsync(id, cancellationToken);
        return "HR Info deleted successfully.".ToInformationResponse("HR Info deleted successfully.");
    }
}
