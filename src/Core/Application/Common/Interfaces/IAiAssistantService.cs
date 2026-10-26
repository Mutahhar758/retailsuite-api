namespace Retailer.Application.Common.Interfaces;

public enum AiActionType
{
    Answer = 0,
    Navigate = 1,
    DraftVoucher = 2
}

public class AiChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = default!;
}

public class AiAssistantRequest
{
    public string Prompt { get; set; } = default!;
    public string? ChatId { get; set; }
    public string? CurrentRoute { get; set; }
    public Dictionary<string, string>? ActiveFilters { get; set; }
    public List<AiChatMessage>? History { get; set; }
}

public class AiAssistantResponse
{
    public string ReplyText { get; set; } = default!;
    public string? ChatId { get; set; }
    public AiActionType ActionType { get; set; } = AiActionType.Answer;
    public string? TargetRoute { get; set; }
    public object? ActionPayload { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public interface IAiAssistantService : IScopedService
{
    Task<AiAssistantResponse> ProcessPromptAsync(AiAssistantRequest request, CancellationToken cancellationToken = default);
}
