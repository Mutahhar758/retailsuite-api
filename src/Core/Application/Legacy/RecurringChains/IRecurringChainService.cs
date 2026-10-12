using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.RecurringChains;

public interface IRecurringChainService : ITransientService
{
    Task<List<RecurringChainResponse>> GetListAsync(CancellationToken cancellationToken);
    Task<RecurringChainResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> CreateFromVoucherAsync(RecurringChainCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(int id, RecurringChainUpdateRequest request, CancellationToken cancellationToken);
    Task ToggleActiveAsync(int id, bool isActive, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<string?> RunChainForTodayAsync(int id, DateOnly? targetDate, CancellationToken cancellationToken);
    Task ProcessTenantDueChainsAsync(DateOnly runDate, CancellationToken cancellationToken);
}
