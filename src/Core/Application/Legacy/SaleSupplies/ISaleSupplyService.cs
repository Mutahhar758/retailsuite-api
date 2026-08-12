namespace Retailer.Application.Legacy.SaleSupplies;

public interface ISaleSupplyService : ITransientService
{
    Task<List<SaleSupplyResponse>> GetListAsync(SaleSupplyListFilter filter, CancellationToken cancellationToken);
    Task<List<SaleSupplyLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(SaleSupplyCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, SaleSupplyUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
    Task<List<SaleSupplyLineResponse>> GetCustomerLinesAsync(string customerId, DateOnly? fromDate, DateOnly? toDate, string? itemId, CancellationToken cancellationToken);
    Task UpdateLineAsync(string voucherNo, int seq, SaleSupplyLineRequest request, CancellationToken cancellationToken);
    Task UpdateCustomerLinesAsync(List<SaleSupplyCustomerLineUpdateRequest> requests, CancellationToken cancellationToken);
}
