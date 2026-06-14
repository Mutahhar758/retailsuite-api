namespace Retailer.Application.Legacy.SaleReturns;

public interface ISaleReturnService : ITransientService
{
    Task<List<SaleReturnResponse>> GetListAsync(SaleReturnListFilter filter, CancellationToken cancellationToken);
    Task<List<SaleReturnLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(SaleReturnCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, SaleReturnUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
}
