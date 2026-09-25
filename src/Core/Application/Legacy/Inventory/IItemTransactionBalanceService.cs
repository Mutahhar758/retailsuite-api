using Retailer.Application.Common.Interfaces;
using Retailer.Domain.Legacy;

namespace Retailer.Application.Legacy.Inventory;

public interface IItemTransactionBalanceService : ITransientService
{
    /// <summary>
    /// Computes and sets RunningQtyBalance on each ItemTransaction in the given voucher.
    /// Locks the item rows to guarantee thread-safe balance accumulation.
    /// If any transaction is backdated or modifies historical sequences, enqueues FIFO & balance rebuild.
    /// </summary>
    Task ProcessVoucherRunningBalancesAsync(
        string tenantId,
        string voucherType,
        string voucherNo,
        DateOnly voucherDate,
        IReadOnlyList<ItemTransaction> transactions,
        CancellationToken cancellationToken = default);
}
