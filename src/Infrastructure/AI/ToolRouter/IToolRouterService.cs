using Pgvector;
using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Multitenancy;

namespace Retailer.Infrastructure.AI.ToolRouter;

public interface IToolRouterService : IScopedService
{
    Task<List<AiTool>> GetTargetedToolsAsync(string? currentRoute, Vector? promptEmbedding, string? userPrompt, CancellationToken cancellationToken = default);
}
