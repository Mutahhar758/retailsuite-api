using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.StockAdjustments;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.StockAdjustments;

internal class StockAdjustmentService : IStockAdjustmentService
{
    private const string VType = "SA";

    private readonly IRepository<StockAdjMaster> _stockAdjMasterRepository;
    private readonly IRepository<StockAdjDetail> _stockAdjDetailRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<ItemDetail> _itemRepository;

    public StockAdjustmentService(
        IRepository<StockAdjMaster> stockAdjMasterRepository,
        IRepository<StockAdjDetail> stockAdjDetailRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<ItemDetail> itemRepository)
    {
        _stockAdjMasterRepository = stockAdjMasterRepository;
        _stockAdjDetailRepository = stockAdjDetailRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _itemRepository = itemRepository;
    }

    public async Task<List<StockAdjustmentResponse>> GetListAsync(StockAdjustmentListFilter filter, CancellationToken cancellationToken)
    {
        var query = _stockAdjMasterRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Narration)
            .Where(x => x.VType == VType);

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.VDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.VDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.VoucherNo))
            query = query.Where(x => x.VNo == filter.VoucherNo);

        if (!string.IsNullOrWhiteSpace(filter.ItemCategoryCode))
        {
            var category = filter.ItemCategoryCode;
            query = query.Where(x => _stockAdjDetailRepository.GetAll()
                .Any(d => d.VType == x.VType && d.VNo == x.VNo && d.CategoryId == category));
        }

        var details = _stockAdjDetailRepository.GetAll().AsNoTracking().Where(d => d.VType == VType);

        return await (from m in query
                      join d in details on m.VNo equals d.VNo into detailGroup
                      select new StockAdjustmentResponse
                      {
                          Date = m.VDate,
                          VoucherNo = m.VNo,
                          Narration = m.Narration != null ? m.Narration.Title : m.NarrationId,
                          NarrationId = m.NarrationId,
                          Description = m.Descr ?? string.Empty,
                          TotalQty = detailGroup.Sum(d => d.QtyIn - d.QtyOut),
                          TotalAmount = detailGroup.Sum(d => (d.QtyIn - d.QtyOut) * d.Rate),
                          CreatedBy = m.CreatedBy,
                          CreatedOn = m.CreatedOn,
                          LastModifiedBy = m.LastModifiedBy,
                          LastModifiedOn = m.LastModifiedOn
                      })
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockAdjustmentLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        return await (
            from d in _stockAdjDetailRepository.GetAll().AsNoTracking()
            join m in _stockAdjMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            join i in _itemRepository.GetAll().AsNoTracking()
                on d.ItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            where d.VType == VType && d.VNo == voucherNo
            orderby d.Seq
            select new StockAdjustmentLineResponse
            {
                Seq = d.Seq,
                Date = m.VDate,
                VoucherNo = d.VNo,
                Narration = m.Narration != null ? m.Narration.Title : m.NarrationId,
                NarrationId = m.NarrationId,
                Description = m.Descr,
                ItemCategoryCode = d.CategoryId!,
                ItemId = d.ItemId!,
                ItemKey = i != null ? i.ItemKey : null,
                Unit = i != null ? (i.DefaultUnitId ?? i.PrimaryUnitId) : null,
                QtyIn = d.QtyIn,
                QtyOut = d.QtyOut,
                Rate = d.Rate,
                Amount = (d.QtyIn - d.QtyOut) * d.Rate,
                CreatedBy = m.CreatedBy,
                CreatedOn = m.CreatedOn,
                LastModifiedBy = m.LastModifiedBy,
                LastModifiedOn = m.LastModifiedOn
            }).ToListAsync(cancellationToken);
    }

    public async Task<string> CreateAsync(StockAdjustmentCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _stockAdjMasterRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType)
            .MaxAsync(x => (string?)x.VNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        var master = new StockAdjMaster
        {
            VDate = request.Date,
            VTime = TimeOnly.FromDateTime(DateTime.Now),
            VType = VType,
            VNo = voucherNo,
            Descr = request.Description,
            NarrationId = request.Narration,
            Terminal = "001"
        };

        await _stockAdjMasterRepository.AddAsync(master, false);

        foreach (var line in request.Lines)
        {
            await _stockAdjDetailRepository.AddAsync(new StockAdjDetail
            {
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                CategoryId = line.ItemCategoryCode,
                ItemId = line.ItemId,
                QtyIn = line.QtyIn,
                QtyOut = line.QtyOut,
                Rate = line.Rate
            }, false);
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Lines, "001", cancellationToken);
        await _stockAdjMasterRepository.SaveChangesAsync(cancellationToken);
        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, StockAdjustmentUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _stockAdjMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Stock adjustment voucher '{voucherNo}' not found.");

        master.VDate = request.Date;
        master.VTime = TimeOnly.FromDateTime(DateTime.Now);
        master.Descr = request.Description;
        master.NarrationId = request.Narration;

        await _stockAdjMasterRepository.UpdateAsync(master, false);

        foreach (var line in request.Lines)
        {
            var existing = await _stockAdjDetailRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq,
                    cancellationToken);

            if (existing is null)
            {
                await _stockAdjDetailRepository.AddAsync(new StockAdjDetail
                {
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    CategoryId = line.ItemCategoryCode,
                    ItemId = line.ItemId,
                    QtyIn = line.QtyIn,
                    QtyOut = line.QtyOut,
                    Rate = line.Rate
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.CategoryId = line.ItemCategoryCode;
                existing.ItemId = line.ItemId;
                existing.QtyIn = line.QtyIn;
                existing.QtyOut = line.QtyOut;
                existing.Rate = line.Rate;

                await _stockAdjDetailRepository.UpdateAsync(existing, false);
            }
        }
        
        var lineSeqSet = request.Lines.Select(x => x.Seq).ToHashSet();
        var staleDetails = await _stockAdjDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo && !lineSeqSet.Contains(x.Seq))
            .ToListAsync(cancellationToken);

        if (staleDetails.Count > 0)
            await _stockAdjDetailRepository.DeleteRangeAsync(staleDetails, false);

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Lines, master.Terminal, cancellationToken);
        await _stockAdjMasterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var details = await _stockAdjDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var masters = await _stockAdjMasterRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var itemTransactions = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        await _stockAdjDetailRepository.DeleteRangeAsync(details, true);
        await _stockAdjMasterRepository.DeleteRangeAsync(masters, true);
        await _itemTransactionRepository.DeleteRangeAsync(itemTransactions, true);
    }

    public async Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        var line = await _stockAdjDetailRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is not null)
            await _stockAdjDetailRepository.DeleteAsync(line, true);

        var itemTransaction = await _itemTransactionRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (itemTransaction is not null)
            await _itemTransactionRepository.DeleteAsync(itemTransaction, true);
    }

    private async Task UpsertItemTransactionsAsync(
        string voucherNo,
        DateOnly date,
        List<StockAdjustmentLineRequest> lines,
        string? counter,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var netQty = line.QtyIn - line.QtyOut;
            var amount = netQty * line.Rate;
            var tranType = netQty >= 0 ? "in" : "out";
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
                    TranType = tranType,
                    AccountId = null,
                    ItemId = line.ItemId,
                    UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit,
                    QtyIn = line.QtyIn,
                    QtyOut = line.QtyOut,
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
                tx.TranType = tranType;
                tx.AccountId = null;
                tx.ItemId = line.ItemId;
                tx.UnitId = string.IsNullOrWhiteSpace(line.Unit) ? null : line.Unit;
                tx.QtyIn = line.QtyIn;
                tx.QtyOut = line.QtyOut;
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
