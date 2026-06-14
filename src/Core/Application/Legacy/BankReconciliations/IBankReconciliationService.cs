namespace Retailer.Application.Legacy.BankReconciliations;

public interface IBankReconciliationService : ITransientService
{
    Task<BankReconciliationSnapshotResponse> GetSnapshotAsync(BankReconciliationFilter filter, CancellationToken cancellationToken);
    Task SaveAsync(BankReconciliationSaveRequest request, CancellationToken cancellationToken);
}
