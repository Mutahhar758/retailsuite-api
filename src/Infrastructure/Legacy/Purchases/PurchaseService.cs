using Hangfire;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Common;
using Retailer.Application.Legacy.Inventory;
using Retailer.Application.Common.Models;
using Retailer.Application.Legacy.Purchases;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Purchases;

internal class PurchaseService : IPurchaseService
{
    private const string VType = "PU";
    private const string CashDefaultAccountTitle = "Cash";

    private readonly IRepository<PurchaseMaster> _purchaseMasterRepository;
    private readonly IRepository<PurchaseDetail> _purchaseDetailRepository;
    private readonly IRepository<PurchasePayment> _purchasePaymentRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IItemTransactionBalanceService _balanceService;
    private readonly ICurrentTenant _currentTenant;

    public PurchaseService(
        IRepository<PurchaseMaster> purchaseMasterRepository,
        IRepository<PurchaseDetail> purchaseDetailRepository,
        IRepository<PurchasePayment> purchasePaymentRepository,
        IRepository<GlEntry> glRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<ItemDetail> itemRepository,
        IItemTransactionBalanceService balanceService,
        ICurrentTenant currentTenant)
    {
        _purchaseMasterRepository = purchaseMasterRepository;
        _purchaseDetailRepository = purchaseDetailRepository;
        _purchasePaymentRepository = purchasePaymentRepository;
        _glRepository = glRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
        _itemRepository = itemRepository;
        _balanceService = balanceService;
        _currentTenant = currentTenant;
    }

    [Obsolete("Unpaginated list. Use GetPagedListAsync instead; this will be removed.")]
    public async Task<List<PurchaseResponse>> GetListAsync(PurchaseListFilter filter, CancellationToken cancellationToken)
    {
        var query = _purchaseMasterRepository.GetAll()
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
            .Select(x => new PurchaseResponse
            {
                Date = x.Master.VDate,
                VoucherNo = x.Master.VNo,
                Account = x.Account != null ? x.Account.Title : x.Master.AccountId,
                Amount = x.Master.Amount ?? 0,
                SellerCnic = x.Master.SellerCnic,
                SellerContact = x.Master.SellerContact,
                CreatedBy = x.Master.CreatedBy,
                CreatedOn = x.Master.CreatedOn,
                LastModifiedBy = x.Master.LastModifiedBy,
                LastModifiedOn = x.Master.LastModifiedOn
            })
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaginationResponse<PurchaseResponse>> GetPagedListAsync(PurchasePagedListFilter filter, CancellationToken cancellationToken)
    {
        var query = _purchaseMasterRepository.GetAll()
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
            .Select(x => new PurchaseResponse
            {
                Date = x.Master.VDate,
                VoucherNo = x.Master.VNo,
                Account = x.Account != null ? x.Account.Title : x.Master.AccountId,
                Amount = x.Master.Amount ?? 0,
                SellerCnic = x.Master.SellerCnic,
                SellerContact = x.Master.SellerContact,
                CreatedBy = x.Master.CreatedBy,
                CreatedOn = x.Master.CreatedOn,
                LastModifiedBy = x.Master.LastModifiedBy,
                LastModifiedOn = x.Master.LastModifiedOn
            });

        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0
            ? PurchasePagedListFilter.DefaultPageSize
            : Math.Min(filter.PageSize, PurchasePagedListFilter.MaxPageSize);

        var totalCount = await projected.CountAsync(cancellationToken);

        var items = await projected
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginationResponse<PurchaseResponse>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<List<PurchaseLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        return await (
            from d in _purchaseDetailRepository.GetAll().AsNoTracking()
            join m in _purchaseMasterRepository.GetAll().AsNoTracking()
                on new { d.VType, d.VNo } equals new { m.VType, VNo = m.VNo }
            join i in _itemRepository.GetAll().AsNoTracking()
                on d.ItemId equals i.Id into itemJoin
            from i in itemJoin.DefaultIfEmpty()
            where d.VType == VType && d.VNo == voucherNo
            orderby d.Seq
            select new PurchaseLineResponse
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
                AddLess = d.AddLess,
                Amount = d.Amount ?? 0m,
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
                CashPaid = m.CashPaid,
                CashBack = m.CashBack ?? 0,
                SellerCnic = m.SellerCnic,
                SellerContact = m.SellerContact,
                CreatedBy = m.CreatedBy,
                CreatedOn = m.CreatedOn,
                LastModifiedBy = m.LastModifiedBy,
                LastModifiedOn = m.LastModifiedOn
            }).ToListAsync(cancellationToken);
    }

    public async Task<List<PaymentRecordResponse>> GetPaymentsAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var payments = await _purchasePaymentRepository.GetAll()
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
            var master = await _purchaseMasterRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == VType && x.VNo == voucherNo)
                .Select(x => new { x.CashPaid, x.CashBack })
                .FirstOrDefaultAsync(cancellationToken);

            var netCash = (master?.CashPaid ?? 0) - (master?.CashBack ?? 0);
            if (netCash > 0)
            {
                var glPayments = await _glRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq >= 2)
                    .OrderBy(x => x.VSeq)
                    .ToListAsync(cancellationToken);

                if (glPayments.Count > 0)
                {
                    var accountIds = glPayments.Select(g => g.CrAccountId).Distinct().ToList();
                    var titles = await _chartOfAccountRepository.GetAll()
                        .AsNoTracking()
                        .Where(a => accountIds.Contains(a.Id))
                        .ToDictionaryAsync(a => a.Id, a => a.Title, cancellationToken);

                    return glPayments.Select((g, idx) => new PaymentRecordResponse
                    {
                        Seq = idx + 1,
                        AccountId = g.CrAccountId,
                        AccountTitle = titles.GetValueOrDefault(g.CrAccountId, g.CrAccountId),
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

    public async Task<string> CreateAsync(PurchaseCreateRequest request, CancellationToken cancellationToken)
    {
        var maxVoucherNo = await _purchaseMasterRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == VType)
            .MaxAsync(x => (string?)x.VNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        var isWanda = _currentTenant.HasVariablePackFeature;
        var totalAmount = request.Lines.Sum(x => isWanda
            ? ((x.Qty * x.Rate) + x.AddLess)
            : ((x.Qty * x.Rate) + x.AddLess + ((x.SecQty ?? 0) * (x.SecRate ?? 0))));

        var validPayments = request.Payments?
            .Where(p => !string.IsNullOrWhiteSpace(p.AccountId) && p.Amount > 0)
            .ToList();
        var cashPaid = validPayments != null && validPayments.Count > 0
            ? validPayments.Sum(p => p.Amount)
            : request.CashPaid;
        var cashBack = validPayments != null && validPayments.Count > 0
            ? 0
            : request.CashBack;

        var master = new PurchaseMaster
        {
            VDate = request.Date,
            VTime = TimeOnly.FromDateTime(DateTime.Now),
            VType = VType,
            VNo = voucherNo,
            AccountId = request.Account,
            Descr = request.Description,
            NarrationId = request.Narration,
            Amount = totalAmount,
            CashPaid = cashPaid,
            CashBack = cashBack,
            SellerCnic = request.SellerCnic,
            SellerContact = request.SellerContact,
            Counter = "001"
        };

        await _purchaseMasterRepository.AddAsync(master, false);

        if (validPayments != null && validPayments.Count > 0)
        {
            var pSeq = 1;
            foreach (var p in validPayments)
            {
                await _purchasePaymentRepository.AddAsync(new PurchasePayment
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
                ? (line.Qty * line.Rate) + line.AddLess
                : (line.Qty * line.Rate) + line.AddLess + ((line.SecQty ?? 0) * (line.SecRate ?? 0));

            await _purchaseDetailRepository.AddAsync(new PurchaseDetail
            {
                VType = VType,
                VNo = voucherNo,
                Seq = line.Seq,
                ItemId = line.ItemId,
                UnitId = resolvedUnitId,
                Qty = line.Qty,
                Rate = line.Rate,
                AddLess = line.AddLess,
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
                BatteryHealth = line.BatteryHealth
            }, false);
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, "001", itemMap, cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request, totalAmount, cancellationToken);

        await _purchaseMasterRepository.SaveChangesAsync(cancellationToken);

        foreach (var line in request.Lines.Where(l => !string.IsNullOrWhiteSpace(l.ItemId)).DistinctBy(l => l.ItemId))
        {
            BackgroundJob.Schedule<IFifoCostingService>(x =>
                x.RebuildItemFifoAsync(_currentTenant.Id, line.ItemId, request.Date, CancellationToken.None),
                TimeSpan.FromSeconds(5));
        }

        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, PurchaseUpdateRequest request, CancellationToken cancellationToken)
    {
        var master = await _purchaseMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is null)
            throw new NotFoundException($"Purchase voucher '{voucherNo}' not found.");

        var oldDate = master.VDate;
        var oldItemIds = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .Select(x => x.ItemId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToListAsync(cancellationToken);

        var isWanda = _currentTenant.HasVariablePackFeature;
        var totalAmount = request.Lines.Sum(x => isWanda
            ? ((x.Qty * x.Rate) + x.AddLess)
            : ((x.Qty * x.Rate) + x.AddLess + ((x.SecQty ?? 0) * (x.SecRate ?? 0))));

        var validPayments = request.Payments?
            .Where(p => !string.IsNullOrWhiteSpace(p.AccountId) && p.Amount > 0)
            .ToList();
        var cashPaid = validPayments != null && validPayments.Count > 0
            ? validPayments.Sum(p => p.Amount)
            : request.CashPaid;
        var cashBack = validPayments != null && validPayments.Count > 0
            ? 0
            : request.CashBack;

        master.VDate = request.Date;
        master.VTime = TimeOnly.FromDateTime(DateTime.Now);
        master.AccountId = request.Account;
        master.Descr = request.Description;
        master.NarrationId = request.Narration;
        master.Amount = totalAmount;
        master.CashPaid = cashPaid;
        master.CashBack = cashBack;
        master.SellerCnic = request.SellerCnic;
        master.SellerContact = request.SellerContact;

        await _purchaseMasterRepository.UpdateAsync(master, false);

        if (request.Payments != null)
        {
            var existingPayments = await _purchasePaymentRepository.GetAll()
                .Where(x => x.VType == VType && x.VNo == voucherNo)
                .ToListAsync(cancellationToken);
            if (existingPayments.Count > 0)
                await _purchasePaymentRepository.DeleteRangeAsync(existingPayments, false);

            if (validPayments != null && validPayments.Count > 0)
            {
                var pSeq = 1;
                foreach (var p in validPayments)
                {
                    await _purchasePaymentRepository.AddAsync(new PurchasePayment
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

            var existing = await _purchaseDetailRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(
                    x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq,
                    cancellationToken);

            var lineAmount = isWanda
                ? (line.Qty * line.Rate) + line.AddLess
                : (line.Qty * line.Rate) + line.AddLess + ((line.SecQty ?? 0) * (line.SecRate ?? 0));

            if (existing is null)
            {
                await _purchaseDetailRepository.AddAsync(new PurchaseDetail
                {
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    ItemId = line.ItemId,
                    UnitId = resolvedUnitId,
                    Qty = line.Qty,
                    Rate = line.Rate,
                    AddLess = line.AddLess,
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
                    BatteryHealth = line.BatteryHealth
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.ItemId = line.ItemId;
                existing.UnitId = resolvedUnitId;
                existing.Qty = line.Qty;
                existing.Rate = line.Rate;
                existing.AddLess = line.AddLess;
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

                await _purchaseDetailRepository.UpdateAsync(existing, false);
            }
        }

        await UpsertItemTransactionsAsync(voucherNo, request.Date, request.Account, request.Lines, master.Counter, itemMap, cancellationToken);
        await UpsertGlEntriesAsync(voucherNo, request, totalAmount, cancellationToken);

        await _purchaseMasterRepository.SaveChangesAsync(cancellationToken);

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
        var details = await _purchaseDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var masters = await _purchaseMasterRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var glEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        var itemTransactions = await _itemTransactionRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var payments = await _purchasePaymentRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .ToListAsync(cancellationToken);

        var oldDate = masters.FirstOrDefault()?.VDate ?? DateOnly.FromDateTime(DateTime.Today);
        var affectedItemIds = itemTransactions.Select(x => x.ItemId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

        await _purchaseDetailRepository.DeleteRangeAsync(details, true);
        await _purchaseMasterRepository.DeleteRangeAsync(masters, true);
        await _glRepository.DeleteRangeAsync(glEntries, true);
        await _itemTransactionRepository.DeleteRangeAsync(itemTransactions, true);
        if (payments.Count > 0)
            await _purchasePaymentRepository.DeleteRangeAsync(payments, true);

        await _purchaseMasterRepository.SaveChangesAsync(cancellationToken);

        foreach (var affectedItemId in affectedItemIds)
        {
            BackgroundJob.Schedule<IFifoCostingService>(x =>
                x.RebuildItemFifoAsync(_currentTenant.Id, affectedItemId!, oldDate, CancellationToken.None),
                TimeSpan.FromSeconds(5));
        }
    }

    public async Task DeleteLineAsync(string voucherNo, int seq, CancellationToken cancellationToken)
    {
        var line = await _purchaseDetailRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is not null)
            await _purchaseDetailRepository.DeleteAsync(line, true);

        var itemTransaction = await _itemTransactionRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == seq, cancellationToken);

        if (itemTransaction is not null)
            await _itemTransactionRepository.DeleteAsync(itemTransaction, false);

        var amount = await _purchaseDetailRepository.GetAll()
            .Where(x => x.VType == VType && x.VNo == voucherNo)
            .SumAsync(x => (decimal?)((x.Qty * x.Rate) + x.AddLess + ((x.SecQty ?? 0) * (x.SecRate ?? 0))), cancellationToken) ?? 0;

        var master = await _purchaseMasterRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo, cancellationToken);

        if (master is not null)
        {
            master.Amount = amount;
            await _purchaseMasterRepository.UpdateAsync(master, false);

            var glRequest = new PurchaseUpdateRequest
            {
                Date = master.VDate,
                Account = master.AccountId!,
                Description = master.Descr,
                Narration = master.NarrationId,
                CashPaid = master.CashPaid,
                CashBack = master.CashBack ?? 0
            };

            await UpsertGlEntriesAsync(voucherNo, glRequest, amount, cancellationToken);
        }

        var affectedItemId = itemTransaction?.ItemId;
        var affectedDate = master?.VDate ?? DateOnly.FromDateTime(DateTime.Today);

        await _purchaseMasterRepository.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(affectedItemId))
        {
            BackgroundJob.Schedule<IFifoCostingService>(x =>
                x.RebuildItemFifoAsync(_currentTenant.Id, affectedItemId, affectedDate, CancellationToken.None),
                TimeSpan.FromSeconds(5));
        }
    }

    private async Task UpsertItemTransactionsAsync(
        string voucherNo,
        DateOnly date,
        string accountId,
        List<PurchaseLineRequest> lines,
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

            var amount = (line.Qty * line.Rate) + line.AddLess + ((line.SecQty ?? 0) * (line.SecRate ?? 0));
            var tx = await _itemTransactionRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(x => x.VType == VType && x.VNo == voucherNo && x.Seq == line.Seq, cancellationToken);

            if (tx is null)
            {
                tx = new ItemTransaction
                {
                    VDate = date,
                    VTime = TimeOnly.FromDateTime(DateTime.Now),
                    VType = VType,
                    VNo = voucherNo,
                    Seq = line.Seq,
                    TranType = "in",
                    AccountId = accountId,
                    ItemId = line.ItemId,
                    UnitId = resolvedUnitId,
                    QtyIn = line.Qty,
                    QtyOut = 0,
                    RemainingQty = line.Qty,
                    Rate = line.Rate,
                    Amount = amount,
                    Counter = counter,
                    SecUnitId = resolvedSecUnitId,
                    SecQtyIn = line.SecQty,
                    SecQtyOut = 0,
                    SecRate = line.SecRate
                };
                await _itemTransactionRepository.AddAsync(tx, false);
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
                tx.UnitId = resolvedUnitId;
                tx.QtyIn = line.Qty;
                tx.QtyOut = 0;
                tx.RemainingQty = line.Qty;
                tx.Rate = line.Rate;
                tx.Amount = amount;
                tx.Counter = counter;
                tx.SecUnitId = resolvedSecUnitId;
                tx.SecQtyIn = line.SecQty;
                tx.SecQtyOut = 0;
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

    private Task UpsertGlEntriesAsync(string voucherNo, PurchaseCreateRequest request, decimal totalAmount, CancellationToken cancellationToken) =>
        UpsertGlEntriesAsync(voucherNo, request.Date, request.Account, request.Narration, request.Description, totalAmount, request.Payments, request.CashPaid, request.CashBack, cancellationToken);

    private Task UpsertGlEntriesAsync(string voucherNo, PurchaseUpdateRequest request, decimal totalAmount, CancellationToken cancellationToken) =>
        UpsertGlEntriesAsync(voucherNo, request.Date, request.Account, request.Narration, request.Description, totalAmount, request.Payments, request.CashPaid, request.CashBack, cancellationToken);

    private async Task UpsertGlEntriesAsync(
        string voucherNo,
        DateOnly date,
        string account,
        string? narration,
        string? description,
        decimal totalAmount,
        List<PaymentRecordRequest>? payments,
        decimal cashPaid,
        decimal cashBack,
        CancellationToken cancellationToken)
    {
        var purchaseAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == VType)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(purchaseAccount))
            throw new NotFoundException("Default purchase account is not configured.");

        // VSeq 1: Dr Purchases / Cr Supplier (Full invoice amount)
        await UpsertGlEntryAsync(voucherNo, date, 1, purchaseAccount, account, totalAmount, narration, description, cancellationToken);

        var validPayments = payments?.Where(p => !string.IsNullOrWhiteSpace(p.AccountId) && p.Amount > 0).ToList();
        var glSeq = 2;

        if (validPayments != null && validPayments.Count > 0)
        {
            foreach (var p in validPayments)
            {
                var paymentRemarks = !string.IsNullOrWhiteSpace(p.Reference)
                    ? (string.IsNullOrWhiteSpace(description) ? p.Reference : $"{description} - Ref: {p.Reference}")
                    : description;

                // Purchase Payment: Dr Supplier / Cr PaymentAccount
                await UpsertGlEntryAsync(voucherNo, date, glSeq++, account, p.AccountId, p.Amount, narration, paymentRemarks, cancellationToken);
            }
        }
        else if (payments == null)
        {
            var netCash = cashPaid - cashBack;
            if (netCash > 0)
            {
                var cashAccount = await _defaultAccountRepository.GetAll()
                    .AsNoTracking()
                    .Where(x => x.Title == CashDefaultAccountTitle)
                    .Select(x => x.AccountId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(cashAccount))
                {
                    await UpsertGlEntryAsync(voucherNo, date, glSeq++, account, cashAccount, netCash, narration, description, cancellationToken);
                }
            }
        }

        // Delete obsolete payment GL entries
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
