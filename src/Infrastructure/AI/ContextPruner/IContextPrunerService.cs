using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.AI.ContextPruner;

public interface IContextPrunerService : IScopedService
{
    List<AiChatMessage> PruneAndScrubHistory(List<AiChatMessage>? history, int maxTurns = 6);
}
