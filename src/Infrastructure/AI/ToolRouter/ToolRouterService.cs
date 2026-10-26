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

    public async Task<RoutedTaskResult> RouteTaskAsync(
        string? currentRoute,
        Vector? promptEmbedding,
        string? userPrompt,
        CancellationToken cancellationToken = default)
    {
        // 1. Vector Search for the TASK, not individual tools
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

        AiTask? matchedTask = null;

        if (promptEmbedding != null)
        {
            try
            {
                matchedTask = await _tenantDbContext.AiTasks
                    .AsNoTracking()
                    .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                        .ThenInclude(tt => tt.Tool)
                    .Where(t => t.IsActive && t.IntentEmbedding != null)
                    .OrderBy(t => t.IntentEmbedding!.CosineDistance(promptEmbedding))
                    .FirstOrDefaultAsync(cancellationToken);

                if (matchedTask != null)
                {
                    _logger.LogInformation("Task vector search matched task: {TaskId} ({TaskName})", matchedTask.Id, matchedTask.TaskName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "pgvector cosine search on AiTasks failed, falling back to heuristics.");
            }
        }

        // 2. UI Context Fallback: if no task matched via vector search, check current screen route
        if (matchedTask == null && !string.IsNullOrWhiteSpace(currentRoute))
        {
            var normalizedRoute = currentRoute.TrimEnd('/');
            matchedTask = await _tenantDbContext.AiTasks
                .AsNoTracking()
                .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                    .ThenInclude(tt => tt.Tool)
                .Where(t => t.IsActive && t.RouteHint != null && t.RouteHint.TrimEnd('/') == normalizedRoute)
                .FirstOrDefaultAsync(cancellationToken);

            if (matchedTask != null)
            {
                _logger.LogInformation("Task UI route matched task: {TaskId} ({TaskName})", matchedTask.Id, matchedTask.TaskName);
            }
        }

        // 3. Keyword Heuristic Fallback
        if (matchedTask == null && !string.IsNullOrWhiteSpace(userPrompt))
        {
            var lower = userPrompt.ToLowerInvariant();
            var allTasks = await _tenantDbContext.AiTasks
                .AsNoTracking()
                .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                    .ThenInclude(tt => tt.Tool)
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            matchedTask = allTasks.FirstOrDefault(t =>
                lower.Contains(t.Id.Replace("task_", "").Replace("_", " "), StringComparison.OrdinalIgnoreCase) ||
                lower.Contains(t.TaskName, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => w.Length > 3 && lower.Contains(w, StringComparison.OrdinalIgnoreCase)));
        }

        // 4. Default Fallback Task (General Assistant)
        if (matchedTask == null)
        {
            matchedTask = await _tenantDbContext.AiTasks
                .AsNoTracking()
                .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                    .ThenInclude(tt => tt.Tool)
                .FirstOrDefaultAsync(t => t.IsActive && t.Id == "task_general_assistant", cancellationToken);
        }

        var tools = matchedTask?.TaskTools
            .Where(tt => tt.Tool != null && tt.Tool.IsActive)
            .Select(tt => tt.Tool)
            .ToList() ?? new List<AiTool>();

        return new RoutedTaskResult
        {
            MatchedTask = matchedTask,
            Tools = tools,
            TaskSpecificSystemPrompt = matchedTask?.TaskSpecificSystemPrompt ?? string.Empty
        };
    }

    public async Task<List<AiTool>> GetTargetedToolsAsync(
        string? currentRoute,
        Vector? promptEmbedding,
        string? userPrompt,
        CancellationToken cancellationToken = default)
    {
        var result = await RouteTaskAsync(currentRoute, promptEmbedding, userPrompt, cancellationToken);
        return result.Tools;
    }
}
