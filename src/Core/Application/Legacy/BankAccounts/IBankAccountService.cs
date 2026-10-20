using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.BankAccounts;

public interface IBankAccountService : ITransientService
{
    Task<List<BankAccountResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<BankAccountResponse?> GetByIdAsync(string account, CancellationToken cancellationToken);
    Task<string> CreateAsync(BankAccountCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string account, BankAccountUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string account, CancellationToken cancellationToken);
}
