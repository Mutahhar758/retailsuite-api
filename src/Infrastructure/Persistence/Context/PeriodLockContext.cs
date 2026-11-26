using System.Collections.Concurrent;

namespace Retailer.Infrastructure.Persistence.Context;

/// <summary>
/// AsyncLocal scope that temporarily allows posting or reversing closing (VType = "CL")
/// GL entries on a closed period date strictly during an authorized Period Close or Reopen operation.
/// </summary>
public static class PeriodClosingScope
{
    private static readonly AsyncLocal<int> _depth = new();

    public static bool IsActive => _depth.Value > 0;

    public static IDisposable Enter()
    {
        _depth.Value++;
        return new ScopeGuard();
    }

    private sealed class ScopeGuard : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _depth.Value = Math.Max(0, _depth.Value - 1);
        }
    }
}

/// <summary>
/// Lightweight per-tenant cache of the active accounting period lock date (MAX ClosingDate where Status = "Closed").
/// Avoids circular DI between BaseDbContext and PeriodClosing services and eliminates redundant DB lookups.
/// </summary>
public static class PeriodLockCache
{
    private static readonly ConcurrentDictionary<string, DateOnly?> _lockDates = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string tenantId, out DateOnly? lockDate) =>
        _lockDates.TryGetValue(tenantId, out lockDate);

    public static void Set(string tenantId, DateOnly? lockDate) =>
        _lockDates[tenantId] = lockDate;

    public static void Invalidate(string tenantId) =>
        _lockDates.TryRemove(tenantId, out _);

    public static void ClearAll() =>
        _lockDates.Clear();
}
