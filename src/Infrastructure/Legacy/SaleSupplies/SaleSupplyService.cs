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
                ItemTitle = m.Item != null ? m.Item.Title : m.ItemId,
                Narration = m.Narration != null ? m.Narration.Title : m.NarrationId,
                NarrationId = m.NarrationId,
                Description = m.Descr,
                SupplyOrderMasterId = m.SupplyOrderMasterId,
                CustomerId = d.CustomerAccountId!,
                CustomerTitle = d.CustomerAccount != null ? d.CustomerAccount.Title : d.CustomerAccountId,
                Unit = d.UnitId,
                Qty = d.Qty,
                Rate = d.GrossRate ?? 0,
                Discount = d.Discount ?? 0,
                AddLess = d.AddLess ?? 0,
                Amount = (d.Qty * ((d.GrossRate ?? 0) - (d.Discount ?? 0))) + (d.AddLess ?? 0) + ((d.SecQty ?? 0) * (d.SecRate ?? 0)),
                SecUnit = d.SecUnitId,
                SecQty = d.SecQty,
                SecRate = d.SecRate,
                QtyInPack = d.QtyInPack,
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

        var grossAmount = request.Lines.Sum(x => (x.Qty * x.Rate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0)));
        var discountAmount = request.Lines.Sum(x => x.Qty * x.Discount);
        var netAmount = request.Lines.Sum(x => (x.Qty * (x.Rate - x.Discount)) + x.AddLess + ((x.SecQty ?? 0) * (x.SecRate ?? 0)));

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
                UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                Qty = line.Qty,
                GrossRate = line.Rate,
                Discount = line.Discount,
                AddLess = line.AddLess,
                SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit,
                SecQty = line.SecQty,
                SecRate = line.SecRate,
                QtyInPack = line.QtyInPack
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

        var grossAmount = request.Lines.Sum(x => (x.Qty * x.Rate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0)));
        var discountAmount = request.Lines.Sum(x => x.Qty * x.Discount);
        var netAmount = request.Lines.Sum(x => (x.Qty * (x.Rate - x.Discount)) + x.AddLess + ((x.SecQty ?? 0) * (x.SecRate ?? 0)));

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
                    UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                    Qty = line.Qty,
                    GrossRate = line.Rate,
                    Discount = line.Discount,
                    AddLess = line.AddLess,
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
                existing.CustomerAccountId = line.CustomerId;
                existing.UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit;
                existing.Qty = line.Qty;
                existing.GrossRate = line.Rate;
                existing.Discount = line.Discount;
                existing.AddLess = line.AddLess;
                existing.SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit;
                existing.SecQty = line.SecQty;
                existing.SecRate = line.SecRate;
                existing.QtyInPack = line.QtyInPack;

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
                Amount = g.Sum(x => (decimal?)((x.Qty * (x.GrossRate ?? 0)) + ((x.SecQty ?? 0) * (x.SecRate ?? 0)))) ?? 0,
                Discount = g.Sum(x => (decimal?)x.Qty * (x.Discount ?? 0)) ?? 0,
                NetAmount = g.Sum(x => (decimal?)((x.Qty * ((x.GrossRate ?? 0) - (x.Discount ?? 0))) + (x.AddLess ?? 0) + ((x.SecQty ?? 0) * (x.SecRate ?? 0)))) ?? 0
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
            var amount = (line.Qty * (line.Rate - line.Discount)) + line.AddLess + ((line.SecQty ?? 0) * (line.SecRate ?? 0));
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
            var amount = (line.Qty * (line.Rate - line.Discount)) + line.AddLess + ((line.SecQty ?? 0) * (line.SecRate ?? 0));
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
                    UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                    QtyIn = 0,
                    QtyOut = line.Qty,
                    Rate = line.Rate,
                    Amount = amount,
                    Counter = counter,
                    SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit,
                    SecQtyIn = 0,
                    SecQtyOut = line.SecQty,
                    SecRate = line.SecRate
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
                tx.UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit;
                tx.QtyIn = 0;
                tx.QtyOut = line.Qty;
                tx.Rate = line.Rate;
                tx.Amount = amount;
                tx.Counter = counter;
                tx.SecUnitId = string.IsNullOrWhiteSpace(line.SecUnit) ? null : line.SecUnit;
                tx.SecQtyIn = 0;
                tx.SecQtyOut = line.SecQty;
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

    public async Task<List<SaleSupplyLineResponse>> GetCustomerLinesAsync(
        string customerId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? itemId,
        CancellationToken cancellationToken)
    {
        var query = from d in _saleSupplyDetailRepository.GetAll().AsNoTracking()
                    join m in _saleSupplyMasterRepository.GetAll().AsNoTracking()
                        on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
                    where d.VType == VType && d.CustomerAccountId == customerId
                    select new { d, m };

        if (fromDate.HasValue)
            query = query.Where(x => x.m.VDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.m.VDate <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(itemId))
            query = query.Where(x => x.m.ItemId == itemId);

        return await query
            .OrderByDescending(x => x.m.VDate)
            .ThenByDescending(x => x.m.VNo)
            .ThenBy(x => x.d.Seq)
            .Select(x => new SaleSupplyLineResponse
            {
                Seq = x.d.Seq,
                Date = x.m.VDate,
                VoucherNo = x.d.VNo,
                ItemId = x.m.ItemId!,
                ItemTitle = x.m.Item != null ? x.m.Item.Title : x.m.ItemId,
                Narration = x.m.Narration != null ? x.m.Narration.Title : x.m.NarrationId,
                NarrationId = x.m.NarrationId,
                Description = x.m.Descr,
                SupplyOrderMasterId = x.m.SupplyOrderMasterId,
                CustomerId = x.d.CustomerAccountId!,
                CustomerTitle = x.d.CustomerAccount != null ? x.d.CustomerAccount.Title : x.d.CustomerAccountId,
                Unit = x.d.UnitId,
                Qty = x.d.Qty,
                Rate = x.d.GrossRate ?? 0,
                Discount = x.d.Discount ?? 0,
                AddLess = x.d.AddLess ?? 0,
                Amount = (x.d.Qty * ((x.d.GrossRate ?? 0) - (x.d.Discount ?? 0))) + (x.d.AddLess ?? 0) + ((x.d.SecQty ?? 0) * (x.d.SecRate ?? 0)),
                SecUnit = x.d.SecUnitId,
                SecQty = x.d.SecQty,
                SecRate = x.d.SecRate,
                QtyInPack = x.d.QtyInPack,
                CreatedBy = x.m.CreatedBy,
                CreatedOn = x.m.CreatedOn,
                LastModifiedBy = x.m.LastModifiedBy,
                LastModifiedOn = x.m.LastModifiedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateLineAsync(string voucherNo, int seq, SaleSupplyLineRequest request, CancellationToken cancellationToken)
    {
        var line = await _saleSupplyDetailRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is null)
            throw new NotFoundException($"Sale supply line seq '{seq}' for voucher '{voucherNo}' not found.");

        var master = await _saleSupplyMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Sale supply voucher '{voucherNo}' not found.");

        line.CustomerAccountId = request.CustomerId;
        line.UnitId = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit;
        line.Qty = request.Qty;
        line.GrossRate = request.Rate;
        line.Discount = request.Discount;
        line.AddLess = request.AddLess;
        line.SecUnitId = string.IsNullOrWhiteSpace(request.SecUnit) ? null : request.SecUnit;
        line.SecQty = request.SecQty;
        line.SecRate = request.SecRate;
        line.QtyInPack = request.QtyInPack;

        await _saleSupplyDetailRepository.UpdateAsync(line, false);

        var allDetails = await _saleSupplyDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        master.Amount = allDetails.Sum(x => (x.Qty * (x.GrossRate ?? 0)) + ((x.SecQty ?? 0) * (x.SecRate ?? 0)));
        master.Discount = allDetails.Sum(x => x.Qty * (x.Discount ?? 0));
        master.NetAmount = allDetails.Sum(x => (x.Qty * ((x.GrossRate ?? 0) - (x.Discount ?? 0))) + (x.AddLess ?? 0) + ((x.SecQty ?? 0) * (x.SecRate ?? 0)));

        await _saleSupplyMasterRepository.UpdateAsync(master, false);

        var netLineAmt = (request.Qty * (request.Rate - request.Discount)) + request.AddLess + ((request.SecQty ?? 0) * (request.SecRate ?? 0));
        var tx = await _itemTransactionRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (tx is not null)
        {
            tx.DeletedOn = null;
            tx.DeletedBy = null;
            tx.VDate = master.VDate;
            tx.AccountId = request.CustomerId;
            tx.ItemId = master.ItemId!;
            tx.UnitId = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit;
            tx.QtyOut = request.Qty;
            tx.Rate = request.Rate;
            tx.Amount = netLineAmt;
            tx.SecUnitId = string.IsNullOrWhiteSpace(request.SecUnit) ? null : request.SecUnit;
            tx.SecQtyOut = request.SecQty;
            tx.SecRate = request.SecRate;

            await _itemTransactionRepository.UpdateAsync(tx, false);
        }

        var saleSupplyAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == VType)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(saleSupplyAccount))
        {
            var gl = await _glRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == seq, cancellationToken);

            if (gl is not null)
            {
                if (netLineAmt <= 0)
                {
                    await _glRepository.DeleteAsync(gl, false);
                }
                else
                {
                    gl.DeletedOn = null;
                    gl.DeletedBy = null;
                    gl.VDate = master.VDate;
                    gl.DrAccountId = request.CustomerId;
                    gl.CrAccountId = saleSupplyAccount;
                    gl.Amount = netLineAmt;
                    await _glRepository.UpdateAsync(gl, false);
                }
            }
            else if (netLineAmt > 0)
            {
                await _glRepository.AddAsync(new GlEntry
                {
                    VDate = master.VDate,
                    VTime = TimeOnly.FromDateTime(DateTime.Now),
                    VoucherNo = voucherNo,
                    VType = VType,
                    VSeq = seq,
                    DrAccountId = request.CustomerId,
                    CrAccountId = saleSupplyAccount,
                    Amount = netLineAmt,
                    NarrationId = master.NarrationId,
                    Remarks = master.Descr,
                    Clear = 0
                }, false);
            }
        }

        await _saleSupplyMasterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCustomerLinesAsync(List<SaleSupplyCustomerLineUpdateRequest> requests, CancellationToken cancellationToken)
    {
        foreach (var item in requests)
        {
            await UpdateLineAsync(item.VoucherNo, item.Seq, item.Line, cancellationToken);
        }
    }
}
