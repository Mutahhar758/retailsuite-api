namespace Retailer.Domain.Multitenancy;

public class AiTaskTool
{
    public string TaskId { get; set; } = default!;
    public AiTask Task { get; set; } = default!;

    public string ToolId { get; set; } = default!;
    public AiTool Tool { get; set; } = default!;

    public int Order { get; set; } = 0;
}
