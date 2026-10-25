namespace Retailer.Domain.Multitenancy;

public class GeminiApiLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = default!;
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string Source { get; set; } = "AiAssistant";
    public int TurnIndex { get; set; }
    public string Model { get; set; } = default!;
    public string Endpoint { get; set; } = default!;
    public string? Prompt { get; set; }
    public string? CurrentRoute { get; set; }
    public string RequestBody { get; set; } = default!;
    public string? ResponseBody { get; set; }
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public long DurationMs { get; set; }
    public string? ToolCalls { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
