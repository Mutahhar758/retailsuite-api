using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.MobileShop;

public interface IRepairJobService : ITransientService
{
    Task<List<RepairJobResponse>> GetListAsync(RepairJobListFilter filter, CancellationToken cancellationToken);
    Task<RepairJobResponse?> GetByIdAsync(string jobNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(RepairJobCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string jobNo, RepairJobUpdateRequest request, CancellationToken cancellationToken);
    Task UpdateStatusAsync(string jobNo, RepairJobStatusUpdateRequest request, CancellationToken cancellationToken);
    Task<string> BillJobAsync(string jobNo, RepairJobBillRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string jobNo, CancellationToken cancellationToken);
}
