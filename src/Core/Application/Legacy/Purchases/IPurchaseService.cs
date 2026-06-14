namespace Retailer.Application.Legacy.Purchases;

public interface IPurchaseService : ITransientService
{
    Task<List<PurchaseResponse>> GetListAsync(PurchaseListFilter filter, CancellationToken cancellationToken);
    Task<List<PurchaseLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(PurchaseCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, PurchaseUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
}
