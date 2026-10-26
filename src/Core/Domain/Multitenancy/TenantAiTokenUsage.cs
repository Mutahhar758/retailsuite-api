namespace Retailer.Domain.Multitenancy;

public class TenantAiTokenUsage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = default!;
    public string Model { get; set; } = default!;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens { get; set; }
    public int CallCount { get; set; }
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}
