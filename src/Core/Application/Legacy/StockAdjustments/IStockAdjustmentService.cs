namespace Retailer.Application.Legacy.StockAdjustments;

public interface IStockAdjustmentService : ITransientService
{
    Task<List<StockAdjustmentResponse>> GetListAsync(StockAdjustmentListFilter filter, CancellationToken cancellationToken);
    Task<List<StockAdjustmentLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<string> CreateAsync(StockAdjustmentCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, StockAdjustmentUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
}
