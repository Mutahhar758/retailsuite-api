using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Legacy.Common;
using Retailer.Application.Legacy.DailyStockClosings;
using Retailer.Application.Legacy.Purchases;
using Retailer.Application.Legacy.Sales;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Legacy;
using Retailer.Application.Common.Persistence;

namespace Retailer.Infrastructure.Legacy.DailyStockClosings;

public class DailyStockClosingService : IDailyStockClosingService
{
    private const string ClosingPrefix = "[DC:";
    private const string CashDefaultAccountTitle = "Cash";

    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<PurchaseMaster> _purchaseMasterRepository;
    private readonly IRepository<PurchaseDetail> _purchaseDetailRepository;
    private readonly IRepository<PurchasePayment> _purchasePaymentRepository;
    private readonly IRepository<SaleMaster> _saleMasterRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<SalePayment> _salePaymentRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<CustomerDetail> _customerDetailRepository;
    private readonly IRepository<SupplierDetail> _supplierDetailRepository;
    private readonly IPurchaseService _purchaseService;
    private readonly ISaleService _saleService;

    public DailyStockClosingService(
        IRepository<ItemDetail> itemRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<PurchaseMaster> purchaseMasterRepository,
        IRepository<PurchaseDetail> purchaseDetailRepository,
        IRepository<PurchasePayment> purchasePaymentRepository,
        IRepository<SaleMaster> saleMasterRepository,
        IRepository<Sale> saleRepository,
        IRepository<SalePayment> salePaymentRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<CustomerDetail> customerDetailRepository,
        IRepository<SupplierDetail> supplierDetailRepository,
        IPurchaseService purchaseService,
        ISaleService saleService)
    {
        _itemRepository = itemRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _purchaseMasterRepository = purchaseMasterRepository;
        _purchaseDetailRepository = purchaseDetailRepository;
        _purchasePaymentRepository = purchasePaymentRepository;
        _saleMasterRepository = saleMasterRepository;
        _saleRepository = saleRepository;
        _salePaymentRepository = salePaymentRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
        _customerDetailRepository = customerDetailRepository;
        _supplierDetailRepository = supplierDetailRepository;
        _purchaseService = purchaseService;
        _saleService = saleService;
    }

    private static string BuildTagPrefix(DateOnly date) => $"[DC:{date:yyyy-MM-dd}";

    private static string BuildDescription(DateOnly date, string reconciliationMode, string? remarks)
    {
        var mode = string.Equals(reconciliationMode, "ProRate", StringComparison.OrdinalIgnoreCase)
            ? "ProRate"
            : "DiscountAddLess";
        var tag = $"[DC:{date:yyyy-MM-dd}|MODE:{mode}]";
        return string.IsNullOrWhiteSpace(remarks) ? tag : $"{tag} {remarks.Trim()}";
    }

    private static (string Mode, string? CleanRemarks) ParseDescription(string? descr)
    {
        if (string.IsNullOrWhiteSpace(descr) || !descr.StartsWith(ClosingPrefix, StringComparison.OrdinalIgnoreCase))
            return ("DiscountAddLess", descr);

        var closeBracket = descr.IndexOf(']');
        if (closeBracket < 0)
            return ("DiscountAddLess", descr);

        var header = descr[..closeBracket];
        var mode = header.Contains("MODE:ProRate", StringComparison.OrdinalIgnoreCase)
            ? "ProRate"
            : "DiscountAddLess";

        var remainder = descr[(closeBracket + 1)..].Trim();
        return (mode, string.IsNullOrWhiteSpace(remainder) ? null : remainder);
    }

    public async Task<List<DailyStockClosingListItemResponse>> GetListAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var puQuery = _purchaseMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PU" && x.Descr != null && x.Descr.StartsWith(ClosingPrefix));

        var slQuery = _saleMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "SL" && x.Descr != null && x.Descr.StartsWith(ClosingPrefix));

        if (fromDate.HasValue)
        {
            puQuery = puQuery.Where(x => x.VDate >= fromDate.Value);
            slQuery = slQuery.Where(x => x.VDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            puQuery = puQuery.Where(x => x.VDate <= toDate.Value);
            slQuery = slQuery.Where(x => x.VDate <= toDate.Value);
        }

        var purchases = await puQuery.ToListAsync(cancellationToken);
        var sales = await slQuery.ToListAsync(cancellationToken);

        var allDates = purchases.Select(x => x.VDate)
            .Union(sales.Select(x => x.VDate))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var result = new List<DailyStockClosingListItemResponse>(allDates.Count);
        foreach (var date in allDates)
        {
            var pu = purchases.Where(x => x.VDate == date).OrderByDescending(x => x.VNo).FirstOrDefault();
            var sl = sales.Where(x => x.VDate == date).OrderByDescending(x => x.VNo).FirstOrDefault();

            var (_, remarks) = ParseDescription(sl?.Descr ?? pu?.Descr);

            result.Add(new DailyStockClosingListItemResponse
            {
                ClosingKey = date.ToString("yyyy-MM-dd"),
                Date = date,
                PurchaseVoucherNo = pu?.VNo,
                PurchaseAmount = pu?.Amount ?? 0m,
                SaleVoucherNo = sl?.VNo,
                SaleAmount = sl?.NetAmount ?? sl?.Amount ?? 0m,
                Remarks = remarks,
                CreatedBy = sl?.CreatedBy ?? pu?.CreatedBy,
                CreatedOn = sl?.CreatedOn ?? pu?.CreatedOn ?? DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<DailyStockClosingSheetResponse> GetSheetAsync(
        DateOnly date,
        string? categoryCode,
        CancellationToken cancellationToken)
    {
        var tagPrefix = BuildTagPrefix(date);

        var existingPu = await _purchaseMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PU" && x.VDate == date && x.Descr != null && x.Descr.StartsWith(tagPrefix))
            .OrderByDescending(x => x.VNo)
            .FirstOrDefaultAsync(cancellationToken);

        var existingSl = await _saleMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "SL" && x.VDate == date && x.Descr != null && x.Descr.StartsWith(tagPrefix))
            .OrderByDescending(x => x.VNo)
            .FirstOrDefaultAsync(cancellationToken);

        var puDetails = existingPu != null
            ? await _purchaseDetailRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == "PU" && x.VNo == existingPu.VNo)
                .ToListAsync(cancellationToken)
            : [];

        var slDetails = existingSl != null
            ? await _saleRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == "SL" && x.VNo == existingSl.VNo)
                .ToListAsync(cancellationToken)
            : [];

        var defaultCashAccountId = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == CashDefaultAccountTitle)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        var defaultCustomerAccountId = existingSl?.AccountId
            ?? await _customerDetailRepository.GetAll()
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

        var defaultVendorAccountId = existingPu?.AccountId
            ?? await _supplierDetailRepository.GetAll()
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

        var creditCustomers = new List<DailyStockClosingCreditLineDto>();
        if (existingSl != null)
        {
            var slPayments = await _salePaymentRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == "SL" && x.VNo == existingSl.VNo)
                .OrderBy(x => x.Seq)
                .ToListAsync(cancellationToken);

            var nonCashPayments = slPayments
                .Where(p => !string.Equals(p.AccountId, defaultCashAccountId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (nonCashPayments.Count > 0)
            {
                var accIds = nonCashPayments.Select(p => p.AccountId).Distinct().ToList();
                var titles = await _chartOfAccountRepository.GetAll()
                    .AsNoTracking()
                    .Where(a => accIds.Contains(a.Id))
                    .ToDictionaryAsync(a => a.Id, a => a.Title, cancellationToken);

                var seq = 1;
                foreach (var p in nonCashPayments)
                {
                    creditCustomers.Add(new DailyStockClosingCreditLineDto
                    {
                        Seq = seq++,
                        AccountId = p.AccountId,
                        AccountTitle = titles.GetValueOrDefault(p.AccountId, p.AccountId),
                        Amount = p.Amount,
                        Remarks = p.Reference
                    });
                }
            }
        }

        var creditSuppliers = new List<DailyStockClosingCreditLineDto>();
        if (existingPu != null)
        {
            var puPayments = await _purchasePaymentRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType == "PU" && x.VNo == existingPu.VNo)
                .OrderBy(x => x.Seq)
                .ToListAsync(cancellationToken);

            var nonCashPayments = puPayments
                .Where(p => !string.Equals(p.AccountId, defaultCashAccountId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (nonCashPayments.Count > 0)
            {
                var accIds = nonCashPayments.Select(p => p.AccountId).Distinct().ToList();
                var titles = await _chartOfAccountRepository.GetAll()
                    .AsNoTracking()
                    .Where(a => accIds.Contains(a.Id))
                    .ToDictionaryAsync(a => a.Id, a => a.Title, cancellationToken);

                var seq = 1;
                foreach (var p in nonCashPayments)
                {
                    creditSuppliers.Add(new DailyStockClosingCreditLineDto
                    {
                        Seq = seq++,
                        AccountId = p.AccountId,
                        AccountTitle = titles.GetValueOrDefault(p.AccountId, p.AccountId),
                        Amount = p.Amount,
                        Remarks = p.Reference
                    });
                }
            }
        }

        var itemsQuery = _itemRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemType != ItemType.Service);

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            itemsQuery = itemsQuery.Where(x => x.ItemCategoryId == categoryCode);
        }

        var items = await itemsQuery
            .Select(x => new
            {
                x.Id,
                x.ItemKey,
                x.Barcode,
                x.Title,
                x.ItemCategoryId,
                CategoryTitle = x.ItemCategory != null ? x.ItemCategory.Title : null,
                UnitId = x.PrimaryUnitId ?? x.DefaultUnitId,
                UnitTitle = x.PrimaryUnit != null
                    ? (x.PrimaryUnit.ShortTitle ?? x.PrimaryUnit.Title)
                    : (x.DefaultUnit != null ? (x.DefaultUnit.ShortTitle ?? x.DefaultUnit.Title) : x.DefaultUnitId),
                x.PriRate
            })
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var stockBalances = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate <= date && !string.IsNullOrWhiteSpace(x.ItemId))
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                Balance = g.Sum(x => x.QtyIn - x.QtyOut)
            })
            .ToDictionaryAsync(x => x.ItemId, x => x.Balance, cancellationToken);

        var lastPurchaseRates = await _purchaseDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId) && x.Rate > 0)
            .GroupBy(x => x.ItemId!)
            .Select(g => new
            {
                ItemId = g.Key,
                LastRate = g.OrderByDescending(x => x.CreatedOn).Select(x => x.Rate).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.ItemId, x => x.LastRate, cancellationToken);

        var puByItem = puDetails
            .GroupBy(x => x.ItemId ?? string.Empty)
            .ToDictionary(g => g.Key, g => new
            {
                Qty = g.Sum(x => x.Qty),
                Rate = g.First().Rate
            });

        var slByItem = slDetails
            .GroupBy(x => x.ItemId ?? string.Empty)
            .ToDictionary(g => g.Key, g => new
            {
                Qty = g.Sum(x => x.Qty),
                Rate = g.First().GrossRate
            });

        var sheetLines = new List<DailyStockClosingItemLineDto>(items.Count);
        foreach (var item in items)
        {
            var currentBal = stockBalances.GetValueOrDefault(item.Id, 0m);
            var existingPuQty = puByItem.TryGetValue(item.Id, out var puInfo) ? puInfo.Qty : 0m;
            var existingSlQty = slByItem.TryGetValue(item.Id, out var slInfo) ? slInfo.Qty : 0m;

            var openingQty = currentBal - existingPuQty + existingSlQty;
            var closingQty = openingQty + existingPuQty - existingSlQty;

            var purchaseRate = puInfo != null && puInfo.Rate > 0
                ? puInfo.Rate
                : lastPurchaseRates.GetValueOrDefault(item.Id, 0m);

            var saleRate = slInfo != null && slInfo.Rate > 0
                ? slInfo.Rate
                : item.PriRate;

            sheetLines.Add(new DailyStockClosingItemLineDto
            {
                ItemId = item.Id,
                ItemKey = item.ItemKey,
                Barcode = item.Barcode,
                Title = item.Title,
                CategoryCode = item.ItemCategoryId ?? string.Empty,
                CategoryTitle = item.CategoryTitle,
                UnitId = item.UnitId,
                UnitTitle = item.UnitTitle,
                OpeningQty = openingQty,
                ClosingQty = closingQty,
                PurchasedQty = existingPuQty,
                SoldQty = existingSlQty,
                PurchaseRate = purchaseRate,
                SaleRate = saleRate
            });
        }

        var (mode, cleanRemarks) = ParseDescription(existingSl?.Descr ?? existingPu?.Descr);

        return new DailyStockClosingSheetResponse
        {
            Date = date,
            IsExisting = existingPu != null || existingSl != null,
            PurchaseVoucherNo = existingPu?.VNo,
            SaleVoucherNo = existingSl?.VNo,
            CustomerAccountId = defaultCustomerAccountId,
            VendorAccountId = defaultVendorAccountId,
            CashAccountId = defaultCashAccountId,
            ReconciliationMode = mode,
            EnteredPurchaseAmount = existingPu?.Amount,
            EnteredSaleAmount = existingSl?.NetAmount ?? existingSl?.Amount,
            Remarks = cleanRemarks,
            Items = sheetLines,
            CreditCustomers = creditCustomers,
            CreditSuppliers = creditSuppliers
        };
    }

    public async Task<DailyStockClosingSaveResponse> SaveAsync(
        DailyStockClosingSaveRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerAccountId))
            throw new BadRequestException("Default Customer Account is required.");

        if (string.IsNullOrWhiteSpace(request.VendorAccountId))
            throw new BadRequestException("Default Vendor Account is required.");

        var tagPrefix = BuildTagPrefix(request.Date);
        var description = BuildDescription(request.Date, request.ReconciliationMode, request.Remarks);
        var isProRate = string.Equals(request.ReconciliationMode, "ProRate", StringComparison.OrdinalIgnoreCase);

        var existingPu = await _purchaseMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PU" && x.VDate == request.Date && x.Descr != null && x.Descr.StartsWith(tagPrefix))
            .OrderByDescending(x => x.VNo)
            .FirstOrDefaultAsync(cancellationToken);

        var existingSl = await _saleMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "SL" && x.VDate == request.Date && x.Descr != null && x.Descr.StartsWith(tagPrefix))
            .OrderByDescending(x => x.VNo)
            .FirstOrDefaultAsync(cancellationToken);

        var cashAccountId = !string.IsNullOrWhiteSpace(request.CashAccountId)
            ? request.CashAccountId
            : await _defaultAccountRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.Title == CashDefaultAccountTitle)
                .Select(x => x.AccountId)
                .FirstOrDefaultAsync(cancellationToken);

        // 1. Prepare Purchase Lines
        var rawPuLines = request.Lines
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId) && x.PurchasedQty > 0)
            .ToList();

        var calcPuGross = rawPuLines.Sum(x => Math.Round(x.PurchasedQty * x.PurchaseRate, 2));
        var targetPuTotal = request.EnteredPurchaseAmount.HasValue && request.EnteredPurchaseAmount.Value >= 0
            ? Math.Round(request.EnteredPurchaseAmount.Value, 2)
            : calcPuGross;

        var validCreditSuppliers = (request.CreditSuppliers ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.AccountId) && x.Amount > 0)
            .ToList();

        var totalSupplierCredit = validCreditSuppliers.Sum(x => Math.Round(x.Amount, 2));
        if (rawPuLines.Count > 0 && totalSupplierCredit > targetPuTotal)
        {
            throw new BadRequestException("Total Credit Supplier amount cannot exceed Total Purchase Amount.");
        }

        var cashPaid = rawPuLines.Count > 0 ? Math.Max(0m, targetPuTotal - totalSupplierCredit) : 0m;
        string? savedPuVoucherNo = existingPu?.VNo;

        if (rawPuLines.Count > 0)
        {
            var puLineRequests = BuildReconciledPurchaseLines(rawPuLines, calcPuGross, targetPuTotal, isProRate);
            var puPayments = BuildPaymentRecords(cashAccountId, cashPaid, "Cash Paid", validCreditSuppliers, "Daily Closing Credit Purchase");

            if (existingPu != null)
            {
                await _purchaseService.UpdateAsync(existingPu.VNo, new PurchaseUpdateRequest
                {
                    Date = request.Date,
                    Account = request.VendorAccountId,
                    Description = description,
                    CashPaid = cashPaid,
                    CashBack = 0m,
                    Lines = puLineRequests,
                    Payments = puPayments
                }, cancellationToken);
                savedPuVoucherNo = existingPu.VNo;
            }
            else
            {
                savedPuVoucherNo = await _purchaseService.CreateAsync(new PurchaseCreateRequest
                {
                    Date = request.Date,
                    Account = request.VendorAccountId,
                    Description = description,
                    CashPaid = cashPaid,
                    CashBack = 0m,
                    Lines = puLineRequests,
                    Payments = puPayments
                }, cancellationToken);
            }
        }
        else if (existingPu != null)
        {
            await _purchaseService.DeleteAsync(existingPu.VNo, cancellationToken);
            savedPuVoucherNo = null;
            targetPuTotal = 0m;
        }
        else
        {
            targetPuTotal = 0m;
        }

        // 2. Prepare Sale Lines
        var rawSlLines = request.Lines
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId) && x.SoldQty > 0)
            .ToList();

        var calcSlGross = rawSlLines.Sum(x => Math.Round(x.SoldQty * x.SaleRate, 2));
        var targetSlTotal = request.EnteredSaleAmount.HasValue && request.EnteredSaleAmount.Value >= 0
            ? Math.Round(request.EnteredSaleAmount.Value, 2)
            : calcSlGross;

        var validCreditCustomers = (request.CreditCustomers ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.AccountId) && x.Amount > 0)
            .ToList();

        var totalCustomerCredit = validCreditCustomers.Sum(x => Math.Round(x.Amount, 2));
        if (rawSlLines.Count > 0 && totalCustomerCredit > targetSlTotal)
        {
            throw new BadRequestException("Total Credit Customer amount cannot exceed Total Sale Amount.");
        }

        var cashReceived = rawSlLines.Count > 0 ? Math.Max(0m, targetSlTotal - totalCustomerCredit) : 0m;
        string? savedSlVoucherNo = existingSl?.VNo;

        if (rawSlLines.Count > 0)
        {
            var slLineRequests = BuildReconciledSaleLines(rawSlLines, calcSlGross, targetSlTotal, isProRate);
            var slPayments = BuildPaymentRecords(cashAccountId, cashReceived, "Cash Received", validCreditCustomers, "Daily Closing Credit Sale");

            if (existingSl != null)
            {
                await _saleService.UpdateAsync(existingSl.VNo, new SaleUpdateRequest
                {
                    Date = request.Date,
                    Account = request.CustomerAccountId,
                    Description = description,
                    CashReceipt = cashReceived,
                    CashBack = 0m,
                    Lines = slLineRequests,
                    Payments = slPayments
                }, cancellationToken);
                savedSlVoucherNo = existingSl.VNo;
            }
            else
            {
                savedSlVoucherNo = await _saleService.CreateAsync(new SaleCreateRequest
                {
                    Date = request.Date,
                    Account = request.CustomerAccountId,
                    Description = description,
                    CashReceipt = cashReceived,
                    CashBack = 0m,
                    Lines = slLineRequests,
                    Payments = slPayments
                }, cancellationToken);
            }
        }
        else if (existingSl != null)
        {
            await _saleService.DeleteAsync(existingSl.VNo, cancellationToken);
            savedSlVoucherNo = null;
            targetSlTotal = 0m;
        }
        else
        {
            targetSlTotal = 0m;
        }

        return new DailyStockClosingSaveResponse
        {
            Date = request.Date,
            PurchaseVoucherNo = savedPuVoucherNo,
            SaleVoucherNo = savedSlVoucherNo,
            TotalPurchaseAmount = targetPuTotal,
            TotalSaleAmount = targetSlTotal,
            CashPaid = cashPaid,
            CashReceived = cashReceived
        };
    }

    public async Task DeleteAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var tagPrefix = BuildTagPrefix(date);

        var existingSl = await _saleMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "SL" && x.VDate == date && x.Descr != null && x.Descr.StartsWith(tagPrefix))
            .ToListAsync(cancellationToken);

        foreach (var sl in existingSl)
        {
            await _saleService.DeleteAsync(sl.VNo, cancellationToken);
        }

        var existingPu = await _purchaseMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PU" && x.VDate == date && x.Descr != null && x.Descr.StartsWith(tagPrefix))
            .ToListAsync(cancellationToken);

        foreach (var pu in existingPu)
        {
            await _purchaseService.DeleteAsync(pu.VNo, cancellationToken);
        }
    }

    private static List<PurchaseLineRequest> BuildReconciledPurchaseLines(
        List<DailyStockClosingSaveLineRequest> rawLines,
        decimal calcGross,
        decimal targetTotal,
        bool isProRate)
    {
        var result = new List<PurchaseLineRequest>(rawLines.Count);
        var totalQty = rawLines.Sum(x => x.PurchasedQty);
        var diff = targetTotal - calcGross;
        decimal runningTotal = 0m;

        for (int i = 0; i < rawLines.Count; i++)
        {
            var raw = rawLines[i];
            var isLast = i == rawLines.Count - 1;
            var lineGross = Math.Round(raw.PurchasedQty * raw.PurchaseRate, 2);
            var weight = calcGross > 0
                ? (lineGross / calcGross)
                : (totalQty > 0 ? (raw.PurchasedQty / totalQty) : (1m / rawLines.Count));

            decimal rate = raw.PurchaseRate;
            decimal addLess = 0m;

            if (isProRate && raw.PurchasedQty > 0)
            {
                var targetLineAmt = isLast
                    ? (targetTotal - runningTotal)
                    : Math.Round(targetTotal * weight, 2);
                rate = Math.Round(targetLineAmt / raw.PurchasedQty, 4);
                var recomputedGross = Math.Round(raw.PurchasedQty * rate, 2);
                addLess = targetLineAmt - recomputedGross;
                runningTotal += recomputedGross + addLess;
            }
            else
            {
                var targetLineAmt = isLast
                    ? (targetTotal - runningTotal)
                    : Math.Round(lineGross + (diff * weight), 2);
                addLess = targetLineAmt - (raw.PurchasedQty * rate);
                runningTotal += (raw.PurchasedQty * rate) + addLess;
            }

            result.Add(new PurchaseLineRequest
            {
                Seq = i + 1,
                ItemId = raw.ItemId,
                Qty = raw.PurchasedQty,
                Rate = rate,
                AddLess = Math.Round(addLess, 2)
            });
        }

        return result;
    }

    private static List<SaleLineRequest> BuildReconciledSaleLines(
        List<DailyStockClosingSaveLineRequest> rawLines,
        decimal calcGross,
        decimal targetTotal,
        bool isProRate)
    {
        var result = new List<SaleLineRequest>(rawLines.Count);
        var totalQty = rawLines.Sum(x => x.SoldQty);
        var totalDiscount = calcGross - targetTotal;
        decimal runningNet = 0m;

        for (int i = 0; i < rawLines.Count; i++)
        {
            var raw = rawLines[i];
            var isLast = i == rawLines.Count - 1;
            var lineGross = Math.Round(raw.SoldQty * raw.SaleRate, 2);
            var weight = calcGross > 0
                ? (lineGross / calcGross)
                : (totalQty > 0 ? (raw.SoldQty / totalQty) : (1m / rawLines.Count));

            decimal rate = raw.SaleRate;
            decimal discount = 0m;

            if (isProRate && raw.SoldQty > 0)
            {
                var targetLineNet = isLast
                    ? (targetTotal - runningNet)
                    : Math.Round(targetTotal * weight, 2);
                rate = Math.Round(targetLineNet / raw.SoldQty, 4);
                var recomputedGross = Math.Round(raw.SoldQty * rate, 2);
                discount = recomputedGross - targetLineNet;
                runningNet += recomputedGross - discount;
            }
            else
            {
                var targetLineNet = isLast
                    ? (targetTotal - runningNet)
                    : Math.Round(lineGross - (totalDiscount * weight), 2);
                discount = (raw.SoldQty * rate) - targetLineNet;
                runningNet += (raw.SoldQty * rate) - discount;
            }

            result.Add(new SaleLineRequest
            {
                Seq = i + 1,
                ItemId = raw.ItemId,
                Qty = raw.SoldQty,
                Rate = rate,
                Discount = Math.Round(discount, 2)
            });
        }

        return result;
    }

    private static List<PaymentRecordRequest>? BuildPaymentRecords(
        string? cashAccountId,
        decimal cashAmount,
        string cashLabel,
        List<DailyStockClosingCreditLineDto> creditLines,
        string defaultCreditLabel)
    {
        if (creditLines.Count == 0)
        {
            // When there are no credit lines, let PurchaseService / SaleService use standard CashPaid / CashReceipt
            return null;
        }

        var payments = new List<PaymentRecordRequest>();
        var seq = 1;

        if (cashAmount > 0 && !string.IsNullOrWhiteSpace(cashAccountId))
        {
            payments.Add(new PaymentRecordRequest
            {
                Seq = seq++,
                AccountId = cashAccountId,
                Amount = Math.Round(cashAmount, 2),
                Reference = cashLabel
            });
        }

        foreach (var c in creditLines)
        {
            payments.Add(new PaymentRecordRequest
            {
                Seq = seq++,
                AccountId = c.AccountId,
                Amount = Math.Round(c.Amount, 2),
                Reference = string.IsNullOrWhiteSpace(c.Remarks) ? defaultCreditLabel : c.Remarks.Trim()
            });
        }

        return payments;
    }
}
