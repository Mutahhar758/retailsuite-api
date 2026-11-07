using Hangfire;
using Microsoft.EntityFrameworkCore;
using Retailer.Shared.Common;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Common;
using Retailer.Application.Legacy.Inventory;
using Retailer.Application.Common.Models;
using Retailer.Application.Legacy.Sales;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Sales;

internal class SaleService : ISaleService
{
    private const string VType = "SL";
    private const string CashDefaultAccountTitle = "Cash";

    private readonly IRepository<SaleMaster> _saleMasterRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<SalePayment> _salePaymentRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<CustomerDetail> _customerDetailRepository;
    private readonly IFifoCostingService _fifoCostingService;
    private readonly IItemTransactionBalanceService _balanceService;
    private readonly ICurrentTenant _currentTenant;

    public SaleService(
        IRepository<SaleMaster> saleMasterRepository,
        IRepository<Sale> saleRepository,
        IRepository<SalePayment> salePaymentRepository,
        IRepository<GlEntry> glRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<CustomerDetail> customerDetailRepository,
        IFifoCostingService fifoCostingService,
        IItemTransactionBalanceService balanceService,
        ICurrentTenant currentTenant)
    {
        _saleMasterRepository = saleMasterRepository;
        _saleRepository = saleRepository;
        _salePaymentRepository = salePaymentRepository;
        _glRepository = glRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
        _itemRepository = itemRepository;
        _customerDetailRepository = customerDetailRepository;
        _fifoCostingService = fifoCostingService;
        _balanceService = balanceService;
        _currentTenant = currentTenant;
    }

    [Obsolete("Unpaginated list. Use GetPagedListAsync instead; this will be removed.")]
    public async Task<List<SaleResponse>> GetListAsync(SaleListFilter filter, CancellationToken cancellationToken)
    {
        var query = _saleMasterRepository.GetAll()
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
            .Select(x => new SaleResponse
            {
                Date = x.Master.VDate,
                VoucherNo = x.Master.VNo,
                Account = x.Account != null ? x.Account.Title : x.Master.AccountId!,
                Amount = x.Master.NetAmount ?? x.Master.Amount ?? 0,
                CreatedBy = x.Master.CreatedBy,
                CreatedOn = x.Master.CreatedOn,
                LastModifiedBy = x.Master.LastModifiedBy,
                LastModifiedOn = x.Master.LastModifiedOn
            })
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginationResponse<SaleResponse>> GetPagedListAsync(SalePagedListFilter filter, CancellationToken cancellationToken)
    {
        var query = _saleMasterRepository.GetAll()
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

        var projected = query
            .GroupJoin(
                _chartOfAccountRepository.GetAll().AsNoTracking(),
                m => m.AccountId,
                a => a.Id,
                (m, a) => new { Master = m, Account = a.FirstOrDefault() })
            .Select(x => new SaleResponse
            {
                Date = x.Master.VDate,
                VoucherNo = x.Master.VNo,
                Account = x.Account != null ? x.Account.Title : x.Master.AccountId!,
                Amount = x.Master.NetAmount ?? x.Master.Amount ?? 0,
                CreatedBy = x.Master.CreatedBy,
                CreatedOn = x.Master.CreatedOn,
                LastModifiedBy = x.Master.LastModifiedBy,
                LastModifiedOn = x.Master.LastModifiedOn
            });

        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0
            ? SalePagedListFilter.DefaultPageSize
            : Math.Min(filter.PageSize, SalePagedListFilter.MaxPageSize);

        var totalCount = await projected.CountAsync(cancellationToken);

        var items = await projected
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginationResponse<SaleResponse>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<List<SaleLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        return await (
            from d in _saleRepository.GetAll().AsNoTracking()
            join m in _saleMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            join i in _itemRepository.GetAll().AsNoTracking()
                on d.ItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            where d.VType == VType && d.VNo == voucherNo
            orderby d.Seq
            select new SaleLineResponse
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
                Rate = d.GrossRate,
                Discount = d.Discount,
                Carriage = d.Carriage,
                Amount = d.Amount,
                SecUnit = d.SecUnitId,
                SecQty = d.SecQty,
                SecRate = d.SecRate,
                QtyInPack = d.QtyInPack,
                Packing = d.Packing,
                Imei = d.Imei,
                Imei2 = d.Imei2,
                PtaStatus = d.PtaStatus,
                ConditionNote = d.ConditionNote,
                BatteryHealth = d.BatteryHealth,
                WarrantyMonths = d.WarrantyMonths,
                WarrantyExpiryDate = d.WarrantyExpiryDate,
                CashReceipt = m.CashReceipt,
                CashBack = m.CashBack ?? 0,
                CreatedBy = m.CreatedBy,
                CreatedOn = m.CreatedOn,
                LastModifiedBy = m.LastModifiedBy,
                LastModifiedOn = m.LastModifiedOn
            }).ToListAsync(cancellationToken);
    }

    public async Task<List<PaymentRecordResponse>> GetPaymentsAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var payments = await _salePaymentRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .OrderBy(x => x.Seq)
            .Select(x => new PaymentRecordResponse
            {
                Seq = x.Seq,
                AccountId = x.AccountId,
                AccountTitle = x.Account != null ? x.Account.Title : x.AccountId,
                Amount = x.Amount,
                Reference = x.Reference,
                CheckNum = x.CheckNum,
                CheckDate = x.CheckDate
            })
            .ToListAsync(cancellationToken);

        if (payments.Count == 0)
        {
            var master = await _saleMasterRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == VType && x.VNo == voucherNo)
                .Select(x => new { x.CashReceipt, x.CashBack })
                .FirstOrDefaultAsync(cancellationToken);

            var netCash = (master?.CashReceipt ?? 0) - (master?.CashBack ?? 0);
            if (netCash > 0)
            {
                var glPayments = await _glRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq >= 2)
                    .OrderBy(x => x.VSeq)
                    .ToListAsync(cancellationToken);

                if (glPayments.Count > 0)
                {
                    var accountIds = glPayments.Select(g => g.DrAccountId).Distinct().ToList();
                    var titles = await _chartOfAccountRepository.GetAll()
                        .AsNoTracking()
                        .Where(a => accountIds.Contains(a.Id))
                        .ToDictionaryAsync(a => a.Id, a => a.Title, cancellationToken);

                    return glPayments.Select((g, idx) => new PaymentRecordResponse
                    {
                        Seq = idx + 1,
                        AccountId = g.DrAccountId,
                        AccountTitle = titles.GetValueOrDefault(g.DrAccountId, g.DrAccountId),
                        Amount = g.Amount,
                        Reference = g.Remarks
                    }).ToList();
                }
                else
                {
                    var defaultCash = await _defaultAccountRepository.GetAll()
                        .AsNoTracking()
                        .Where(x => x.Title == CashDefaultAccountTitle)
                        .Select(x => x.AccountId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (!string.IsNullOrEmpty(defaultCash))
                    {
                        var cashTitle = await _chartOfAccountRepository.GetAll()
                            .AsNoTracking()
                            .Where(a => a.Id == defaultCash)
                            .Select(a => a.Title)
                            .FirstOrDefaultAsync(cancellationToken);

                        return
                        [
                            new PaymentRecordResponse
                            {
                                Seq = 1,
                                AccountId = defaultCash,
                                AccountTitle = cashTitle ?? "Cash",
                                Amount = netCash
                            }
                        ];
                    }
                }
            }
        }

        return payments;
    }

    public async Task<string> CreateAsync(SaleCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _saleMasterRepository.GetAll()
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
        var discountAmount = request.Lines.Sum(x => x.Discount);
        var netAmount = request.Lines.Sum(x => isWanda
            ? ((x.Qty * x.Rate) - x.Discount + ((x.SecQty ?? 0) * (x.Carriage ?? 0)))
            : (((x.Qty * x.Rate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))) - x.Discount + (x.Carriage ?? 0)));

        var validPayments = request.Payments?
            .Where(p => !string.IsNullOrWhiteSpace(p.AccountId) && p.Amount > 0)
            .ToList();
        var cashReceipt = validPayments != null && validPayments.Count > 0
            ? validPayments.Sum(p => p.Amount)
            : request.CashReceipt;
        var cashBack = validPayments != null && validPayments.Count > 0
            ? 0
            : request.CashBack;

        var master = new SaleMaster
        {
            VDate = request.Date,
            VTime = PakistanTime.CurrentTime,
            VType = VType,
            VNo = voucherNo,
            AccountId = request.Account,
            Descr = request.Description,
            NarrationId = request.Narration,
            Amount = grossAmount,
            Discount = discountAmount,
            NetAmount = netAmount,
            CashReceipt = cashReceipt,
            CashBack = cashBack,
            Counter = "001"
        };

        await _saleMasterRepository.AddAsync(master, false);

        if (validPayments != null && validPayments.Count > 0)
        {
            var pSeq = 1;
            foreach (var p in validPayments)
            {
                await _salePaymentRepository.AddAsync(new SalePayment
                {
                    VType = VType,
                    VNo = voucherNo,
                    Seq = pSeq++,
                    AccountId = p.AccountId,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    CheckNum = p.CheckNum,
                    CheckDate = p.CheckDate
                }, false);
            }
        }

        var itemIds = request.Lines.Select(x => x.ItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var itemMap = await _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var line in request.Lines)
        {
            var item = itemMap.GetValueOrDefault(line.ItemId);
            if (item?.RequireImei == true && string.IsNullOrWhiteSpace(line.Imei))
            {
                throw new CustomException($"Item '{item.Title}' requires an IMEI / Serial number.");
            }

            var resolvedUnitId = item?.DefaultUnitId ?? item?.PrimaryUnitId;
            var resolvedSecUnitId = item?.SecondaryUnitId;

            var lineAmount = isWanda
                ? (line.Qty * line.Rate) - line.Discount + ((line.SecQty ?? 0) * (line.Carriage ?? 0))
                : ((line.Qty * line.Rate) + ((line.SecQty ?? 0) * (line.SecRate ?? 0))) - line.Discount + (line.Carriage ?? 0);

            await _saleRepository.AddAsync(new Sale
            {
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                ItemId = line.ItemId,
                UnitId = resolvedUnitId,
                Qty = line.Qty,
                GrossRate = line.Rate,
                Discount = line.Discount,
                Carriage = line.Carriage ?? 0m,
                Amount = lineAmount,
                SecUnitId = resolvedSecUnitId,
                SecQty = line.SecQty,
                SecRate = line.SecRate,
                QtyInPack = line.QtyInPack,
                Packing = line.Packing,
                Imei = line.Imei,
                Imei2 = line.Imei2,
                PtaStatus = line.PtaStatus,
                ConditionNote = line.ConditionNote,
                BatteryHealth = line.BatteryHealth,
                WarrantyMonths = line.WarrantyMonths,
                WarrantyExpiryDate = line.WarrantyExpiryDate
            }, false);
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, "001", itemMap, cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request, netAmount, cancellationToken);

        await _saleMasterRepository.SaveChangesAsync(cancellationToken);

        var createdTxs = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        foreach (var tx in createdTxs)
        {
            await _fifoCostingService.AllocateFifoCostAsync(tx, cancellationToken);
        }

        await _itemTransactionRepository.SaveChangesAsync(cancellationToken);

        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, SaleUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _saleMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Sale voucher '{voucherNo}' not found.");

        var oldDate = master.VDate;
        var oldItemIds = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .Select(x => x.ItemId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToListAsync(cancellationToken);

        var isWanda = _currentTenant.HasVariablePackFeature;
        var grossAmount = request.Lines.Sum(x => isWanda
            ? (x.Qty * x.Rate)
            : ((x.Qty * x.Rate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))));
        var discountAmount = request.Lines.Sum(x => x.Discount);
        var netAmount = request.Lines.Sum(x => isWanda
            ? ((x.Qty * x.Rate) - x.Discount + ((x.SecQty ?? 0) * (x.Carriage ?? 0)))
            : (((x.Qty * x.Rate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))) - x.Discount + (x.Carriage ?? 0)));

        var validPayments = request.Payments?
            .Where(p => !string.IsNullOrWhiteSpace(p.AccountId) && p.Amount > 0)
            .ToList();
        var cashReceipt = validPayments != null && validPayments.Count > 0
            ? validPayments.Sum(p => p.Amount)
            : request.CashReceipt;
        var cashBack = validPayments != null && validPayments.Count > 0
            ? 0
            : request.CashBack;

        master.VDate = request.Date;
        master.VTime = PakistanTime.CurrentTime;
        master.AccountId = request.Account;
        master.Descr = request.Description;
        master.NarrationId = request.Narration;
        master.Amount = grossAmount;
        master.Discount = discountAmount;
        master.NetAmount = netAmount;
        master.CashReceipt = cashReceipt;
        master.CashBack = cashBack;

        await _saleMasterRepository.UpdateAsync(master, false);

        if (request.Payments != null)
        {
            var existingPayments = await _salePaymentRepository.GetAll()
                .Where(x => x.VType == VType && x.VNo == voucherNo)
                .ToListAsync(cancellationToken);
            if (existingPayments.Count > 0)
                await _salePaymentRepository.DeleteRangeAsync(existingPayments, false);

            if (validPayments != null && validPayments.Count > 0)
            {
                var pSeq = 1;
                foreach (var p in validPayments)
                {
                    await _salePaymentRepository.AddAsync(new SalePayment
                    {
                        VType = VType,
                        VNo = voucherNo,
                        Seq = pSeq++,
                        AccountId = p.AccountId,
                        Amount = p.Amount,
                        Reference = p.Reference,
                        CheckNum = p.CheckNum,
                        CheckDate = p.CheckDate
                    }, false);
                }
            }
        }

        var itemIds = request.Lines.Select(x => x.ItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var itemMap = await _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var line in request.Lines)
        {
            var item = itemMap.GetValueOrDefault(line.ItemId);
            if (item?.RequireImei == true && string.IsNullOrWhiteSpace(line.Imei))
            {
                throw new CustomException($"Item '{item.Title}' requires an IMEI / Serial number.");
            }

            var resolvedUnitId = item?.DefaultUnitId ?? item?.PrimaryUnitId;
            var resolvedSecUnitId = item?.SecondaryUnitId;

            var existing = await _saleRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq,
                    cancellationToken);

            var lineAmount = isWanda
                ? (line.Qty * line.Rate) - line.Discount + ((line.SecQty ?? 0) * (line.Carriage ?? 0))
                : ((line.Qty * line.Rate) + ((line.SecQty ?? 0) * (line.SecRate ?? 0))) - line.Discount + (line.Carriage ?? 0);

            if (existing is null)
            {
                await _saleRepository.AddAsync(new Sale
                {
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    ItemId = line.ItemId,
                    UnitId = resolvedUnitId,
                    Qty = line.Qty,
                    GrossRate = line.Rate,
                    Discount = line.Discount,
                    Carriage = line.Carriage ?? 0m,
                    Amount = lineAmount,
                    SecUnitId = resolvedSecUnitId,
                    SecQty = line.SecQty,
                    SecRate = line.SecRate,
                    QtyInPack = line.QtyInPack,
                    Packing = line.Packing,
                    Imei = line.Imei,
                    Imei2 = line.Imei2,
                    PtaStatus = line.PtaStatus,
                    ConditionNote = line.ConditionNote,
                    BatteryHealth = line.BatteryHealth,
                    WarrantyMonths = line.WarrantyMonths,
                    WarrantyExpiryDate = line.WarrantyExpiryDate
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.ItemId = line.ItemId;
                existing.UnitId = resolvedUnitId;
                existing.Qty = line.Qty;
                existing.GrossRate = line.Rate;
                existing.Discount = line.Discount;
                existing.Carriage = line.Carriage ?? 0m;
                existing.Amount = lineAmount;
                existing.SecUnitId = resolvedSecUnitId;
                existing.SecQty = line.SecQty;
                existing.SecRate = line.SecRate;
                existing.QtyInPack = line.QtyInPack;
                existing.Packing = line.Packing;
                existing.Imei = line.Imei;
                existing.Imei2 = line.Imei2;
                existing.PtaStatus = line.PtaStatus;
                existing.ConditionNote = line.ConditionNote;
                existing.BatteryHealth = line.BatteryHealth;
                existing.WarrantyMonths = line.WarrantyMonths;
                existing.WarrantyExpiryDate = line.WarrantyExpiryDate;

                await _saleRepository.UpdateAsync(existing, false);
            }
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, master.Counter, itemMap, cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request, netAmount, cancellationToken);

        await _saleMasterRepository.SaveChangesAsync(cancellationToken);

        var earliestDate = request.Date < oldDate ? request.Date : oldDate;
        var affectedItemIds = oldItemIds.Union(request.Lines.Select(x => x.ItemId)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct();

        foreach (var affectedItemId in affectedItemIds)
        {
            BackgroundJob.Schedule<IFifoCostingService>(x =>
                x.RebuildItemFifoAsync(_currentTenant.Id, affectedItemId!, earliestDate, CancellationToken.None),
                TimeSpan.FromSeconds(5));
        }
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var details = await _saleRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var masters = await _saleMasterRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var glEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        var itemTransactions = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var payments = await _salePaymentRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var oldDate = masters.FirstOrDefault()?.VDate ?? PakistanTime.Today;
        var affectedItemIds = itemTransactions.Select(x => x.ItemId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

        await _saleRepository.DeleteRangeAsync(details, true);
        await _saleMasterRepository.DeleteRangeAsync(masters, true);
        await _glRepository.DeleteRangeAsync(glEntries, true);
        await _itemTransactionRepository.DeleteRangeAsync(itemTransactions, true);
        if (payments.Count > 0)
            await _salePaymentRepository.DeleteRangeAsync(payments, true);

        await _saleMasterRepository.SaveChangesAsync(cancellationToken);

        foreach (var affectedItemId in affectedItemIds)
        {
            BackgroundJob.Schedule<IFifoCostingService>(x =>
                x.RebuildItemFifoAsync(_currentTenant.Id, affectedItemId!, oldDate, CancellationToken.None),
                TimeSpan.FromSeconds(5));
        }
    }

    public async Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        var line = await _saleRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is not null)
            await _saleRepository.DeleteAsync(line, true);

        var itemTransaction = await _itemTransactionRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (itemTransaction is not null)
            await _itemTransactionRepository.DeleteAsync(itemTransaction, false);

        var isWanda = _currentTenant.HasVariablePackFeature;
        var totals = await _saleRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .GroupBy(x => 1)
            .Select(g => new
            {
                Amount = g.Sum(x => (decimal?)(isWanda ? (x.Qty * x.GrossRate) : ((x.Qty * x.GrossRate) + ((x.SecQty ?? 0) * (x.SecRate ?? 0))))) ?? 0,
                Discount = g.Sum(x => (decimal?)x.Discount) ?? 0,
                NetAmount = g.Sum(x => (decimal?)x.Amount) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        var grossAmount = totals?.Amount ?? 0;
        var discountAmount = totals?.Discount ?? 0;
        var netAmount = totals?.NetAmount ?? 0;

        var master = await _saleMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is not null)
        {
            master.Amount = grossAmount;
            master.Discount = discountAmount;
            master.NetAmount = netAmount;
            await _saleMasterRepository.UpdateAsync(master, false);

            var glRequest = new SaleUpdateRequest
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

        var affectedItemId = itemTransaction?.ItemId;
        var affectedDate = master?.VDate ?? PakistanTime.Today;

        await _saleMasterRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(affectedItemId))
        {
            BackgroundJob.Schedule<IFifoCostingService>(x =>
                x.RebuildItemFifoAsync(_currentTenant.Id, affectedItemId, affectedDate, CancellationToken.None),
                TimeSpan.FromSeconds(5));
        }
    }

    private Task UpsertGlEntriesAsync(string voucherNo, SaleCreateRequest request, decimal netAmount, CancellationToken cancellationToken) =>
        UpsertGlEntriesAsync(voucherNo, request.Date, request.Account, request.Narration, request.Description, netAmount, request.Payments, request.CashReceipt, request.CashBack, cancellationToken);

    private Task UpsertGlEntriesAsync(string voucherNo, SaleUpdateRequest request, decimal netAmount, CancellationToken cancellationToken) =>
        UpsertGlEntriesAsync(voucherNo, request.Date, request.Account, request.Narration, request.Description, netAmount, request.Payments, request.CashReceipt, request.CashBack, cancellationToken);

    private async Task UpsertGlEntriesAsync(
        string voucherNo,
        DateOnly date,
        string account,
        string? narration,
        string? description,
        decimal netAmount,
        List<PaymentRecordRequest>? payments,
        decimal cashReceipt,
        decimal cashBack,
        CancellationToken cancellationToken)
    {
        var salesAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == VType)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(salesAccount))
            throw new NotFoundException("Default sales account is not configured.");

        var customerDetail = !string.IsNullOrWhiteSpace(account)
            ? await _customerDetailRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Id == account)
                .Select(x => new { x.Id, x.ParentCustomerAccountId })
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var isSub = !string.IsNullOrWhiteSpace(customerDetail?.ParentCustomerAccountId);
        var effectiveCustomerAccount = isSub ? customerDetail!.ParentCustomerAccountId! : account;
        var glDescription = description;
        if (isSub)
        {
            var subTitle = await _chartOfAccountRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Id == account)
                .Select(x => x.Title)
                .FirstOrDefaultAsync(cancellationToken) ?? account;
            var prefix = $"[Sub: {subTitle}]";
            glDescription = string.IsNullOrWhiteSpace(glDescription) ? prefix : $"{prefix} {glDescription}";
        }

        // VSeq 1: Dr Customer / Cr Sales (Full invoice amount)
        await UpsertGlEntryAsync(voucherNo, date, 1, effectiveCustomerAccount, salesAccount, netAmount, narration, glDescription, cancellationToken);

        var validPayments = payments?.Where(p => !string.IsNullOrWhiteSpace(p.AccountId) && p.Amount > 0).ToList();
        var glSeq = 2;

        if (validPayments != null && validPayments.Count > 0)
        {
            foreach (var p in validPayments)
            {
                var paymentRemarks = !string.IsNullOrWhiteSpace(p.Reference)
                    ? (string.IsNullOrWhiteSpace(glDescription) ? p.Reference : $"{glDescription} - Ref: {p.Reference}")
                    : glDescription;

                await UpsertGlEntryAsync(voucherNo, date, glSeq++, p.AccountId, effectiveCustomerAccount, p.Amount, narration, paymentRemarks, cancellationToken);
            }
        }
        else if (payments == null)
        {
            var netCash = cashReceipt - cashBack;
            if (netCash > 0)
            {
                var cashAccount = await _defaultAccountRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.Title == CashDefaultAccountTitle)
                    .Select(x => x.AccountId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(cashAccount))
                {
                    await UpsertGlEntryAsync(voucherNo, date, glSeq++, cashAccount, effectiveCustomerAccount, netCash, narration, glDescription, cancellationToken);
                }
            }
        }

        // Delete obsolete payment GL entries (e.g. from previous edit or reduced payment count)
        var obsoleteGlEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq >= glSeq)
            .ToListAsync(cancellationToken);

        if (obsoleteGlEntries.Count > 0)
        {
            await _glRepository.DeleteRangeAsync(obsoleteGlEntries, false);
        }
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
                VTime = PakistanTime.CurrentTime,
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
            gl.VTime = PakistanTime.CurrentTime;
            gl.DrAccountId = drAccount;
            gl.CrAccountId = crAccount;
            gl.Amount = amount;
            gl.NarrationId = narration;
            gl.Remarks = description;

            await _glRepository.UpdateAsync(gl, false);
        }
    }

    private async Task UpsertItemTransactionsAsync(
        string voucherNo,
        DateOnly date,
        string accountId,
        List<SaleLineRequest> lines,
        string? counter,
        Dictionary<string, ItemDetail> itemMap,
        CancellationToken cancellationToken)
    {
        var txsToProcess = new List<ItemTransaction>();
        foreach (var line in lines)
        {
            var item = itemMap.GetValueOrDefault(line.ItemId);
            var resolvedUnitId = item?.DefaultUnitId ?? item?.PrimaryUnitId;
            var resolvedSecUnitId = item?.SecondaryUnitId;

            var amount = (line.Qty * line.Rate) - line.Discount + ((line.SecQty ?? 0) * (line.SecRate ?? 0));
            var tx = await _itemTransactionRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq, cancellationToken);

            if (tx is null)
            {
                tx = new ItemTransaction
                {
                    VDate = date,
                    VTime = PakistanTime.CurrentTime,
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    TranType = "out",
                    AccountId = accountId,
                    ItemId = line.ItemId,
                    UnitId = resolvedUnitId,
                    QtyIn = 0,
                    QtyOut = line.Qty,
                    Rate = line.Rate,
                    Amount = amount,
                    Counter = counter,
                    SecUnitId = resolvedSecUnitId,
                    SecQtyIn = 0,
                    SecQtyOut = line.SecQty,
                    SecRate = line.SecRate
                };
                await _itemTransactionRepository.AddAsync(tx, false);
            }
            else
            {
                tx.DeletedOn = null;
                tx.DeletedBy = null;
                tx.VDate = date;
                tx.VTime = PakistanTime.CurrentTime;
                tx.TranType = "out";
                tx.AccountId = accountId;
                tx.ItemId = line.ItemId;
                tx.UnitId = resolvedUnitId;
                tx.QtyIn = 0;
                tx.QtyOut = line.Qty;
                tx.Rate = line.Rate;
                tx.Amount = amount;
                tx.Counter = counter;
                tx.SecUnitId = resolvedSecUnitId;
                tx.SecQtyIn = 0;
                tx.SecQtyOut = line.SecQty;
                tx.SecRate = line.SecRate;

                await _itemTransactionRepository.UpdateAsync(tx, false);
            }

            txsToProcess.Add(tx);
        }

        var lineSeqSet = lines.Select(x => x.Seq).ToHashSet();
        var staleEntries = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo && !lineSeqSet.Contains(x.Seq))
            .ToListAsync(cancellationToken);

        if (staleEntries.Count > 0)
            await _itemTransactionRepository.DeleteRangeAsync(staleEntries, false);

        await _balanceService.ProcessVoucherRunningBalancesAsync(_currentTenant.Id, VType, voucherNo, date, txsToProcess, cancellationToken);
    }

    public async Task<CustomerItemPriceHistoryResponse> GetCustomerItemHistoryAsync(string accountId, string itemId, CancellationToken cancellationToken)
    {
        var recentTransactions = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.TranType == "out"
                        && x.AccountId == accountId
                        && x.ItemId == itemId
                        && x.QtyOut > 0)
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.CreatedOn)
            .ThenByDescending(x => x.Id)
            .Select(x => new CustomerItemSaleRecord
            {
                VoucherNo = x.VNo,
                VType = x.VType,
                Date = x.VDate,
                Qty = x.QtyOut,
                Rate = x.Rate,
                SecQty = x.SecQtyOut,
                SecRate = x.SecRate,
                Amount = x.Amount
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        if (recentTransactions.Count == 0)
        {
            return new CustomerItemPriceHistoryResponse
            {
                HasHistory = false,
                RecentSales = recentTransactions
            };
        }

        var last = recentTransactions[0];
        return new CustomerItemPriceHistoryResponse
        {
            HasHistory = true,
            LastSaleDate = last.Date,
            LastRate = last.Rate,
            LastQty = last.Qty,
            LastSecRate = last.SecRate,
            LastVoucherNo = last.VoucherNo,
            LastVType = last.VType,
            RecentSales = recentTransactions
        };
    }
}

