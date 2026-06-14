using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.BankReconciliations;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.BankReconciliations;

internal class BankReconciliationService : IBankReconciliationService
{
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;

    public BankReconciliationService(
        IRepository<GlEntry> glRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository)
    {
        _glRepository = glRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
    }

    public async Task<BankReconciliationSnapshotResponse> GetSnapshotAsync(BankReconciliationFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.BankAccount))
            throw new BadRequestException("Bank account is required.");

        var entries = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.CheckNum != null
                && x.VDate >= filter.FromDate
                && x.VDate <= filter.ToDate
                && (x.DrAccountId == filter.BankAccount || x.CrAccountId == filter.BankAccount))
            .Select(x => new
            {
                x.VType,
                x.VoucherNo,
                x.VSeq,
                x.VDate,
                x.CheckDate,
                x.CheckNum,
                x.Clear,
                x.Amount,
                x.DrAccountId,
                x.CrAccountId
            })
            .ToListAsync(cancellationToken);

        var counterpartIds = entries
            .Select(x => x.DrAccountId == filter.BankAccount ? x.CrAccountId : x.DrAccountId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var titleMap = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => counterpartIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var lines = entries
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.VType)
            .ThenBy(x => x.VoucherNo)
            .ThenBy(x => x.VSeq)
            .Select(x =>
            {
                var counterpart = x.DrAccountId == filter.BankAccount ? x.CrAccountId : x.DrAccountId;
                return new BankReconciliationLineResponse
                {
                    VoucherNo = x.VType + "-" + x.VoucherNo,
                    Date = x.VDate,
                    CheckDate = x.CheckDate,
                    CheckNum = x.CheckNum,
                    ReconcileDate = null,
                    Title = counterpart != null && titleMap.TryGetValue(counterpart, out var title)
                        ? title
                        : string.Empty,
                    Dr = x.DrAccountId == filter.BankAccount ? x.Amount : 0,
                    Cr = x.CrAccountId == filter.BankAccount ? x.Amount : 0,
                    Clear = x.Clear != 0,
                    VSeq = x.VSeq
                };
            })
            .ToList();

        var reconcileBalance = await GetBalanceAsync(filter.ToDate, filter.BankAccount, true, cancellationToken);
        var statementBalance = await GetBalanceAsync(filter.ToDate, filter.BankAccount, false, cancellationToken);

        return new BankReconciliationSnapshotResponse
        {
            Lines = lines,
            ReconcileBalance = reconcileBalance,
            StatementBalance = statementBalance
        };
    }

    public async Task SaveAsync(BankReconciliationSaveRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            return;

        foreach (var line in request.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.VoucherNo))
                continue;

            var parts = line.VoucherNo.Split('-');
            if (parts.Length != 2)
                continue;

            var vType = parts[0];
            var voucherNo = parts[1];

            var entries = await _glRepository.GetAll()
                .Where(x => x.VType == vType && x.VoucherNo == voucherNo && x.VSeq == line.VSeq)
                .ToListAsync(cancellationToken);

            foreach (var entry in entries)
            {
                entry.Clear = line.Clear ? 1 : 0;
                await _glRepository.UpdateAsync(entry, false);
            }
        }

        await _glRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<decimal> GetBalanceAsync(
        DateOnly toDate,
        string account,
        bool onlyCleared,
        CancellationToken cancellationToken)
    {
        var query = _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate <= toDate && (x.DrAccountId == account || x.CrAccountId == account));

        if (onlyCleared)
            query = query.Where(x => x.Clear == 1);

        return await query
            .Select(x => x.DrAccountId == account ? x.Amount : -x.Amount)
            .SumAsync(cancellationToken);
    }
}
