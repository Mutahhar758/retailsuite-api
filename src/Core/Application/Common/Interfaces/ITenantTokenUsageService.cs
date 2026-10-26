namespace Retailer.Application.Common.Interfaces;

public class TenantAiTokenUsageDto
{
    public string TenantId { get; set; } = default!;
    public string Model { get; set; } = default!;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens { get; set; }
    public int CallCount { get; set; }
    public DateTime LastUsedAt { get; set; }
}

public interface ITenantTokenUsageService : IScopedService
{
    Task RecordUsageAsync(
        string tenantId,
        string model,
        int inputTokens,
        int outputTokens,
        int totalTokens,
        CancellationToken cancellationToken = default);

    Task<List<TenantAiTokenUsageDto>> GetTenantTokenUsagesAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}
