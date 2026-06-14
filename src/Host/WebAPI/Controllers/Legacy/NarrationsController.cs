using Retailer.Application.Legacy.Narrations;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class NarrationsController : VersionNeutralApiController
{
    private readonly INarrationService _narrationService;

    public NarrationsController(INarrationService narrationService)
    {
        _narrationService = narrationService;
    }

    [HttpGet]
    [OpenApiOperation("Get active narrations.", "")]
    public async Task<HttpResponseDto<List<NarrationResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var narrations = await _narrationService.GetActiveAsync(cancellationToken);
        return narrations.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create a narration.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(NarrationCreateRequest request, CancellationToken cancellationToken)
    {
        await _narrationService.CreateAsync(request, cancellationToken);
        return "Narration created.".ToInformationResponse("Narration created.");
    }

    [HttpPut("{code}")]
    [OpenApiOperation("Update a narration.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string code, NarrationUpdateRequest request, CancellationToken cancellationToken)
    {
        await _narrationService.UpdateAsync(code, request, cancellationToken);
        return "Narration updated.".ToInformationResponse("Narration updated.");
    }

    [HttpDelete("{code}")]
    [OpenApiOperation("Delete a narration.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string code, CancellationToken cancellationToken)
    {
        await _narrationService.DeleteAsync(code, cancellationToken);
        return "Narration deleted.".ToInformationResponse("Narration deleted.");
    }
}
