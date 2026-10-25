using System.Text.Json;
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

        // 2. Condense previous turns: strip bloated json blocks but preserve short tool execution info
        var result = new List<AiChatMessage>(pruned.Count);
        for (int i = 0; i < pruned.Count; i++)
        {
            var msg = pruned[i];

            if (i < pruned.Count - 1)
            {
                var content = msg.Content;

                // Scrub large markdown JSON code blocks
                content = JsonCodeBlockRegex.Replace(content, string.Empty);

                var trimmed = content.Trim();
                // If content is a raw JSON payload, summarize key properties compactly instead of wiping it
                if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
                {
                    trimmed = SummarizeJsonShort(trimmed);
                }

                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    result.Add(new AiChatMessage { Role = msg.Role, Content = trimmed });
                }
            }
            else
            {
                result.Add(new AiChatMessage { Role = msg.Role, Content = msg.Content });
            }
        }

        return result;
    }

    private static string SummarizeJsonShort(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                var pairs = new List<string>();
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                    {
                        var val = prop.Value.ToString();
                        if (val.Length <= 40)
                        {
                            pairs.Add($"{prop.Name}={val}");
                        }
                    }
                    if (pairs.Count >= 4) break;
                }

                if (pairs.Count > 0)
                {
                    return $"[Tool Result: {string.Join(", ", pairs)}]";
                }
            }
        }
        catch { }

        return "[Tool Executed]";
    }
}
