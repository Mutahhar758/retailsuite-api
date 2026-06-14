namespace Retailer.Application.Legacy.PurchaseReturns;

public interface IPurchaseReturnService : ITransientService
{
    Task<List<PurchaseReturnResponse>> GetListAsync(PurchaseReturnListFilter filter, CancellationToken cancellationToken);
    Task<List<PurchaseReturnLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(PurchaseReturnCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, PurchaseReturnUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
}
