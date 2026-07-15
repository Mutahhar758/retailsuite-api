namespace Retailer.Application.Legacy.Narrations;

public interface INarrationService : ITransientService
{
    Task<List<NarrationResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task<List<NarrationLookupResponse>> GetLookupAsync(CancellationToken cancellationToken);
    Task CreateAsync(NarrationCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string code, NarrationUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string code, CancellationToken cancellationToken);
}
