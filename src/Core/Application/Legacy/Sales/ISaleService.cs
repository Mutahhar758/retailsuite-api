namespace Retailer.Application.Legacy.Sales;

public interface ISaleService : ITransientService
{
    [Obsolete("Unpaginated list. Use GetPagedListAsync instead; this will be removed.")]
    Task<List<SaleResponse>> GetListAsync(SaleListFilter filter, CancellationToken cancellationToken);
    Task<PaginationResponse<SaleResponse>> GetPagedListAsync(SalePagedListFilter filter, CancellationToken cancellationToken);
    Task<List<SaleLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(SaleCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, SaleUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
    Task<CustomerItemPriceHistoryResponse> GetCustomerItemHistoryAsync(string accountId, string itemId, CancellationToken cancellationToken);
}
