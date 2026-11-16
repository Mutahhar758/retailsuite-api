using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Retailer.Shared.Common;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Multitenancy;
using Retailer.Infrastructure.Multitenancy;

namespace Retailer.Infrastructure.AI.Gemini;

public class TenantTokenUsageService : ITenantTokenUsageService, IScopedService
{
#if !ON_PREMISES
    private readonly TenantDbContext? _dbContext;
#endif
    private readonly ILogger<TenantTokenUsageService> _logger;

    // Process-level locks keyed by "{tenantId}:{model}" to prevent local thread race conditions
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public TenantTokenUsageService(IServiceProvider serviceProvider, ILogger<TenantTokenUsageService> logger)
    {
#if !ON_PREMISES
        _dbContext = serviceProvider.GetService<TenantDbContext>();
#endif
        _logger = logger;
    }

    public async Task RecordUsageAsync(
        string tenantId,
        string model,
        int inputTokens,
        int outputTokens,
        int totalTokens,
        CancellationToken cancellationToken = default)
    {
#if ON_PREMISES
        await Task.CompletedTask;
        return;
#else
        if (_dbContext is null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(tenantId)) tenantId = "GLOBAL";
        if (string.IsNullOrWhiteSpace(model)) model = "unknown";

        var cleanTenantId = tenantId.Trim();
        var cleanModel = model.Trim();
        var lockKey = $"{cleanTenantId.ToLowerInvariant()}:{cleanModel.ToLowerInvariant()}";

        var semaphore = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var now = PakistanTime.Now;
            var newId = Guid.NewGuid();

            // PostgreSQL atomic upsert with automatic row-level write lock.
            // Guarantees parallel calls (across processes, instances, or threads) do not overwrite or lose tokens.
            const string sql = @"
                INSERT INTO ""TenantAiTokenUsages"" (""id"", ""tenant_id"", ""model"", ""input_tokens"", ""output_tokens"", ""total_tokens"", ""call_count"", ""last_used_at"")
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, 1, {6})
                ON CONFLICT (""tenant_id"", ""model"")
                DO UPDATE SET
                    ""input_tokens"" = ""TenantAiTokenUsages"".""input_tokens"" + EXCLUDED.""input_tokens"",
                    ""output_tokens"" = ""TenantAiTokenUsages"".""output_tokens"" + EXCLUDED.""output_tokens"",
                    ""total_tokens"" = ""TenantAiTokenUsages"".""total_tokens"" + EXCLUDED.""total_tokens"",
                    ""call_count"" = ""TenantAiTokenUsages"".""call_count"" + 1,
                    ""last_used_at"" = EXCLUDED.""last_used_at"";";

            await _dbContext.Database.ExecuteSqlRawAsync(
                sql,
                new object[]
                {
                    newId,
                    cleanTenantId,
                    cleanModel,
                    (long)inputTokens,
                    (long)outputTokens,
                    (long)totalTokens,
                    now
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record AI token usage for Tenant: {TenantId}, Model: {Model}", cleanTenantId, cleanModel);
        }
        finally
        {
            semaphore.Release();
        }
#endif
    }

    public async Task<List<TenantAiTokenUsageDto>> GetTenantTokenUsagesAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
#if ON_PREMISES
        await Task.CompletedTask;
        return new List<TenantAiTokenUsageDto>();
#else
        if (_dbContext is null)
        {
            return new List<TenantAiTokenUsageDto>();
        }
        var cleanTenantId = (tenantId ?? "GLOBAL").Trim();
        return await _dbContext.TenantAiTokenUsages
            .AsNoTracking()
            .Where(x => x.TenantId == cleanTenantId)
            .OrderByDescending(x => x.LastUsedAt)
            .Select(x => new TenantAiTokenUsageDto
            {
                TenantId = x.TenantId,
                Model = x.Model,
                InputTokens = x.InputTokens,
                OutputTokens = x.OutputTokens,
                TotalTokens = x.TotalTokens,
                CallCount = x.CallCount,
                LastUsedAt = x.LastUsedAt
            })
            .ToListAsync(cancellationToken);
#endif
    }
}
