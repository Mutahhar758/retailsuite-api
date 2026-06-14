namespace Retailer.Application.Legacy.HumanResources;

public interface IHRInfoService : ITransientService
{
    Task<List<HRInfoResponse>> GetAsync(CancellationToken cancellationToken);
    Task<HRInfoResponse?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task CreateAsync(HRInfoUpsertRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string id, HRInfoUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
