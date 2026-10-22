namespace Retailer.Application.Legacy.Receipts;

public interface IReceiptService : ITransientService
{
    [Obsolete("Unpaginated list. Use GetPagedListAsync instead; this will be removed.")]
    Task<List<ReceiptResponse>> GetListAsync(ReceiptListFilter filter, CancellationToken cancellationToken);
    Task<PaginationResponse<ReceiptResponse>> GetPagedListAsync(ReceiptPagedListFilter filter, CancellationToken cancellationToken);
    Task<List<ReceiptLineResponse>> GetDetailAsync(string voucherNo, string? cashBankAccount, CancellationToken cancellationToken);
    Task<decimal> GetAccountBalanceAsync(string accountId, CancellationToken cancellationToken);
    Task<string> CreateAsync(ReceiptCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, ReceiptUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
}
