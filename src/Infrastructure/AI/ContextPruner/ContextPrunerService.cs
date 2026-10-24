using System.Text.RegularExpressions;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.AI.ContextPruner;

public class ContextPrunerService : IContextPrunerService
{
    private static readonly Regex JsonCodeBlockRegex = new(@"```(?:json)?\s*[\{\[][\s\S]*?[\}\]]\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public List<AiChatMessage> PruneAndScrubHistory(List<AiChatMessage>? history, int maxTurns = 6)
    {
        if (history == null || history.Count == 0)
        {
            return new List<AiChatMessage>();
        }

        // 1. Sliding Window: take the last maxTurns
        var pruned = history
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(maxTurns)
            .ToList();

        // 2. JSON Scrubbing: remove raw database JSON blobs from older turns
        var result = new List<AiChatMessage>(pruned.Count);
        for (int i = 0; i < pruned.Count; i++)
        {
            var msg = pruned[i];

            // Don't scrub the very latest message if it's the current turn
            if (i < pruned.Count - 1 && msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                var scrubbedContent = JsonCodeBlockRegex.Replace(msg.Content, string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(scrubbedContent))
                {
                    scrubbedContent = msg.Content.Length > 200 ? msg.Content[..200] + "..." : msg.Content;
                }

                result.Add(new AiChatMessage { Role = msg.Role, Content = scrubbedContent });
            }
            else
            {
                result.Add(new AiChatMessage { Role = msg.Role, Content = msg.Content });
            }
        }

        return result;
    }
}
