using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Retailer.Domain.Multitenancy;
using Retailer.Infrastructure.Multitenancy;

namespace Retailer.Infrastructure.AI.ToolRouter;

public class ToolRouterService : IToolRouterService
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly ILogger<ToolRouterService> _logger;

    public ToolRouterService(TenantDbContext tenantDbContext, ILogger<ToolRouterService> logger)
    {
        _tenantDbContext = tenantDbContext;
        _logger = logger;
    }

    public async Task<List<AiTool>> GetTargetedToolsAsync(
        string? currentRoute,
        Vector? promptEmbedding,
        string? userPrompt,
        CancellationToken cancellationToken = default)
    {
        var resultTools = new Dictionary<string, AiTool>(StringComparer.OrdinalIgnoreCase);

        // 1. UI Context Anchor (O(1)): fetch 1-2 tools mapped to current screen
        if (!string.IsNullOrWhiteSpace(currentRoute))
        {
            var normalizedRoute = currentRoute.TrimEnd('/');
            var uiTools = await _tenantDbContext.AiTools
                .AsNoTracking()
                .Where(t => t.IsActive && t.RouteHint != null && t.RouteHint.TrimEnd('/') == normalizedRoute)
                .Take(2)
                .ToListAsync(cancellationToken);

            foreach (var tool in uiTools)
            {
                resultTools[tool.Id] = tool;
            }
        }

        // 2. Semantic Intent Anchor: pgvector Cosine Search
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
                _logger.LogWarning(ex, "pgvector cosine search failed, falling back to keyword heuristics.");
            }
        }

        // 3. Fallback Heuristic if less than 2 tools were found
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
                    if (resultTools.Count >= 4) break;
                }
            }
        }

        // 4. Default baseline: if still empty, provide general navigational & search tools
        if (resultTools.Count == 0)
        {
            var defaultTools = await _tenantDbContext.AiTools
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Take(3)
                .ToListAsync(cancellationToken);

            foreach (var tool in defaultTools)
            {
                resultTools[tool.Id] = tool;
            }
        }

        return resultTools.Values.ToList();
    }
}
