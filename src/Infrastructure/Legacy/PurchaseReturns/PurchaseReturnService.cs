using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.PurchaseReturns;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.PurchaseReturns;

internal class PurchaseReturnService : IPurchaseReturnService
{
    private const string VType = "PR";

    private readonly IRepository<PurchaseRetMaster> _purchaseRetMasterRepository;
    private readonly IRepository<PurchaseRetDetail> _purchaseRetDetailRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<ItemDetail> _itemRepository;

    public PurchaseReturnService(
        IRepository<PurchaseRetMaster> purchaseRetMasterRepository,
        IRepository<PurchaseRetDetail> purchaseRetDetailRepository,
        IRepository<GlEntry> glRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<ItemDetail> itemRepository)
    {
        _purchaseRetMasterRepository = purchaseRetMasterRepository;
        _purchaseRetDetailRepository = purchaseRetDetailRepository;
        _glRepository = glRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
        _itemRepository = itemRepository;
    }

    public async Task<List<PurchaseReturnResponse>> GetListAsync(PurchaseReturnListFilter filter, CancellationToken cancellationToken)
    {
        var query = _purchaseRetMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == VType);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.VDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.VDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.Account))
            query = query.Where(x => x.AccountId == filter.Account);

        if (!string.IsNullOrWhiteSpace(filter.VoucherNo))
            query = query.Where(x => x.VNo == filter.VoucherNo);

        return await query
            .GroupJoin(
                _chartOfAccountRepository.GetAll().AsNoTracking(),
                m => m.AccountId,
                a => a.Id,
                (m, a) => new { Master = m, Account = a.FirstOrDefault() })
            .Select(x => new PurchaseReturnResponse
            {
                Date = x.Master.VDate,
                VoucherNo = x.Master.VNo,
                Account = x.Account != null ? x.Account.Title : x.Master.AccountId!,
                CreatedBy = x.Master.CreatedBy,
                CreatedOn = x.Master.CreatedOn,
                LastModifiedBy = x.Master.LastModifiedBy,
                LastModifiedOn = x.Master.LastModifiedOn
            })
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PurchaseReturnLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        return await (
            from d in _purchaseRetDetailRepository.GetAll().AsNoTracking()
            join m in _purchaseRetMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            join i in _itemRepository.GetAll().AsNoTracking()
                on d.ItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            where d.VType == VType && d.VNo == voucherNo
            orderby d.Seq
            select new PurchaseReturnLineResponse
            {
                Seq = d.Seq,
                Date = m.VDate,
                VoucherNo = d.VNo,
                AccountId = m.AccountId!,
                Narration = m.Narration != null ? m.Narration.Title : m.NarrationId,
                NarrationId = m.NarrationId,
                Description = m.Descr,
                ItemId = d.ItemId!,
                ItemKey = i != null ? i.ItemKey : null,
                ItemCategoryCode = i != null ? i.ItemCategoryId! : string.Empty,
                Unit = d.UnitId,
                Qty = d.Qty,
                Rate = d.Rate,
                Amount = d.Qty * d.Rate,
                CreatedBy = m.CreatedBy,
                CreatedOn = m.CreatedOn,
                LastModifiedBy = m.LastModifiedBy,
                LastModifiedOn = m.LastModifiedOn
            }).ToListAsync(cancellationToken);
    }

    public async Task<string> CreateAsync(PurchaseReturnCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _purchaseRetMasterRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType)
            .MaxAsync(x => (string?)x.VNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        var amount = request.Lines.Sum(x => x.Qty * x.Rate);
        var master = new PurchaseRetMaster
        {
            VDate = request.Date,
            VTime = TimeOnly.FromDateTime(DateTime.Now),
            VType = VType,
            VNo = voucherNo,
            AccountId = request.Account,
            Descr = request.Description,
            NarrationId = request.Narration,
            Amount = amount,
            Counter = "001"
        };

        await _purchaseRetMasterRepository.AddAsync(master, false);

        foreach (var line in request.Lines)
        {
            await _purchaseRetDetailRepository.AddAsync(new PurchaseRetDetail
            {
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                ItemId = line.ItemId,
                UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                Qty = line.Qty,
                Rate = line.Rate
            }, false);
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, "001", cancellationToken);
        await UpsertGlEntryAsync(voucherNo, request.Date, request.Account, request.Narration, request.Description, amount, cancellationToken);

        await _purchaseRetMasterRepository.SaveChangesAsync(cancellationToken);
        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, PurchaseReturnUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _purchaseRetMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Purchase return voucher '{voucherNo}' not found.");

        var amount = request.Lines.Sum(x => x.Qty * x.Rate);

        master.VDate = request.Date;
        master.VTime = TimeOnly.FromDateTime(DateTime.Now);
        master.AccountId = request.Account;
        master.Descr = request.Description;
        master.NarrationId = request.Narration;
        master.Amount = amount;

        await _purchaseRetMasterRepository.UpdateAsync(master, false);

        foreach (var line in request.Lines)
        {
            var existing = await _purchaseRetDetailRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq,
                    cancellationToken);

            if (existing is null)
            {
                await _purchaseRetDetailRepository.AddAsync(new PurchaseRetDetail
                {
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    ItemId = line.ItemId,
                    UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                    Qty = line.Qty,
                    Rate = line.Rate
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.ItemId = line.ItemId;
                existing.UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit;
                existing.Qty = line.Qty;
                existing.Rate = line.Rate;

                await _purchaseRetDetailRepository.UpdateAsync(existing, false);
            }
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, master.Counter, cancellationToken);
        await UpsertGlEntryAsync(voucherNo, request.Date, request.Account, request.Narration, request.Description, amount, cancellationToken);

        await _purchaseRetMasterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var details = await _purchaseRetDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var masters = await _purchaseRetMasterRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var glEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        var itemTransactions = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        await _purchaseRetDetailRepository.DeleteRangeAsync(details, true);
        await _purchaseRetMasterRepository.DeleteRangeAsync(masters, true);
        await _glRepository.DeleteRangeAsync(glEntries, true);
        await _itemTransactionRepository.DeleteRangeAsync(itemTransactions, true);
    }

    public async Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        var line = await _purchaseRetDetailRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is not null)
            await _purchaseRetDetailRepository.DeleteAsync(line, true);

        var itemTransaction = await _itemTransactionRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (itemTransaction is not null)
            await _itemTransactionRepository.DeleteAsync(itemTransaction, false);

        var amount = await _purchaseRetDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .SumAsync(x => (decimal?)(x.Qty * x.Rate), cancellationToken) ?? 0;

        var master = await _purchaseRetMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is not null)
        {
            master.Amount = amount;
            await _purchaseRetMasterRepository.UpdateAsync(master, false);

            await UpsertGlEntryAsync(
                voucherNo,
                master.VDate,
                master.AccountId!,
                master.NarrationId,
                master.Descr,
                amount,
                cancellationToken);
        }

        await _purchaseRetMasterRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertItemTransactionsAsync(
        string voucherNo,
        DateOnly date,
        string accountId,
        List<PurchaseReturnLineRequest> lines,
        string? counter,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var amount = line.Qty * line.Rate;
            var tx = await _itemTransactionRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq, cancellationToken);

            if (tx is null)
            {
                await _itemTransactionRepository.AddAsync(new ItemTransaction
                {
                    VDate = date,
                    VTime = TimeOnly.FromDateTime(DateTime.Now),
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    TranType = "out",
                    AccountId = accountId,
                    ItemId = line.ItemId,
                    UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                    QtyIn = 0,
                    QtyOut = line.Qty,
                    Rate = line.Rate,
                    Amount = amount,
                    Counter = counter
                }, false);
            }
            else
            {
                tx.DeletedOn = null;
                tx.DeletedBy = null;
                tx.VDate = date;
                tx.VTime = TimeOnly.FromDateTime(DateTime.Now);
                tx.TranType = "out";
                tx.AccountId = accountId;
                tx.ItemId = line.ItemId;
                tx.UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit;
                tx.QtyIn = 0;
                tx.QtyOut = line.Qty;
                tx.Rate = line.Rate;
                tx.Amount = amount;
                tx.Counter = counter;

                await _itemTransactionRepository.UpdateAsync(tx, false);
            }
        }

        var lineSeqSet = lines.Select(x => x.Seq).ToHashSet();
        var staleEntries = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo && !lineSeqSet.Contains(x.Seq))
            .ToListAsync(cancellationToken);

        if (staleEntries.Count > 0)
            await _itemTransactionRepository.DeleteRangeAsync(staleEntries, false);
    }

    private async Task UpsertGlEntryAsync(
        string voucherNo,
        DateOnly date,
        string account,
        string? narration,
        string? description,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var purchaseReturnAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == VType)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(purchaseReturnAccount))
            throw new NotFoundException("Default purchase return account is not configured.");

        var gl = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .FirstOrDefaultAsync(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == 1, cancellationToken);

        if (gl is null)
        {
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = date,
                VTime = TimeOnly.FromDateTime(DateTime.Now),
                VoucherNo = voucherNo,
                VType = VType,
                VSeq = 1,
                DrAccountId = account,
                CrAccountId = purchaseReturnAccount,
                Amount = amount,
                NarrationId = narration,
                Remarks = description,
                Clear = 0
            }, false);
        }
        else
        {
            gl.DeletedOn = null;
            gl.DeletedBy = null;
            gl.VDate = date;
            gl.VTime = TimeOnly.FromDateTime(DateTime.Now);
            gl.DrAccountId = account;
            gl.CrAccountId = purchaseReturnAccount;
            gl.Amount = amount;
            gl.NarrationId = narration;
            gl.Remarks = description;

            await _glRepository.UpdateAsync(gl, false);
        }
    }
}
