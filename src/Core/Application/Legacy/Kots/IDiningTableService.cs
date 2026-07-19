using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Kots;

public interface IDiningTableService : ITransientService
{
    Task<List<DiningTableDto>> GetDiningTablesAsync(CancellationToken cancellationToken);
    Task<DiningTableDto> CreateDiningTableAsync(DiningTableCreateRequest request, CancellationToken cancellationToken);
    Task UpdateDiningTableAsync(int id, DiningTableUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteDiningTableAsync(int id, CancellationToken cancellationToken);
}
