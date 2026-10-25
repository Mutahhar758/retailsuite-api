using System.Text.RegularExpressions;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.AI.ContextPruner;

public class ContextPrunerService : IContextPrunerService
{
    private static readonly Regex JsonCodeBlockRegex = new(@"```(?:json)?\s*[\{\[][\s\S]*?[\}\]]\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FunctionCallBlobsRegex = new(@"\""(?:functionCall|functionResponse)\""\s*:\s*\{[\s\S]*?\}\s*\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        // 2. JSON Scrubbing: remove raw database JSON blobs, functionCall dumps, and JSON code blocks from older turns
        var result = new List<AiChatMessage>(pruned.Count);
        for (int i = 0; i < pruned.Count; i++)
        {
            var msg = pruned[i];

            // Scrub completed previous turns to retain only natural language conversation
            if (i < pruned.Count - 1)
            {
                var content = msg.Content;

                // Scrub markdown JSON code blocks: ```json { ... } ```
                content = JsonCodeBlockRegex.Replace(content, string.Empty);

                // Scrub functionCall / functionResponse JSON blocks
                content = FunctionCallBlobsRegex.Replace(content, string.Empty);

                var trimmed = content.Trim();
                // If content is a raw standalone JSON object or array
                if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
                {
                    trimmed = "Action processed.";
                }

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    trimmed = "Action processed.";
                }

                result.Add(new AiChatMessage { Role = msg.Role, Content = trimmed });
            }
            else
            {
                result.Add(new AiChatMessage { Role = msg.Role, Content = msg.Content });
            }
        }

        return result;
    }
}
