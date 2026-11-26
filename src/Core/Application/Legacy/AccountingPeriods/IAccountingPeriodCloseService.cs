namespace Retailer.Application.Legacy.AccountingPeriods;

public interface IAccountingPeriodCloseService : ITransientService
{
    Task<PeriodLockStatusResponse> GetLockStatusAsync(CancellationToken cancellationToken);
    Task<List<AccountingPeriodCloseResponse>> GetHistoryAsync(CancellationToken cancellationToken);
    Task<AccountingPeriodCloseDetailResponse> GetDetailAsync(int id, CancellationToken cancellationToken);
    Task<PeriodClosePreviewResponse> PreviewCloseAsync(PeriodClosePreviewRequest request, CancellationToken cancellationToken);
    Task<AccountingPeriodCloseResponse> ExecuteCloseAsync(PeriodCloseExecuteRequest request, CancellationToken cancellationToken);
    Task<AccountingPeriodCloseResponse> ReopenLatestAsync(int id, PeriodReopenRequest request, CancellationToken cancellationToken);
}
