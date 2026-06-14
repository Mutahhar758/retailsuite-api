namespace Retailer.Application.Legacy.ChartOfAccounts;

public interface IChartOfAccountService : ITransientService
{
    Task<List<ChartOfAccountResponse>> GetActiveAsync(CancellationToken cancellationToken);
    Task<List<ChartOfAccountHeadResponse>> GetHeadsAsync(int level, CancellationToken cancellationToken);
    Task<List<ChartOfAccountHeadResponse>> GetByPrefixAsync(string prefix, int? level, CancellationToken cancellationToken);
    Task<List<ChartOfAccountHeadResponse>> GetDetailAccountsAsync(CancellationToken cancellationToken);
    Task<List<ChartOfAccountHeadResponse>> GetCashBankAccountsAsync(CancellationToken cancellationToken);
    Task<List<ChartOfAccountHeadResponse>> GetSupplierAccountsAsync(CancellationToken cancellationToken);
    Task<List<ChartOfAccountHeadResponse>> GetCustomerAccountsAsync(CancellationToken cancellationToken);
    Task<string> CreateAsync(ChartOfAccountCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string account, ChartOfAccountUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string account, CancellationToken cancellationToken);
}
