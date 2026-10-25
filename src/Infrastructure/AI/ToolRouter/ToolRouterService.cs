using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Retailer.Domain.Multitenancy;
using Retailer.Infrastructure.AI.Gemini;
using Retailer.Infrastructure.Multitenancy;

namespace Retailer.Infrastructure.AI.ToolRouter;

public class ToolRouterService : IToolRouterService
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly IGeminiEmbeddingService? _embeddingService;
    private readonly ILogger<ToolRouterService> _logger;

    public ToolRouterService(
        TenantDbContext tenantDbContext,
        ILogger<ToolRouterService> logger,
        IGeminiEmbeddingService? embeddingService = null)
    {
        _tenantDbContext = tenantDbContext;
        _logger = logger;
        _embeddingService = embeddingService;
    }

    public async Task<List<AiTool>> GetTargetedToolsAsync(
        string? currentRoute,
        Vector? promptEmbedding,
        string? userPrompt,
        CancellationToken cancellationToken = default)
    {
        var resultTools = new Dictionary<string, AiTool>(StringComparer.OrdinalIgnoreCase);

        // 1. Semantic Intent Anchor: pgvector Cosine Search
        // If embedding wasn't provided directly, compute it from userPrompt
        if (promptEmbedding == null && !string.IsNullOrWhiteSpace(userPrompt) && _embeddingService != null)
        {
            try
            {
                promptEmbedding = await _embeddingService.GenerateEmbeddingAsync(userPrompt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compute embedding for user prompt: {Prompt}", userPrompt);
            }
        }

        if (promptEmbedding != null)
        {
            try
            {
                var vectorTools = await _tenantDbContext.AiTools
                    .AsNoTracking()
                    .Where(t => t.IsActive && t.Embedding != null)
                    .OrderBy(t => t.Embedding!.CosineDistance(promptEmbedding))
                    .Take(2)
                    .ToListAsync(cancellationToken);

                foreach (var tool in vectorTools)
                {
                    resultTools[tool.Id] = tool;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "pgvector cosine search failed, falling back to heuristics.");
            }
        }

        // 2. UI Context Anchor: fetch tools mapped to current screen if slots available
        if (resultTools.Count < 2 && !string.IsNullOrWhiteSpace(currentRoute))
        {
            var normalizedRoute = currentRoute.TrimEnd('/');
            var uiTools = await _tenantDbContext.AiTools
                .AsNoTracking()
                .Where(t => t.IsActive && t.RouteHint != null && t.RouteHint.TrimEnd('/') == normalizedRoute)
                .Take(2 - resultTools.Count)
                .ToListAsync(cancellationToken);

            foreach (var tool in uiTools)
            {
                resultTools[tool.Id] = tool;
            }
        }

        // 3. Fallback Keyword Heuristic if less than 2 tools were found
        if (resultTools.Count < 2 && !string.IsNullOrWhiteSpace(userPrompt))
        {
            var lower = userPrompt.ToLowerInvariant();
            var keywords = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var fallbackTools = await _tenantDbContext.AiTools
                .AsNoTracking()
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var tool in fallbackTools)
            {
                if (resultTools.ContainsKey(tool.Id)) continue;

                if (keywords.Any(k => tool.ToolName.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                                      tool.Description.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                                      tool.Category.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    resultTools[tool.Id] = tool;
                    if (resultTools.Count >= 2) break;
                }
            }
        }

        // 4. Default baseline: if still empty, provide general navigational & search tools
        if (resultTools.Count == 0)
        {
            var defaultTools = await _tenantDbContext.AiTools
                .AsNoTracking()
                .Where(t => t.IsActive && (t.Id == "navigate_to_report" || t.Id == "search_account"))
                .Take(2)
                .ToListAsync(cancellationToken);

            foreach (var tool in defaultTools)
            {
                resultTools[tool.Id] = tool;
            }
        }

        // 5. Dependent Helper Tool: If navigate_to_report or draft voucher is selected, ensure search_account or search_vendor is available
        if (resultTools.ContainsKey("navigate_to_report") && !resultTools.ContainsKey("search_account") && resultTools.Count < 3)
        {
            var searchTool = await _tenantDbContext.AiTools
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.Id == "search_account", cancellationToken);
            if (searchTool != null)
            {
                resultTools[searchTool.Id] = searchTool;
            }
        }
        else if (resultTools.ContainsKey("draft_payment_voucher") && !resultTools.ContainsKey("search_vendor") && resultTools.Count < 3)
        {
            var vendorTool = await _tenantDbContext.AiTools
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.Id == "search_vendor", cancellationToken);
            if (vendorTool != null)
            {
                resultTools[vendorTool.Id] = vendorTool;
            }
        }

        return resultTools.Values.Take(3).ToList();
    }
}
