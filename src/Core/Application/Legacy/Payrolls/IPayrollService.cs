namespace Retailer.Application.Legacy.Payrolls;

public interface IPayrollService : ITransientService
{
    Task<List<PayrollResponse>> GetListAsync(PayrollListFilter filter, CancellationToken cancellationToken);
    Task<List<PayrollLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken);
    Task<PayrollLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken);
    Task<string> CreateAsync(PayrollUpsertRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, PayrollUpsertRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, long seq, CancellationToken cancellationToken);
}
