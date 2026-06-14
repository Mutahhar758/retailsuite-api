namespace Retailer.Application.Legacy.Units;

public interface IUnitService : ITransientService
{
    Task<List<UnitLookupResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task CreateAsync(UnitCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string code, UnitUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string code, CancellationToken cancellationToken);
}
