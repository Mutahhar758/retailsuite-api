using Pgvector;

namespace Retailer.Domain.Multitenancy;

public class AiTool
{
    public string Id { get; set; } = default!;
    public string ToolName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string JsonSchema { get; set; } = default!;
    public string? RouteHint { get; set; }
    public string? RequiredPermission { get; set; }
    public Vector? Embedding { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AiTaskTool> TaskTools { get; set; } = new List<AiTaskTool>();
}
