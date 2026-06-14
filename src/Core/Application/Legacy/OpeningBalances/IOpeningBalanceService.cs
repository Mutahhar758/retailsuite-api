namespace Retailer.Application.Legacy.OpeningBalances;

public interface IOpeningBalanceService : ITransientService
{
    Task<List<OpeningBalanceResponse>> GetAsync(string? parentAccountId, CancellationToken cancellationToken);
    Task UpsertAsync(OpeningBalanceUpsertRequest request, CancellationToken cancellationToken);
}
