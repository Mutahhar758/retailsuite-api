namespace Retailer.Application.Legacy.DailyStockClosings;

public interface IDailyStockClosingService : ITransientService
{
    Task<List<DailyStockClosingListItemResponse>> GetListAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
    Task<DailyStockClosingSheetResponse> GetSheetAsync(DateOnly date, string? categoryCode, CancellationToken cancellationToken);
    Task<DailyStockClosingSaveResponse> SaveAsync(DailyStockClosingSaveRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(DateOnly date, CancellationToken cancellationToken);
}
