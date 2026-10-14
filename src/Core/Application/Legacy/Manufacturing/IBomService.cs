using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Manufacturing;

public interface IBomService : ITransientService
{
    Task<List<BomResponse>> GetListAsync(BomListFilter filter, CancellationToken cancellationToken);
    Task<BomResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<BomResponse?> GetDefaultByFinishedItemIdAsync(string finishedItemId, CancellationToken cancellationToken);
    Task<int> CreateAsync(BomCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(int id, BomUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
