using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.SaleReturns;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.SaleReturns;

internal class SaleReturnService : ISaleReturnService
{
    private const string VType = "SR";
    private const string CashDefaultAccountTitle = "Cash";

    private readonly IRepository<SaleRetMaster> _saleRetMasterRepository;
    private readonly IRepository<SaleRetDetail> _saleRetDetailRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly ICurrentTenant _currentTenant;

    public SaleReturnService(
        IRepository<SaleRetMaster> saleRetMasterRepository,
        IRepository<SaleRetDetail> saleRetDetailRepository,
        IRepository<GlEntry> glRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<ItemDetail> itemRepository,
        ICurrentTenant currentTenant)
    {
        _saleRetMasterRepository = saleRetMasterRepository;
        _saleRetDetailRepository = saleRetDetailRepository;
        _glRepository = glRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
        _itemRepository = itemRepository;
        _currentTenant = currentTenant;
    }

    public async Task<List<SaleReturnResponse>> GetListAsync(SaleReturnListFilter filter, CancellationToken cancellationToken)
    {
        var query = _saleRetMasterRepository.GetAll()
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
            .Select(x => new SaleReturnResponse
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

    public async Task<List<SaleReturnLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        return await (
            from d in _saleRetDetailRepository.GetAll().AsNoTracking()
            join m in _saleRetMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            join i in _itemRepository.GetAll().AsNoTracking()
                on d.ItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            where d.VType == VType && d.VNo == voucherNo
            orderby d.Seq
            select new SaleReturnLineResponse
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
                Rate = d.GrossRate ?? 0,
                Discount = d.Discount ?? 0,
                Amount = (d.Qty * ((d.GrossRate ?? 0) - (d.Discount ?? 0))) + ((d.SecQty ?? 0) * (d.SecRate ?? 0)),
                SecUnit = d.SecUnitId,
                SecQty = d.SecQty,
                SecRate = d.SecRate,
                QtyInPack = d.QtyInPack,
                CashReceipt = m.CashReceipt,
                CashBack = m.CashBack ?? 0,
                CreatedBy = m.CreatedBy,
                CreatedOn = m.CreatedOn,
                LastModifiedBy = m.LastModifiedBy,
                LastModifiedOn = m.LastModifiedOn
            }).ToListAsync(cancellationToken);
    }

    public async Task<string> CreateAsync(SaleReturnCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _saleRetMasterRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType)
            .MaxAsync(x => (string?)x.VNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        var isWanda = _currentTenant.HasVariablePackFeature;
        var grossAmount = request.Lines.Sum(x => isWanda
            ? (x.Qty * x.Rate)
            : ((x.Qty * x.Rate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))));
        var discountAmount = request.Lines.Sum(x => x.Qty * x.Discount);
        var netAmount = request.Lines.Sum(x => isWanda
            ? (x.Qty * (x.Rate - x.Discount))
            : ((x.Qty * (x.Rate - x.Discount)) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))));

        var master = new SaleRetMaster
        {
            VDate = request.Date,
            VTime = TimeOnly.FromDateTime(DateTime.Now),
            VType = VType,
            VNo = voucherNo,
            AccountId = request.Account,
            Descr = request.Description,
            NarrationId = request.Narration,
            Amount = grossAmount,
            Discount = discountAmount,
            NetAmount = netAmount,
            CashReceipt = request.CashReceipt,
            CashBack = request.CashBack,
            Counter = "001"
        };

        await _saleRetMasterRepository.AddAsync(master, false);

        foreach (var line in request.Lines)
        {
            await _saleRetDetailRepository.AddAsync(new SaleRetDetail
            {
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                ItemId = line.ItemId,
                UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                Qty = line.Qty,
                GrossRate = line.Rate,
                Discount = line.Discount,
                SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit,
                SecQty = line.SecQty,
                SecRate = line.SecRate,
                QtyInPack = line.QtyInPack
            }, false);
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, "001", cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request, netAmount, cancellationToken);

        await _saleRetMasterRepository.SaveChangesAsync(cancellationToken);
        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, SaleReturnUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _saleRetMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Sale return voucher '{voucherNo}' not found.");

        var isWanda = _currentTenant.HasVariablePackFeature;
        var grossAmount = request.Lines.Sum(x => isWanda
            ? (x.Qty * x.Rate)
            : ((x.Qty * x.Rate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))));
        var discountAmount = request.Lines.Sum(x => x.Qty * x.Discount);
        var netAmount = request.Lines.Sum(x => isWanda
            ? (x.Qty * (x.Rate - x.Discount))
            : ((x.Qty * (x.Rate - x.Discount)) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))));

        master.VDate = request.Date;
        master.VTime = TimeOnly.FromDateTime(DateTime.Now);
        master.AccountId = request.Account;
        master.Descr = request.Description;
        master.NarrationId = request.Narration;
        master.Amount = grossAmount;
        master.Discount = discountAmount;
        master.NetAmount = netAmount;
        master.CashReceipt = request.CashReceipt;
        master.CashBack = request.CashBack;

        await _saleRetMasterRepository.UpdateAsync(master, false);

        foreach (var line in request.Lines)
        {
            var existing = await _saleRetDetailRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq,
                    cancellationToken);

            if (existing is null)
            {
                await _saleRetDetailRepository.AddAsync(new SaleRetDetail
                {
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    ItemId = line.ItemId,
                    UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                    Qty = line.Qty,
                    GrossRate = line.Rate,
                    Discount = line.Discount,
                    SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit,
                    SecQty = line.SecQty,
                    SecRate = line.SecRate,
                    QtyInPack = line.QtyInPack
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.ItemId = line.ItemId;
                existing.UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit;
                existing.Qty = line.Qty;
                existing.GrossRate = line.Rate;
                existing.Discount = line.Discount;
                existing.SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit;
                existing.SecQty = line.SecQty;
                existing.SecRate = line.SecRate;
                existing.QtyInPack = line.QtyInPack;

                await _saleRetDetailRepository.UpdateAsync(existing, false);
            }
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, master.Counter, cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request, netAmount, cancellationToken);

        await _saleRetMasterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var details = await _saleRetDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var masters = await _saleRetMasterRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var glEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        var itemTransactions = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        await _saleRetDetailRepository.DeleteRangeAsync(details, true);
        await _saleRetMasterRepository.DeleteRangeAsync(masters, true);
        await _glRepository.DeleteRangeAsync(glEntries, true);
        await _itemTransactionRepository.DeleteRangeAsync(itemTransactions, true);
    }

    public async Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        var line = await _saleRetDetailRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is not null)
            await _saleRetDetailRepository.DeleteAsync(line, true);

        var itemTransaction = await _itemTransactionRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (itemTransaction is not null)
            await _itemTransactionRepository.DeleteAsync(itemTransaction, false);

        var totals = await _saleRetDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Amount = g.Sum(x => (decimal?)x.Qty * (x.GrossRate ?? 0)) ?? 0,
                Discount = g.Sum(x => (decimal?)x.Qty * (x.Discount ?? 0)) ?? 0,
                NetAmount = g.Sum(x => (decimal?)x.Qty * ((x.GrossRate ?? 0) - (x.Discount ?? 0))) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        var grossAmount = totals?.Amount ?? 0;
        var discountAmount = totals?.Discount ?? 0;
        var netAmount = totals?.NetAmount ?? 0;

        var master = await _saleRetMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is not null)
        {
            master.Amount = grossAmount;
            master.Discount = discountAmount;
            master.NetAmount = netAmount;
            await _saleRetMasterRepository.UpdateAsync(master, false);

            var glRequest = new SaleReturnUpdateRequest
            {
                Date = master.VDate,
                Account = master.AccountId!,
                Description = master.Descr,
                Narration = master.NarrationId,
                CashReceipt = master.CashReceipt,
                CashBack = master.CashBack ?? 0
            };

            await UpsertGlEntriesAsync(voucherNo, glRequest, netAmount, cancellationToken);
        }

        await _saleRetMasterRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertItemTransactionsAsync(
        string voucherNo,
        DateOnly date,
        string accountId,
        List<SaleReturnLineRequest> lines,
        string? counter,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var amount = (line.Qty * (line.Rate - line.Discount)) + ((line.SecQty ?? 0) * (line.SecRate ?? 0));
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
                    TranType = "in",
                    AccountId = accountId,
                    ItemId = line.ItemId,
                    UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                    QtyIn = line.Qty,
                    QtyOut = 0,
                    Rate = line.Rate,
                    Amount = amount,
                    Counter = counter,
                    SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit,
                    SecQtyIn = line.SecQty,
                    SecQtyOut = 0,
                    SecRate = line.SecRate
                }, false);
            }
            else
            {
                tx.DeletedOn = null;
                tx.DeletedBy = null;
                tx.VDate = date;
                tx.VTime = TimeOnly.FromDateTime(DateTime.Now);
                tx.TranType = "in";
                tx.AccountId = accountId;
                tx.ItemId = line.ItemId;
                tx.UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit;
                tx.QtyIn = line.Qty;
                tx.QtyOut = 0;
                tx.Rate = line.Rate;
                tx.Amount = amount;
                tx.Counter = counter;
                tx.SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit;
                tx.SecQtyIn = line.SecQty;
                tx.SecQtyOut = 0;
                tx.SecRate = line.SecRate;

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

    private async Task UpsertGlEntriesAsync(string voucherNo, SaleReturnCreateRequest request, decimal netAmount, CancellationToken cancellationToken)
    {
        var saleReturnAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == VType)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(saleReturnAccount))
            throw new NotFoundException("Default sale return account is not configured.");

        var cashAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == CashDefaultAccountTitle)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(cashAccount))
            throw new NotFoundException("Default cash account is not configured.");

        await UpsertGlEntryAsync(voucherNo, request.Date, 1, saleReturnAccount, request.Account, netAmount, request.Narration, request.Description, cancellationToken);
        await UpsertGlEntryAsync(voucherNo, request.Date, 2, request.Account, cashAccount, request.CashReceipt - request.CashBack, request.Narration, request.Description, cancellationToken);
    }

    private async Task UpsertGlEntriesAsync(string voucherNo, SaleReturnUpdateRequest request, decimal netAmount, CancellationToken cancellationToken)
    {
        var saleReturnAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == VType)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(saleReturnAccount))
            throw new NotFoundException("Default sale return account is not configured.");

        var cashAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == CashDefaultAccountTitle)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(cashAccount))
            throw new NotFoundException("Default cash account is not configured.");

        await UpsertGlEntryAsync(voucherNo, request.Date, 1, saleReturnAccount, request.Account, netAmount, request.Narration, request.Description, cancellationToken);
        await UpsertGlEntryAsync(voucherNo, request.Date, 2, request.Account, cashAccount, request.CashReceipt - request.CashBack, request.Narration, request.Description, cancellationToken);
    }

    private async Task UpsertGlEntryAsync(
        string voucherNo,
        DateOnly date,
        int seq,
        string drAccount,
        string crAccount,
        decimal amount,
        string? narration,
        string? description,
        CancellationToken cancellationToken)
    {
        var gl = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .FirstOrDefaultAsync(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == seq, cancellationToken);

        if (gl is null)
        {
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = date,
                VTime = TimeOnly.FromDateTime(DateTime.Now),
                VoucherNo = voucherNo,
                VType = VType,
                VSeq = seq,
                DrAccountId = drAccount,
                CrAccountId = crAccount,
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
            gl.DrAccountId = drAccount;
            gl.CrAccountId = crAccount;
            gl.Amount = amount;
            gl.NarrationId = narration;
            gl.Remarks = description;

            await _glRepository.UpdateAsync(gl, false);
        }
    }
}
