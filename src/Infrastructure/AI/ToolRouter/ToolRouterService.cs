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
        var lowerPrompt = (userPrompt ?? string.Empty).ToLowerInvariant().Trim();
        bool isExplicitReportRequest = lowerPrompt.Contains("report") ||
                                       lowerPrompt.Contains("statement") ||
                                       lowerPrompt.Contains("ledger") ||
                                       lowerPrompt.Contains("khata") ||
                                       lowerPrompt.Contains("khol") ||
                                       lowerPrompt.Contains("open") ||
                                       lowerPrompt.Contains("dikha") ||
                                       lowerPrompt.Contains("print") ||
                                       lowerPrompt.Contains("pdf");

        bool hasAccountIntent = lowerPrompt.Contains("balance") ||
                                lowerPrompt.Contains("account") ||
                                lowerPrompt.Contains("customer") ||
                                lowerPrompt.Contains("vendor") ||
                                lowerPrompt.Contains("supplier") ||
                                lowerPrompt.Contains("paise") ||
                                lowerPrompt.Contains("dena") ||
                                lowerPrompt.Contains("lena");

        bool hasItemStockIntent = lowerPrompt.Contains("stock") ||
                                  lowerPrompt.Contains("item") ||
                                  lowerPrompt.Contains("product") ||
                                  lowerPrompt.Contains("maal") ||
                                  lowerPrompt.Contains("kitna para") ||
                                  lowerPrompt.Contains("kitna bacha") ||
                                  lowerPrompt.Contains("carton") ||
                                  lowerPrompt.Contains("packet");

        // 1. Compute prompt embedding if not provided
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

        var matchedTasks = new List<AiTask>();

        if (promptEmbedding != null)
        {
            try
            {
                var candidateTasks = await _tenantDbContext.AiTasks
                    .AsNoTracking()
                    .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                        .ThenInclude(tt => tt.Tool)
                    .Where(t => t.IsActive && t.IntentEmbedding != null)
                    .OrderBy(t => t.IntentEmbedding!.CosineDistance(promptEmbedding))
                    .Take(5)
                    .ToListAsync(cancellationToken);

                if (candidateTasks.Count > 0)
                {
                    // Check if candidate is a report task but user didn't ask to open/view any report
                    AiTask primary = candidateTasks[0];

                    if (!isExplicitReportRequest && primary.Id.StartsWith("task_report_"))
                    {
                        // Swap with search inventory if item stock intent, or account search if balance intent
                        var alternative = candidateTasks.FirstOrDefault(t =>
                            hasItemStockIntent ? t.Id == "task_search_inventory" :
                            hasAccountIntent ? t.Id == "task_check_balance_search" :
                            !t.Id.StartsWith("task_report_"));

                        if (alternative != null)
                        {
                            primary = alternative;
                        }
                    }

                    matchedTasks.Add(primary);

                    // Multi-intent / Compound Query Support (e.g. Account balance + Item stock in one prompt)
                    if (hasAccountIntent && hasItemStockIntent)
                    {
                        var accountTask = candidateTasks.FirstOrDefault(t => t.Id == "task_check_balance_search")
                            ?? await _tenantDbContext.AiTasks
                                .AsNoTracking()
                                .Include(t => t.TaskTools.OrderBy(tt => tt.Order)).ThenInclude(tt => tt.Tool)
                                .FirstOrDefaultAsync(t => t.IsActive && t.Id == "task_check_balance_search", cancellationToken);

                        var itemTask = candidateTasks.FirstOrDefault(t => t.Id == "task_search_inventory")
                            ?? await _tenantDbContext.AiTasks
                                .AsNoTracking()
                                .Include(t => t.TaskTools.OrderBy(tt => tt.Order)).ThenInclude(tt => tt.Tool)
                                .FirstOrDefaultAsync(t => t.IsActive && t.Id == "task_search_inventory", cancellationToken);

                        if (accountTask != null && !matchedTasks.Any(t => t.Id == accountTask.Id)) matchedTasks.Add(accountTask);
                        if (itemTask != null && !matchedTasks.Any(t => t.Id == itemTask.Id)) matchedTasks.Add(itemTask);
                    }
                    else
                    {
                        // If multiple tasks have strong semantic affinity (e.g. top candidates with complementary tools),
                        // bundle tools from top matching non-conflicting tasks so Gemini has all necessary execution tools.
                        for (int i = 1; i < candidateTasks.Count && matchedTasks.Count < 2; i++)
                        {
                            var secondary = candidateTasks[i];
                            if (secondary.Id == primary.Id) continue;
                            if (!isExplicitReportRequest && secondary.Id.StartsWith("task_report_")) continue;

                            matchedTasks.Add(secondary);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "pgvector cosine search on AiTasks failed, falling back to heuristics.");
            }
        }

        // 2. UI Context Fallback: if no task matched via vector search, check current screen route
        if (matchedTasks.Count == 0 && !string.IsNullOrWhiteSpace(currentRoute))
        {
            var normalizedRoute = currentRoute.TrimEnd('/');
            var uiTask = await _tenantDbContext.AiTasks
                .AsNoTracking()
                .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                    .ThenInclude(tt => tt.Tool)
                .Where(t => t.IsActive && t.RouteHint != null && t.RouteHint.TrimEnd('/') == normalizedRoute)
                .FirstOrDefaultAsync(cancellationToken);

            if (uiTask != null)
            {
                matchedTasks.Add(uiTask);
            }
        }

        // 3. Keyword Heuristic Fallback
        if (matchedTasks.Count == 0 && !string.IsNullOrWhiteSpace(userPrompt))
        {
            var allTasks = await _tenantDbContext.AiTasks
                .AsNoTracking()
                .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                    .ThenInclude(tt => tt.Tool)
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            var keywordTask = allTasks.FirstOrDefault(t =>
                lowerPrompt.Contains(t.Id.Replace("task_", "").Replace("_", " "), StringComparison.OrdinalIgnoreCase) ||
                lowerPrompt.Contains(t.TaskName, StringComparison.OrdinalIgnoreCase));

            if (keywordTask != null)
            {
                matchedTasks.Add(keywordTask);
            }
        }

        // 4. Default Fallback Task (General Assistant)
        if (matchedTasks.Count == 0)
        {
            var defaultTask = await _tenantDbContext.AiTasks
                .AsNoTracking()
                .Include(t => t.TaskTools.OrderBy(tt => tt.Order))
                    .ThenInclude(tt => tt.Tool)
                .FirstOrDefaultAsync(t => t.IsActive && t.Id == "task_general_assistant", cancellationToken);

            if (defaultTask != null)
            {
                matchedTasks.Add(defaultTask);
            }
        }

        // Deduplicate tools across matched tasks, limit to 3 tools
        var toolsDict = new Dictionary<string, AiTool>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in matchedTasks)
        {
            foreach (var tt in task.TaskTools.Where(tt => tt.Tool != null && tt.Tool.IsActive))
            {
                if (!toolsDict.ContainsKey(tt.Tool.Id))
                {
                    toolsDict[tt.Tool.Id] = tt.Tool;
                }
            }
        }

        // Combine task instructions
        var combinedPrompt = string.Join("\n", matchedTasks
            .Select(t => t.TaskSpecificSystemPrompt)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct());

        var primaryTask = matchedTasks.FirstOrDefault();

        return new RoutedTaskResult
        {
            MatchedTask = primaryTask,
            Tools = toolsDict.Values.Take(5).ToList(),
            TaskSpecificSystemPrompt = combinedPrompt
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
