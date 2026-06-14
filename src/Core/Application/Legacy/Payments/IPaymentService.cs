namespace Retailer.Application.Legacy.Payments;

public interface IPaymentService : ITransientService
{
    Task<List<PaymentResponse>> GetListAsync(PaymentListFilter filter, CancellationToken cancellationToken);
    Task<List<PaymentLineResponse>> GetDetailAsync(string voucherNo, string? cashBankAccount, CancellationToken cancellationToken);
    Task<decimal> GetAccountBalanceAsync(string accountId, CancellationToken cancellationToken);
    Task<string> CreateAsync(PaymentCreateRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(string voucherNo, PaymentUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string voucherNo, CancellationToken cancellationToken);
    Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken);
}
