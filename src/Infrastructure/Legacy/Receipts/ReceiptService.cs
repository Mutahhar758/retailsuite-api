using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Receipts;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Receipts;

internal class ReceiptService : IReceiptService
{
    private const string VType = "RV";

    private readonly IRepository<GlEntry> _glRepository;

    public ReceiptService(IRepository<GlEntry> glRepository)
    {
        _glRepository = glRepository;
    }

    public async Task<List<ReceiptResponse>> GetListAsync(ReceiptListFilter filter, CancellationToken cancellationToken)
    {
        var query = _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == VType);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.VDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.VDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.CashBankAccount))
            query = query.Where(x => x.DrAccountId == filter.CashBankAccount);

        if (!string.IsNullOrWhiteSpace(filter.Account))
            query = query.Where(x => x.CrAccountId == filter.Account);

        if (!string.IsNullOrWhiteSpace(filter.Narration))
            query = query.Where(x => x.NarrationId == filter.Narration);

        return await query
            .GroupBy(x => new
            {
                x.VoucherNo,
                x.VDate,
                x.ClearingDate,
                Narration = x.Narration != null ? x.Narration.Title : x.NarrationId,
                NarrationId = x.NarrationId
            })
            .Select(g => new ReceiptResponse
            {
                VoucherNo = g.Key.VoucherNo,
                Date = g.Key.VDate,
                ClearingDate = g.Key.ClearingDate,
                Amount = g.Sum(x => x.Amount),
                Narration = g.Key.Narration,
                NarrationId = g.Key.NarrationId,
                CreatedBy = g.Max(x => x.CreatedBy),
                CreatedOn = g.Min(x => x.CreatedOn),
                LastModifiedBy = g.Max(x => x.LastModifiedBy),
                LastModifiedOn = g.Max(x => x.LastModifiedOn)
            })
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ReceiptLineResponse>> GetDetailAsync(string voucherNo, string? cashBankAccount, CancellationToken cancellationToken)
    {
        var query = _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo);

        if (!string.IsNullOrWhiteSpace(cashBankAccount))
            query = query.Where(x => x.DrAccountId == cashBankAccount || x.CrAccountId == cashBankAccount);

        return await query
            .OrderBy(x => x.VSeq)
            .Select(x => new ReceiptLineResponse
            {
                Seq = x.VSeq,
                Date = x.VDate,
                ClearingDate = x.ClearingDate,
                VoucherNo = x.VoucherNo,
                CashBankAccountId = x.DrAccountId!,
                AccountId = x.CrAccountId!,
                Amount = x.Amount,
                Narration = x.Narration != null ? x.Narration.Title : x.NarrationId,
                NarrationId = x.NarrationId,
                CheckNum = x.CheckNum,
                CheckDate = x.CheckDate,
                Remarks = x.Remarks,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn,
                LastModifiedBy = x.LastModifiedBy,
                LastModifiedOn = x.LastModifiedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetAccountBalanceAsync(string accountId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            return 0;

        var dr = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.DrAccountId == accountId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        var cr = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.CrAccountId == accountId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

        return dr - cr;
    }

    public async Task<string> CreateAsync(ReceiptCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType)
            .MaxAsync(x => (string?)x.VoucherNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        foreach (var line in request.Lines)
        {
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = request.Date,
                ClearingDate = request.ClearingDate,
                VTime = TimeOnly.FromDateTime(DateTime.Now),
                VoucherNo = voucherNo,
                VType = VType,
                VSeq = line.Seq,
                DrAccountId = request.CashBankAccount,
                Amount = line.Amount,
                CrAccountId = line.Account,
                NarrationId = request.Narration,
                Remarks = line.Remarks,
                CheckNum = line.CheckNum,
                CheckDate = line.CheckDate,
                CheckStatus = line.CheckNum != null ? "Pending" : null,
                Clear = line.CheckNum != null ? 2m : 0m
            }, false);
        }

        await _glRepository.SaveChangesAsync(cancellationToken);
        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, ReceiptUpdateRequest request, CancellationToken cancellationToken)
    {
        var anyExists = await _glRepository.GetAll()
            .AnyAsync(x => x.VType == VType && x.VoucherNo == voucherNo, cancellationToken);

        if (!anyExists)
            throw new NotFoundException($"Receipt voucher '{voucherNo}' not found.");

        foreach (var line in request.Lines)
        {
            var existing = await _glRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == line.Seq,
                    cancellationToken);

            if (existing is null)
            {
                await _glRepository.AddAsync(new GlEntry
                {
                    VDate = request.Date,
                    ClearingDate = request.ClearingDate,
                    VTime = TimeOnly.FromDateTime(DateTime.Now),
                    VoucherNo = voucherNo,
                    VType = VType,
                    VSeq = line.Seq,
                    DrAccountId = request.CashBankAccount,
                    Amount = line.Amount,
                    CrAccountId = line.Account,
                    NarrationId = request.Narration,
                    Remarks = line.Remarks,
                    CheckNum = line.CheckNum,
                    CheckDate = line.CheckDate,
                    CheckStatus = line.CheckNum != null ? "Pending" : null,
                    Clear = line.CheckNum != null ? 2m : 0m
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.VDate = request.Date;
                existing.ClearingDate = request.ClearingDate;
                existing.VTime = TimeOnly.FromDateTime(DateTime.Now);
                existing.DrAccountId = request.CashBankAccount;
                existing.Amount = line.Amount;
                existing.CrAccountId = line.Account;
                existing.NarrationId = request.Narration;
                existing.Remarks = line.Remarks;
                existing.CheckNum = line.CheckNum;
                existing.CheckDate = line.CheckDate;
                existing.CheckStatus = line.CheckNum != null ? "Pending" : existing.CheckStatus;

                await _glRepository.UpdateAsync(existing, false);
            }
        }

        await _glRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var entries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        await _glRepository.DeleteRangeAsync(entries, true);
    }

    public async Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        var entry = await _glRepository.GetAll()
            .FirstOrDefaultAsync(
                x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == seq,
                cancellationToken);

        if (entry is not null)
            await _glRepository.DeleteAsync(entry, true);
    }
}
