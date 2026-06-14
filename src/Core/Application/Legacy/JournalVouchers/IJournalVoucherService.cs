namespace Retailer.Application.Legacy.JournalVouchers;

public interface IJournalVoucherService : ITransientService
{
    Task<List<JournalVoucherResponse>> GetListAsync(JournalVoucherListFilter filter, CancellationToken cancellationToken);
    Task<List<JournalVoucherLineResponse>> GetDetailAsync(string voucherNo, string? account, CancellationToken cancellationToken);
    Task<decimal> GetAccountBalanceAsync(string accountId, CancellationToken cancellationToken);
    Task<string> CreateAsync(JournalVoucherCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, JournalVoucherUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
}
