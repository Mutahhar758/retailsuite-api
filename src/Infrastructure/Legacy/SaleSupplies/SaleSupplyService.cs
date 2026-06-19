using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.SaleSupplies;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.SaleSupplies;

internal class SaleSupplyService : ISaleSupplyService
{
    private const string VType = "SP";

    private readonly IRepository<SaleSupplyMaster> _saleSupplyMasterRepository;
    private readonly IRepository<SaleSupplyDetail> _saleSupplyDetailRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<ItemDetail> _itemRepository;

    public SaleSupplyService(
        IRepository<SaleSupplyMaster> saleSupplyMasterRepository,
        IRepository<SaleSupplyDetail> saleSupplyDetailRepository,
        IRepository<GlEntry> glRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<ItemDetail> itemRepository)
    {
        _saleSupplyMasterRepository = saleSupplyMasterRepository;
        _saleSupplyDetailRepository = saleSupplyDetailRepository;
        _glRepository = glRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _itemRepository = itemRepository;
    }

    public async Task<List<SaleSupplyResponse>> GetListAsync(SaleSupplyListFilter filter, CancellationToken cancellationToken)
    {
        var query = _saleSupplyMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == VType);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.VDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.VDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.ItemId))
            query = query.Where(x => x.ItemId == filter.ItemId);

        if (!string.IsNullOrWhiteSpace(filter.VoucherNo))
            query = query.Where(x => x.VNo == filter.VoucherNo);

        return await query
            .Select(x => new SaleSupplyResponse
            {
                Date = x.VDate,
                VoucherNo = x.VNo,
                Item = x.Item != null ? x.Item.Title : x.ItemId!,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn,
                LastModifiedBy = x.LastModifiedBy,
                LastModifiedOn = x.LastModifiedOn,
                SupplyOrderMasterId = x.SupplyOrderMasterId,
                SupplyOrderTitle = x.SupplyOrderMaster != null ? x.SupplyOrderMaster.Title : null
            })
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SaleSupplyLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        return await (
            from d in _saleSupplyDetailRepository.GetAll().AsNoTracking()
            join m in _saleSupplyMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            where d.VType == VType && d.VNo == voucherNo
            orderby d.Seq
            select new SaleSupplyLineResponse
            {
                Seq = d.Seq,
                Date = m.VDate,
                VoucherNo = d.VNo,
                ItemId = m.ItemId!,
                Narration = m.Narration != null ? m.Narration.Title : m.NarrationId,
                NarrationId = m.NarrationId,
                Description = m.Descr,
                SupplyOrderMasterId = m.SupplyOrderMasterId,
                CustomerId = d.CustomerAccountId!,
                Unit = d.UnitId,
                Qty = d.Qty,
                Rate = d.GrossRate ?? 0,
                Discount = d.Discount ?? 0,
                AddLess = d.AddLess ?? 0,
                Amount = (d.Qty * ((d.GrossRate ?? 0) - (d.Discount ?? 0))) + (d.AddLess ?? 0),
                CreatedBy = m.CreatedBy,
                CreatedOn = m.CreatedOn,
                LastModifiedBy = m.LastModifiedBy,
                LastModifiedOn = m.LastModifiedOn
            }).ToListAsync(cancellationToken);
    }

    public async Task<string> CreateAsync(SaleSupplyCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _saleSupplyMasterRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType)
            .MaxAsync(x => (string?)x.VNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        var grossAmount = request.Lines.Sum(x => x.Qty * x.Rate);
        var discountAmount = request.Lines.Sum(x => x.Qty * x.Discount);
        var netAmount = request.Lines.Sum(x => (x.Qty * (x.Rate - x.Discount)) + x.AddLess);

        var master = new SaleSupplyMaster
        {
            VDate = request.Date,
            VTime = TimeOnly.FromDateTime(DateTime.Now),
            VType = VType,
            VNo = voucherNo,
            ItemId = request.ItemId,
            Descr = request.Description,
            NarrationId = request.Narration,
            SupplyOrderMasterId = request.SupplyOrderMasterId,
            Amount = grossAmount,
            Discount = discountAmount,
            NetAmount = netAmount,
            Counter = "001"
        };

        await _saleSupplyMasterRepository.AddAsync(master, false);

        foreach (var line in request.Lines)
        {
            await _saleSupplyDetailRepository.AddAsync(new SaleSupplyDetail
            {
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                CustomerAccountId = line.CustomerId,
                UnitId = line.Unit,
                Qty = line.Qty,
                GrossRate = line.Rate,
                Discount = line.Discount,
                AddLess = line.AddLess
            }, false);
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.ItemId, request.Lines, "001", cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request.Date, request.Narration, request.Description, request.Lines, cancellationToken);

        await _saleSupplyMasterRepository.SaveChangesAsync(cancellationToken);
        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, SaleSupplyUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _saleSupplyMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Sale supply voucher '{voucherNo}' not found.");

        var grossAmount = request.Lines.Sum(x => x.Qty * x.Rate);
        var discountAmount = request.Lines.Sum(x => x.Qty * x.Discount);
        var netAmount = request.Lines.Sum(x => (x.Qty * (x.Rate - x.Discount)) + x.AddLess);

        master.VDate = request.Date;
        master.VTime = TimeOnly.FromDateTime(DateTime.Now);
        master.ItemId = request.ItemId;
        master.Descr = request.Description;
        master.NarrationId = request.Narration;
        master.SupplyOrderMasterId = request.SupplyOrderMasterId;
        master.Amount = grossAmount;
        master.Discount = discountAmount;
        master.NetAmount = netAmount;

        await _saleSupplyMasterRepository.UpdateAsync(master, false);

        foreach (var line in request.Lines)
        {
            var existing = await _saleSupplyDetailRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq,
                    cancellationToken);

            if (existing is null)
            {
                await _saleSupplyDetailRepository.AddAsync(new SaleSupplyDetail
                {
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    CustomerAccountId = line.CustomerId,
                    UnitId = line.Unit,
                    Qty = line.Qty,
                    GrossRate = line.Rate,
                    Discount = line.Discount,
                    AddLess = line.AddLess
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.CustomerAccountId = line.CustomerId;
                existing.UnitId = line.Unit;
                existing.Qty = line.Qty;
                existing.GrossRate = line.Rate;
                existing.Discount = line.Discount;
                existing.AddLess = line.AddLess;

                await _saleSupplyDetailRepository.UpdateAsync(existing, false);
            }
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.ItemId, request.Lines, master.Counter, cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request.Date, request.Narration, request.Description, request.Lines, cancellationToken);

        await _saleSupplyMasterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var details = await _saleSupplyDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var masters = await _saleSupplyMasterRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var glEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        var itemTransactions = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        await _saleSupplyDetailRepository.DeleteRangeAsync(details, true);
        await _saleSupplyMasterRepository.DeleteRangeAsync(masters, true);
        await _glRepository.DeleteRangeAsync(glEntries, true);
        await _itemTransactionRepository.DeleteRangeAsync(itemTransactions, true);
    }

    public async Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        var line = await _saleSupplyDetailRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is not null)
            await _saleSupplyDetailRepository.DeleteAsync(line, false);

        var itemTransaction = await _itemTransactionRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (itemTransaction is not null)
            await _itemTransactionRepository.DeleteAsync(itemTransaction, false);

        var totals = await _saleSupplyDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Amount = g.Sum(x => (decimal?)x.Qty * (x.GrossRate ?? 0)) ?? 0,
                Discount = g.Sum(x => (decimal?)x.Qty * (x.Discount ?? 0)) ?? 0,
                NetAmount = g.Sum(x => (decimal?)((x.Qty * ((x.GrossRate ?? 0) - (x.Discount ?? 0))) + (x.AddLess ?? 0))) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        var master = await _saleSupplyMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is not null)
        {
            master.Amount = totals?.Amount ?? 0;
            master.Discount = totals?.Discount ?? 0;
            master.NetAmount = totals?.NetAmount ?? 0;
            await _saleSupplyMasterRepository.UpdateAsync(master, false);
        }

        var gl = await _glRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == seq, cancellationToken);

        if (gl is not null)
            await _glRepository.DeleteAsync(gl, false);

        await _saleSupplyMasterRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertGlEntriesAsync(
        string voucherNo,
        DateOnly date,
        string? narration,
        string? description,
        List<SaleSupplyLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var saleSupplyAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == VType)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(saleSupplyAccount))
            throw new NotFoundException("Default sale supply account is not configured.");

        foreach (var line in lines)
        {
            var amount = (line.Qty * (line.Rate - line.Discount)) + line.AddLess;
            var gl = await _glRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == line.Seq,
                    cancellationToken);

            if (amount <= 0)
            {
                if (gl is not null)
                    await _glRepository.DeleteAsync(gl, false);

                continue;
            }

            if (gl is null)
            {
                await _glRepository.AddAsync(new GlEntry
                {
                    VDate = date,
                    VTime = TimeOnly.FromDateTime(DateTime.Now),
                    VoucherNo = voucherNo,
                    VType = VType,
                    VSeq = line.Seq,
                    DrAccountId = line.CustomerId,
                    CrAccountId = saleSupplyAccount,
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
                gl.DrAccountId = line.CustomerId;
                gl.CrAccountId = saleSupplyAccount;
                gl.Amount = amount;
                gl.NarrationId = narration;
                gl.Remarks = description;

                await _glRepository.UpdateAsync(gl, false);
            }
        }

        var lineSeqSet = lines.Select(x => x.Seq).ToHashSet();
        var staleEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo && !lineSeqSet.Contains(x.VSeq))
            .ToListAsync(cancellationToken);

        if (staleEntries.Count > 0)
            await _glRepository.DeleteRangeAsync(staleEntries, false);
    }

    private async Task UpsertItemTransactionsAsync(
        string voucherNo,
        DateOnly date,
        string itemId,
        List<SaleSupplyLineRequest> lines,
        string? counter,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var amount = (line.Qty * (line.Rate - line.Discount)) + line.AddLess;
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
                    AccountId = line.CustomerId,
                    ItemId = itemId,
                    UnitId = line.Unit,
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
                tx.AccountId = line.CustomerId;
                tx.ItemId = itemId;
                tx.UnitId = line.Unit;
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
}
