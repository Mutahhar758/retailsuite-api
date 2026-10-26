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
    private readonly ITenantTokenUsageService _tokenUsageService;
    private readonly ICurrentTenant _currentTenant;

    public AiAssistantController(
        IAiAssistantService aiAssistantService,
        ITenantTokenUsageService tokenUsageService,
        ICurrentTenant currentTenant)
    {
        _aiAssistantService = aiAssistantService;
        _tokenUsageService = tokenUsageService;
        _currentTenant = currentTenant;
    }

    [HttpPost("chat")]
    [OpenApiOperation("Process user voice or text prompt via the Agentic AI Assistant.", "")]
    public async Task<HttpResponseDto<AiAssistantResponse>> ChatAsync([FromBody] AiAssistantRequest request, CancellationToken cancellationToken)
    {
        var response = await _aiAssistantService.ProcessPromptAsync(request, cancellationToken);
        return response.ToInformationResponse();
    }

    [HttpGet("token-usage")]
    [OpenApiOperation("Get cumulative AI consumed token usage per model for the current tenant.", "")]
    public async Task<HttpResponseDto<List<TenantAiTokenUsageDto>>> GetTokenUsageAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant?.Id ?? "GLOBAL";
        var usages = await _tokenUsageService.GetTenantTokenUsagesAsync(tenantId, cancellationToken);
        return usages.ToInformationResponse();
    }
}
