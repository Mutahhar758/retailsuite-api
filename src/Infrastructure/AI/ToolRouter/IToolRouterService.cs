using Pgvector;
using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Multitenancy;

namespace Retailer.Infrastructure.AI.ToolRouter;

public class RoutedTaskResult
{
    public AiTask? MatchedTask { get; set; }
    public List<AiTool> Tools { get; set; } = new();
    public string TaskSpecificSystemPrompt { get; set; } = string.Empty;
}

public interface IToolRouterService : IScopedService
{
    Task<RoutedTaskResult> RouteTaskAsync(
        string? currentRoute,
        Vector? promptEmbedding,
        string? userPrompt,
        CancellationToken cancellationToken = default);

    Task<List<AiTool>> GetTargetedToolsAsync(
        string? currentRoute,
        Vector? promptEmbedding,
        string? userPrompt,
        CancellationToken cancellationToken = default);
}
