using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Models;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.AI;

[Authorize]
[Route("api/v1/ai/assistant")]
public class AiAssistantController : VersionNeutralApiController
{
    private readonly IAiAssistantService _aiAssistantService;

    public AiAssistantController(IAiAssistantService aiAssistantService)
    {
        _aiAssistantService = aiAssistantService;
    }

    [HttpPost("chat")]
    [OpenApiOperation("Process user voice or text prompt via the Agentic AI Assistant.", "")]
    public async Task<HttpResponseDto<AiAssistantResponse>> ChatAsync([FromBody] AiAssistantRequest request, CancellationToken cancellationToken)
    {
        var response = await _aiAssistantService.ProcessPromptAsync(request, cancellationToken);
        return response.ToInformationResponse();
    }
}
