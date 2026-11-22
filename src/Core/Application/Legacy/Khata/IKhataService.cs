namespace Retailer.Application.Legacy.Khata;

public interface IKhataService : ITransientService
{
    Task<KhataSummaryResponse> GetPartiesAsync(string? partyType, string? search, CancellationToken cancellationToken);
    Task<KhataLedgerResponse> GetPartyLedgerAsync(string accountId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
}
