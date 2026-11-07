using Pgvector;
using Retailer.Shared.Common;

namespace Retailer.Domain.Multitenancy;

public class AiTask
{
    public string Id { get; set; } = default!;
    public string TaskName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? RouteHint { get; set; }
    public Vector? IntentEmbedding { get; set; }
    public string TaskSpecificSystemPrompt { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = PakistanTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AiTaskTool> TaskTools { get; set; } = new List<AiTaskTool>();
}
