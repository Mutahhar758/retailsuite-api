using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Reports;
using Retailer.Domain.Legacy;
using Retailer.Domain.Common.Enums;

namespace Retailer.Infrastructure.Legacy.Reports;

internal class ReportService : IReportService
{
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<SaleMaster> _saleMasterRepository;
    private readonly IRepository<SaleSupplyDetail> _saleSupplyDetailRepository;
    private readonly IRepository<SaleSupplyMaster> _saleSupplyMasterRepository;
    private readonly IRepository<CustomerDetail> _customerDetailRepository;
    private readonly IRepository<CompanyDetail> _companyDetailRepository;
    private readonly IRepository<PurchaseDetail> _purchaseDetailRepository;
    private readonly IRepository<PurchaseMaster> _purchaseMasterRepository;
    private readonly IRepository<SupplierDetail> _supplierDetailRepository;
    private readonly IRepository<PurchaseRetDetail> _purchaseRetDetailRepository;
    private readonly IRepository<PurchaseRetMaster> _purchaseRetMasterRepository;
    private readonly IRepository<SaleRetDetail> _saleRetDetailRepository;
    private readonly IRepository<SaleRetMaster> _saleRetMasterRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;

    public ReportService(
        IRepository<GlEntry> glRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<Sale> saleRepository,
        IRepository<SaleMaster> saleMasterRepository,
        IRepository<SaleSupplyDetail> saleSupplyDetailRepository,
        IRepository<SaleSupplyMaster> saleSupplyMasterRepository,
        IRepository<CustomerDetail> customerDetailRepository,
        IRepository<CompanyDetail> companyDetailRepository,
        IRepository<PurchaseDetail> purchaseDetailRepository,
        IRepository<PurchaseMaster> purchaseMasterRepository,
        IRepository<SupplierDetail> supplierDetailRepository,
        IRepository<PurchaseRetDetail> purchaseRetDetailRepository,
        IRepository<PurchaseRetMaster> purchaseRetMasterRepository,
        IRepository<SaleRetDetail> saleRetDetailRepository,
        IRepository<SaleRetMaster> saleRetMasterRepository,
        IRepository<DefaultAccount> defaultAccountRepository)
    {
        _glRepository = glRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _saleRepository = saleRepository;
        _saleMasterRepository = saleMasterRepository;
        _saleSupplyDetailRepository = saleSupplyDetailRepository;
        _saleSupplyMasterRepository = saleSupplyMasterRepository;
        _customerDetailRepository = customerDetailRepository;
        _companyDetailRepository = companyDetailRepository;
        _purchaseDetailRepository = purchaseDetailRepository;
        _purchaseMasterRepository = purchaseMasterRepository;
        _supplierDetailRepository = supplierDetailRepository;
        _purchaseRetDetailRepository = purchaseRetDetailRepository;
        _purchaseRetMasterRepository = purchaseRetMasterRepository;
        _saleRetDetailRepository = saleRetDetailRepository;
        _saleRetMasterRepository = saleRetMasterRepository;
        _defaultAccountRepository = defaultAccountRepository;
    }

    public async Task<List<AccountStatementLineResponse>> GetAccountStatementAsync(AccountStatementFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.Account))
            throw new BadRequestException("Account is required.");

        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        bool useClearingDate = string.Equals(filter.DateBasis, "ClearingDate", StringComparison.OrdinalIgnoreCase);

        var openingBalance = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == filter.Account || x.CrAccountId == filter.Account)
                && (x.VType == "Op" || (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) < filter.FromDate))
            .Select(x => x.DrAccountId == filter.Account ? x.Amount : -x.Amount)
            .SumAsync(cancellationToken);

        var entries = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == filter.Account || x.CrAccountId == filter.Account)
                && (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) >= filter.FromDate
                && (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) <= filter.ToDate
                && x.VType != "Op")
            .Select(x => new
            {
                VDate = useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate,
                x.VType,
                x.VoucherNo,
                x.VSeq,
                x.Amount,
                x.DrAccountId,
                x.CrAccountId,
                x.Remarks
            })
            .ToListAsync(cancellationToken);

        var counterpartIds = entries
            .Select(x => x.DrAccountId == filter.Account ? x.CrAccountId : x.DrAccountId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var titleMap = counterpartIds.Count == 0
            ? new Dictionary<string, string>()
            : await _chartOfAccountRepository.GetAll()
                .AsNoTracking()
                .Where(x => counterpartIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var movementLines = entries
            .Select(x =>
            {
                var counterpartId = x.DrAccountId == filter.Account ? x.CrAccountId : x.DrAccountId;
                var counterpartTitle = !string.IsNullOrWhiteSpace(counterpartId) && titleMap.TryGetValue(counterpartId, out var title)
                    ? title
                    : string.Empty;
                var particular = counterpartTitle + "  " + (x.Remarks ?? string.Empty);

                return new
                {
                    x.VDate,
                    VNo = x.VType + "-" + x.VoucherNo,
                    x.VSeq,
                    Particular = particular,
                    Dr = x.DrAccountId == filter.Account ? x.Amount : 0m,
                    Cr = x.CrAccountId == filter.Account ? x.Amount : 0m
                };
            })
            .GroupBy(x => new { x.VDate, x.VNo, x.VSeq, x.Particular })
            .Select(g => new AccountStatementLineResponse
            {
                VDate = g.Key.VDate,
                VNo = g.Key.VNo,
                VSeq = g.Key.VSeq,
                Particular = g.Key.Particular,
                Dr = g.Sum(x => x.Dr),
                Cr = g.Sum(x => x.Cr)
            })
            .Where(x => (x.Dr - x.Cr) != 0)
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.VNo)
            .ThenBy(x => x.VSeq)
            .ToList();

        var result = new List<AccountStatementLineResponse>
        {
            new AccountStatementLineResponse
            {
                VDate = filter.FromDate.AddDays(-1),
                VNo = null,
                VSeq = 0,
                Particular = "Openning Balance",
                Dr = openingBalance >= 0 ? openingBalance : 0,
                Cr = openingBalance < 0 ? -openingBalance : 0
            }
        };

        result.AddRange(movementLines);
        return result;
    }

    public async Task<List<AccountStatementWithDueLineResponse>> GetAccountStatementWithDueAsync(AccountStatementFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.Account))
            throw new BadRequestException("Account is required.");

        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        bool useClearingDate = string.Equals(filter.DateBasis, "ClearingDate", StringComparison.OrdinalIgnoreCase);

        var openingBalance = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == filter.Account || x.CrAccountId == filter.Account)
                && (x.VType == "Op" || (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) < filter.FromDate))
            .Select(x => x.DrAccountId == filter.Account ? x.Amount : -x.Amount)
            .SumAsync(cancellationToken);

        var entries = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == filter.Account || x.CrAccountId == filter.Account)
                && (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) >= filter.FromDate
                && (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) <= filter.ToDate
                && x.VType != "Op")
            .Select(x => new
            {
                VDate = useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate,
                x.VType,
                x.VoucherNo,
                x.VSeq,
                x.Amount,
                x.DrAccountId,
                x.CrAccountId,
                x.Remarks
            })
            .ToListAsync(cancellationToken);

        var counterpartIds = entries
            .Select(x => x.DrAccountId == filter.Account ? x.CrAccountId : x.DrAccountId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var titleMap = counterpartIds.Count == 0
            ? new Dictionary<string, string>()
            : await _chartOfAccountRepository.GetAll()
                .AsNoTracking()
                .Where(x => counterpartIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var movementLines = entries
            .Select(x =>
            {
                var counterpartId = x.DrAccountId == filter.Account ? x.CrAccountId : x.DrAccountId;
                var counterpartTitle = !string.IsNullOrWhiteSpace(counterpartId) && titleMap.TryGetValue(counterpartId, out var title)
                    ? title
                    : string.Empty;
                var particular = counterpartTitle + "  " + (x.Remarks ?? string.Empty);

                return new
                {
                    x.VDate,
                    VNo = x.VType + "-" + x.VoucherNo,
                    x.VSeq,
                    Particular = particular,
                    Dr = x.DrAccountId == filter.Account ? x.Amount : 0m,
                    Cr = x.CrAccountId == filter.Account ? x.Amount : 0m
                };
            })
            .GroupBy(x => new { x.VDate, x.VNo, x.VSeq, x.Particular })
            .Select(g => new AccountStatementWithDueLineResponse
            {
                VDate = g.Key.VDate,
                VNo = g.Key.VNo,
                VSeq = g.Key.VSeq,
                Particular = g.Key.Particular,
                Dr = g.Sum(x => x.Dr),
                Cr = g.Sum(x => x.Cr),
                DueDays = null
            })
            .Where(x => (x.Dr - x.Cr) != 0)
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.VNo)
            .ThenBy(x => x.VSeq)
            .ToList();

        var result = new List<AccountStatementWithDueLineResponse>
        {
            new AccountStatementWithDueLineResponse
            {
                VDate = filter.FromDate.AddDays(-1),
                VNo = null,
                VSeq = 0,
                Particular = "Openning Balance",
                Dr = openingBalance >= 0 ? openingBalance : 0,
                Cr = openingBalance < 0 ? -openingBalance : 0,
                DueDays = null
            }
        };

        result.AddRange(movementLines);

        var totalBalance = result.Sum(x => x.Dr - x.Cr);
        var dueBalance = totalBalance;

        if (totalBalance > 0)
        {
            for (int i = result.Count - 1; i >= 0 && dueBalance > 0; i--)
            {
                var line = result[i];
                if (!string.IsNullOrWhiteSpace(line.VNo) && line.Dr > 0)
                {
                    line.DueDays = filter.ToDate.DayNumber - line.VDate.DayNumber;
                    dueBalance -= (line.Dr - line.Cr);
                }
            }
        }
        else if (totalBalance < 0)
        {
            for (int i = result.Count - 1; i >= 0 && dueBalance < 0; i--)
            {
                var line = result[i];
                if (!string.IsNullOrWhiteSpace(line.VNo) && line.Cr > 0)
                {
                    line.DueDays = filter.ToDate.DayNumber - line.VDate.DayNumber;
                    dueBalance += (line.Dr + line.Cr);
                }
            }
        }

        return result
            .OrderBy(x => x.VDate)
            .ThenBy(x => x.VNo)
            .ThenBy(x => x.VSeq)
            .ToList();
    }

    public async Task<List<BalanceDetailLineResponse>> GetBalanceDetailAsync(BalanceDetailFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.Account))
            throw new BadRequestException("Account is required.");

        var childAccounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.ParentId == filter.Account)
            .Select(x => new { x.Id, x.Title })
            .ToListAsync(cancellationToken);

        if (childAccounts.Count == 0)
            return new List<BalanceDetailLineResponse>();

        var accountIds = childAccounts.Select(x => x.Id).ToList();

        var balances = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.VDate <= filter.ToDate || x.VType == "Op")
                && (accountIds.Contains(x.DrAccountId!) || accountIds.Contains(x.CrAccountId!)))
            .Select(x => new
            {
                x.DrAccountId,
                x.CrAccountId,
                x.Amount
            })
            .ToListAsync(cancellationToken);

        var balanceMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < balances.Count; i++)
        {
            var dr = balances[i].DrAccountId;
            var cr = balances[i].CrAccountId;
            var amount = balances[i].Amount;

            if (!string.IsNullOrWhiteSpace(dr) && accountIds.Contains(dr))
            {
                decimal current;
                balanceMap.TryGetValue(dr, out current);
                balanceMap[dr] = current + amount;
            }

            if (!string.IsNullOrWhiteSpace(cr) && accountIds.Contains(cr))
            {
                decimal current;
                balanceMap.TryGetValue(cr, out current);
                balanceMap[cr] = current - amount;
            }
        }

        return childAccounts
            .Select(x => new BalanceDetailLineResponse
            {
                Account = x.Title,
                Balance = balanceMap.ContainsKey(x.Id) ? balanceMap[x.Id] : 0m
            })
            .Where(x => x.Balance != 0)
            .OrderBy(x => x.Account)
            .ToList();
    }

    public async Task<List<TrialBalanceLineResponse>> GetTrialBalanceAsync(TrialBalanceFilter filter, CancellationToken cancellationToken)
    {
        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        var allAccounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Select(x => new { x.Id, x.Title, x.ParentId, x.AccLevel })
            .ToListAsync(cancellationToken);

        var detailAccounts = allAccounts
            .Where(x => x.AccLevel == 5)
            .ToList();

        if (detailAccounts.Count == 0)
            return new List<TrialBalanceLineResponse>();

        var accountIds = detailAccounts.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var gl = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x =>
                (accountIds.Contains(x.DrAccountId!) || accountIds.Contains(x.CrAccountId!))
                && (
                    x.VType == "Op"
                    || (x.VDate >= filter.FromDate && x.VDate <= filter.ToDate)
                    || x.VDate <= filter.ToDate
                    || x.VDate < filter.FromDate))
            .Select(x => new
            {
                x.VDate,
                x.VType,
                x.Amount,
                x.DrAccountId,
                x.CrAccountId
            })
            .ToListAsync(cancellationToken);

        var pri = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var dr = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var cr = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var cur = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < gl.Count; i++)
        {
            var e = gl[i];
            var drAcc = e.DrAccountId;
            var crAcc = e.CrAccountId;
            var amount = e.Amount;

            var isPri = e.VType == "Op" || e.VDate < filter.FromDate;
            var isRange = e.VType != "Op" && e.VDate >= filter.FromDate && e.VDate <= filter.ToDate;
            var isCur = e.VType == "Op" || e.VDate <= filter.ToDate;

            if (!string.IsNullOrWhiteSpace(drAcc) && accountIds.Contains(drAcc))
            {
                if (isPri)
                    pri[drAcc] = (pri.ContainsKey(drAcc) ? pri[drAcc] : 0m) + amount;
                if (isRange)
                    dr[drAcc] = (dr.ContainsKey(drAcc) ? dr[drAcc] : 0m) + amount;
                if (isCur)
                    cur[drAcc] = (cur.ContainsKey(drAcc) ? cur[drAcc] : 0m) + amount;
            }

            if (!string.IsNullOrWhiteSpace(crAcc) && accountIds.Contains(crAcc))
            {
                if (isPri)
                    pri[crAcc] = (pri.ContainsKey(crAcc) ? pri[crAcc] : 0m) - amount;
                if (isRange)
                    cr[crAcc] = (cr.ContainsKey(crAcc) ? cr[crAcc] : 0m) + amount;
                if (isCur)
                    cur[crAcc] = (cur.ContainsKey(crAcc) ? cur[crAcc] : 0m) - amount;
            }
        }

        var accountMap = allAccounts.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

        string GetTitle(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;
            return accountMap.ContainsKey(id) ? accountMap[id].Title : string.Empty;
        }

        string? GetParent(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !accountMap.ContainsKey(id))
                return null;
            return accountMap[id].ParentId;
        }

        var result = new List<TrialBalanceLineResponse>();
        foreach (var a in detailAccounts)
        {
            result.Add(new TrialBalanceLineResponse
            {
                Lvl1 = GetTitle(GetParent(GetParent(GetParent(GetParent(a.Id))))),
                Lvl2 = GetTitle(GetParent(GetParent(GetParent(a.Id)))),
                Lvl3 = GetTitle(GetParent(GetParent(a.Id))),
                Lvl4 = GetTitle(GetParent(a.Id)),
                Title = a.Title,
                PriBal = pri.ContainsKey(a.Id) ? pri[a.Id] : 0m,
                Dr = dr.ContainsKey(a.Id) ? dr[a.Id] : 0m,
                Cr = cr.ContainsKey(a.Id) ? cr[a.Id] : 0m,
                CurBal = cur.ContainsKey(a.Id) ? cur[a.Id] : 0m
            });
        }

        return result
            .OrderBy(x => x.Lvl4)
            .ThenBy(x => x.Title)
            .ToList();
    }

    public async Task<List<StockLedgerLineResponse>> GetStockLedgerAsync(StockLedgerFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.FkItem))
            throw new BadRequestException("Item is required.");
        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        var openingStock = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemId == filter.FkItem && (x.VType == "Op" || x.VDate < filter.FromDate))
            .Select(x => x.QtyIn - x.QtyOut)
            .SumAsync(cancellationToken);

        var openingSecStock = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemId == filter.FkItem && (x.VType == "Op" || x.VDate < filter.FromDate))
            .Select(x => (x.SecQtyIn ?? 0) - (x.SecQtyOut ?? 0))
            .SumAsync(cancellationToken);

        var movement = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemId == filter.FkItem
                && x.VType != "Op"
                && x.VDate >= filter.FromDate
                && x.VDate <= filter.ToDate)
            .Select(x => new StockLedgerLineResponse
            {
                Vdate = x.VDate,
                Vno = x.VType + "-" + x.VNo,
                Particular = x.Account != null ? x.Account.Title : string.Empty,
                QtyIn = x.QtyIn,
                QtyOut = x.QtyOut,
                Rate = x.Rate,
                SecUnit = x.SecUnit != null ? x.SecUnit.Title : x.SecUnitId,
                SecQtyIn = x.SecQtyIn ?? 0,
                SecQtyOut = x.SecQtyOut ?? 0
            })
            .OrderBy(x => x.Vdate)
            .ThenBy(x => x.Vno)
            .ToListAsync(cancellationToken);

        var result = new List<StockLedgerLineResponse>
        {
            new StockLedgerLineResponse
            {
                Vdate = filter.FromDate.AddDays(-1),
                Vno = null,
                Particular = "Openning Stock",
                QtyIn = openingStock >= 0 ? openingStock : 0m,
                QtyOut = openingStock < 0 ? -openingStock : 0m,
                Rate = null,
                SecQtyIn = openingSecStock >= 0 ? openingSecStock : 0m,
                SecQtyOut = openingSecStock < 0 ? -openingSecStock : 0m
            }
        };

        result.AddRange(movement);
        return result;
    }

    public async Task<List<StockBalanceLineResponse>> GetStockBalanceAsync(StockBalanceFilter filter, CancellationToken cancellationToken)
    {
        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        var rows = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate <= filter.ToDate && !string.IsNullOrWhiteSpace(x.ItemId))
            .Where(x => x.Item != null && x.Item.ItemType != ItemType.Service)
            .Where(x => string.IsNullOrWhiteSpace(filter.Catagory) || (x.Item != null && x.Item.ItemCategoryId == filter.Catagory))
            .Select(x => new
            {
                x.ItemId,
                Item = x.Item != null ? x.Item.Title : string.Empty,
                Unit = x.Item != null ? (x.Item.DefaultUnit != null ? x.Item.DefaultUnit.Title : x.Item.DefaultUnitId) : string.Empty,
                SecUnit = x.Item != null ? (x.Item.SecondaryUnit != null ? x.Item.SecondaryUnit.Title : x.Item.SecondaryUnitId) : string.Empty,
                x.VDate,
                x.VType,
                x.TranType,
                x.QtyIn,
                x.QtyOut,
                SecQtyIn = x.SecQtyIn ?? 0,
                SecQtyOut = x.SecQtyOut ?? 0,
                x.Rate
            })
            .ToListAsync(cancellationToken);

        var data = rows
            .GroupBy(x => new { x.ItemId, x.Item, x.Unit, x.SecUnit })
            .Select(g =>
            {
                var priQty = g.Where(x => x.VDate < filter.FromDate || x.VType == "Op").Sum(x => x.QtyIn - x.QtyOut);
                var qty = g.Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate && x.VType != "Op").Sum(x => x.QtyIn - x.QtyOut);
                var qtyIn = g.Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate && x.VType != "Op").Sum(x => x.QtyIn);
                var qtyOut = g.Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate && x.VType != "Op").Sum(x => x.QtyOut);
                var qtyBal = g.Sum(x => x.QtyIn - x.QtyOut);
                var positiveQtyBal = g.Where(x => (x.QtyIn - x.QtyOut) > 0).Sum(x => x.QtyIn - x.QtyOut);
                var amt = g.Where(x => string.Equals(x.TranType, "in", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => (x.QtyIn - x.QtyOut) * x.Rate);
                var rate = positiveQtyBal == 0 ? 0 : amt / positiveQtyBal;

                var secPriQty = g.Where(x => x.VDate < filter.FromDate || x.VType == "Op").Sum(x => x.SecQtyIn - x.SecQtyOut);
                var secQtyIn = g.Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate && x.VType != "Op").Sum(x => x.SecQtyIn);
                var secQtyOut = g.Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate && x.VType != "Op").Sum(x => x.SecQtyOut);
                var secQtyBal = g.Sum(x => x.SecQtyIn - x.SecQtyOut);

                return new StockBalanceLineResponse
                {
                    Item = g.Key.Item,
                    Unit = g.Key.Unit ?? string.Empty,
                    PriQty = priQty,
                    Qty = qty,
                    QtyIn = qtyIn,
                    QtyOut = qtyOut,
                    QtyBal = qtyBal,
                    Rate = rate,
                    SecUnit = g.Key.SecUnit,
                    SecPriQty = secPriQty,
                    SecQtyIn = secQtyIn,
                    SecQtyOut = secQtyOut,
                    SecQtyBal = secQtyBal
                };
            })
            .Where(x => x.QtyBal != 0 || x.SecQtyBal != 0)
            .ToList();

        var mode = (filter.Filter ?? "All").Trim();
        if (mode.Equals("Equal", StringComparison.OrdinalIgnoreCase))
            data = data.Where(x => x.QtyBal == filter.Qty).ToList();
        else if (mode.Equals("Greater", StringComparison.OrdinalIgnoreCase))
            data = data.Where(x => x.QtyBal >= filter.Qty).ToList();
        else if (mode.Equals("Less", StringComparison.OrdinalIgnoreCase))
            data = data.Where(x => x.QtyBal <= filter.Qty).ToList();

        return data
            .OrderBy(x => x.Item)
            .ToList();
    }

    public async Task<List<BalanceSheetLineResponse>> GetBalanceSheetAsync(BalanceSheetFilter filter, CancellationToken cancellationToken)
    {
        var allAccounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Select(x => new { x.Id, x.Title, x.ParentId, x.AccLevel })
            .ToListAsync(cancellationToken);

        var detailAccounts = allAccounts
            .Where(x => x.AccLevel == 5)
            .ToList();

        if (detailAccounts.Count == 0)
            return new List<BalanceSheetLineResponse>();

        var accountIds = detailAccounts.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var gl = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x =>
                (accountIds.Contains(x.DrAccountId!) || accountIds.Contains(x.CrAccountId!))
                && (x.VType == "Op" || x.VDate <= filter.ToDate))
            .Select(x => new
            {
                x.Amount,
                x.DrAccountId,
                x.CrAccountId
            })
            .ToListAsync(cancellationToken);

        var curBal = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < gl.Count; i++)
        {
            var e = gl[i];
            var drAcc = e.DrAccountId;
            var crAcc = e.CrAccountId;
            var amount = e.Amount;

            if (!string.IsNullOrWhiteSpace(drAcc) && accountIds.Contains(drAcc))
                curBal[drAcc] = (curBal.ContainsKey(drAcc) ? curBal[drAcc] : 0m) + amount;

            if (!string.IsNullOrWhiteSpace(crAcc) && accountIds.Contains(crAcc))
                curBal[crAcc] = (curBal.ContainsKey(crAcc) ? curBal[crAcc] : 0m) - amount;
        }

        var accountMap = allAccounts.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

        string? GetParent(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !accountMap.ContainsKey(id))
                return null;
            return accountMap[id].ParentId;
        }

        string GetTitle(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !accountMap.ContainsKey(id))
                return string.Empty;
            return accountMap[id].Title;
        }

        var grouped = detailAccounts
            .Select(a => new
            {
                Lvl5Id = a.Id,
                Lvl4Id = GetParent(a.Id),
                Lvl3Id = GetParent(GetParent(a.Id)),
                Lvl2Id = GetParent(GetParent(GetParent(a.Id))),
                Lvl1Id = GetParent(GetParent(GetParent(GetParent(a.Id)))),
                Balance = curBal.ContainsKey(a.Id) ? curBal[a.Id] : 0m
            })
            .Where(x => x.Balance != 0m)
            .Where(x => x.Lvl1Id == "001" || x.Lvl1Id == "002" || x.Lvl1Id == "005")
            .GroupBy(x => new { x.Lvl1Id, x.Lvl2Id, x.Lvl3Id, x.Lvl4Id })
            .Select(g => new BalanceSheetLineResponse
            {
                Lvl1 = GetTitle(g.Key.Lvl1Id),
                Lvl2 = GetTitle(g.Key.Lvl2Id),
                Lvl3 = GetTitle(g.Key.Lvl3Id),
                Lvl4 = GetTitle(g.Key.Lvl4Id),
                Title = GetTitle(g.Key.Lvl4Id),
                PriBal = 0m,
                DrCr = g.Sum(x => x.Balance),
                CurBal = g.Sum(x => x.Balance)
            })
            .OrderBy(x => x.Lvl1)
            .ThenBy(x => x.Lvl2)
            .ThenBy(x => x.Lvl3)
            .ThenBy(x => x.Lvl4)
            .ToList();

        return grouped;
    }

    public async Task<List<IncomeSummaryLineResponse>> GetIncomeSummaryAsync(IncomeSummaryFilter filter, CancellationToken cancellationToken)
    {
        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        var allAccounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Select(x => new { x.Id, x.Title, x.ParentId, x.AccLevel })
            .ToListAsync(cancellationToken);

        var accountMap = allAccounts.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

        string? GetParent(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || !accountMap.ContainsKey(id))
                return null;
            return accountMap[id].ParentId;
        }

        var glRows = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate >= filter.FromDate && x.VDate <= filter.ToDate)
            .Select(x => new { x.Amount, x.DrAccountId, x.CrAccountId })
            .ToListAsync(cancellationToken);

        var drMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var crMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < glRows.Count; i++)
        {
            var row = glRows[i];
            if (!string.IsNullOrWhiteSpace(row.DrAccountId))
                drMap[row.DrAccountId] = (drMap.ContainsKey(row.DrAccountId) ? drMap[row.DrAccountId] : 0m) + row.Amount;

            if (!string.IsNullOrWhiteSpace(row.CrAccountId))
                crMap[row.CrAccountId] = (crMap.ContainsKey(row.CrAccountId) ? crMap[row.CrAccountId] : 0m) + row.Amount;
        }

        var detailAccounts = allAccounts.Where(x => x.AccLevel == 5).ToList();

        var salesRows = detailAccounts
            .Where(x => GetParent(GetParent(GetParent(GetParent(x.Id)))) == "003")
            .Select(x =>
            {
                var dr = drMap.ContainsKey(x.Id) ? drMap[x.Id] : 0m;
                var cr = crMap.ContainsKey(x.Id) ? crMap[x.Id] : 0m;
                return new IncomeSummaryLineResponse
                {
                    VType = "Sales",
                    Title = x.Title,
                    Dr = dr,
                    Cr = cr,
                    Bal = dr - cr
                };
            })
            .Where(x => x.Bal != 0)
            .ToList();

        var purchaseAccount = detailAccounts
            .FirstOrDefault(x => string.Equals(x.Title, "Purchase", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(x.Title, "PU", StringComparison.OrdinalIgnoreCase));

        var openingStock = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate < filter.FromDate || x.VType == "Op")
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                QtyBal = g.Sum(x => x.QtyIn - x.QtyOut),
                PosQtyBal = g.Where(x => (x.QtyIn - x.QtyOut) > 0).Sum(x => x.QtyIn - x.QtyOut),
                Amt = g.Where(x => x.TranType == "in").Sum(x => (x.QtyIn - x.QtyOut) * x.Rate)
            })
            .ToListAsync(cancellationToken);

        var openingStockValue = openingStock.Sum(x => x.QtyBal * (x.PosQtyBal == 0 ? 0 : x.Amt / x.PosQtyBal));

        var closingStock = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate <= filter.ToDate || x.VType == "Op")
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                QtyBal = g.Sum(x => x.QtyIn - x.QtyOut),
                PosQtyBal = g.Where(x => (x.QtyIn - x.QtyOut) > 0).Sum(x => x.QtyIn - x.QtyOut),
                Amt = g.Where(x => x.TranType == "in").Sum(x => (x.QtyIn - x.QtyOut) * x.Rate)
            })
            .ToListAsync(cancellationToken);

        var closingStockValue = closingStock.Sum(x => x.QtyBal * (x.PosQtyBal == 0 ? 0 : x.Amt / x.PosQtyBal));

        var purchaseDr = purchaseAccount != null && drMap.ContainsKey(purchaseAccount.Id) ? drMap[purchaseAccount.Id] : 0m;
        var purchaseCr = purchaseAccount != null && crMap.ContainsKey(purchaseAccount.Id) ? crMap[purchaseAccount.Id] : 0m;

        var cogsRows = new List<IncomeSummaryLineResponse>
        {
            new IncomeSummaryLineResponse { VType = "Cost of Goods Sold", Title = "Openning Stock", Dr = openingStockValue, Cr = 0m, Bal = openingStockValue },
            new IncomeSummaryLineResponse { VType = "Cost of Goods Sold", Title = "PURCHASE", Dr = purchaseDr, Cr = purchaseCr, Bal = purchaseDr - purchaseCr },
            new IncomeSummaryLineResponse { VType = "Cost of Goods Sold", Title = "Closing Stock", Dr = -closingStockValue, Cr = 0m, Bal = -closingStockValue }
        };

        var expenseRows = detailAccounts
            .Where(x => GetParent(GetParent(GetParent(GetParent(x.Id)))) == "004")
            .Where(x => purchaseAccount == null || !string.Equals(x.Id, purchaseAccount.Id, StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var dr = drMap.ContainsKey(x.Id) ? drMap[x.Id] : 0m;
                var cr = crMap.ContainsKey(x.Id) ? crMap[x.Id] : 0m;
                return new IncomeSummaryLineResponse
                {
                    VType = "Expenses",
                    Title = x.Title,
                    Dr = dr,
                    Cr = cr,
                    Bal = dr - cr
                };
            })
            .Where(x => x.Bal != 0)
            .ToList();

        var result = new List<IncomeSummaryLineResponse>();
        result.AddRange(salesRows);
        result.AddRange(cogsRows);
        result.AddRange(expenseRows);
        return result;
    }

    public async Task<CustomerBillResponse> GetCustomerBillAsync(CustomerBillFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.Account))
            throw new BadRequestException("Account is required.");

        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        var unitMap = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.UnitId))
            .Select(x => new { Code = x.UnitId!, Title = x.Unit != null ? x.Unit.Title : x.UnitId! })
            .Distinct()
            .ToDictionaryAsync(x => x.Code, x => x.Title, cancellationToken);

        var saleLines = await (
            from sd in _saleRepository.GetAll().AsNoTracking()
            join sm in _saleMasterRepository.GetAll().AsNoTracking()
                on new { sd.VType, sd.VNo } equals new { sm.VType, VNo = sm.VNo }
            where sm.AccountId == filter.Account
                  && sm.VDate >= filter.FromDate && sm.VDate <= filter.ToDate
            select new CustomerBillLineResponse
            {
                Date = sm.VDate,
                VNo = sd.VType + "-" + sd.VNo,
                Item = sd.Item != null ? sd.Item.Title : (sd.ItemId ?? string.Empty),
                UnitId = sd.UnitId ?? string.Empty,
                UnitTitle = !string.IsNullOrWhiteSpace(sd.UnitId) && unitMap.ContainsKey(sd.UnitId) ? unitMap[sd.UnitId] : (sd.UnitId ?? string.Empty),
                Qty = sd.Qty,
                Rate = (sd.GrossRate ?? 0m) - (sd.Discount ?? 0m),
                AddLess = 0m,
                Amount = sd.Qty * ((sd.GrossRate ?? 0m) - (sd.Discount ?? 0m)),
                SecQty = sd.SecQty,
                SecRate = sd.SecRate,
                QtyInPack = sd.QtyInPack,
                SecUnitTitle = !string.IsNullOrWhiteSpace(sd.SecUnitId) && unitMap.ContainsKey(sd.SecUnitId) ? unitMap[sd.SecUnitId] : (sd.SecUnitId ?? string.Empty)
            }).ToListAsync(cancellationToken);

        var supplyLines = await (
            from ssd in _saleSupplyDetailRepository.GetAll().AsNoTracking()
            join ssm in _saleSupplyMasterRepository.GetAll().AsNoTracking()
                on new { ssd.VType, ssd.VNo } equals new { ssm.VType, VNo = ssm.VNo }
            where ssd.CustomerAccountId == filter.Account
                  && ssm.VDate >= filter.FromDate && ssm.VDate <= filter.ToDate
            select new CustomerBillLineResponse
            {
                Date = ssm.VDate,
                VNo = ssd.VType + "-" + ssd.VNo,
                Item = ssm.Item != null ? ssm.Item.Title : (ssm.ItemId ?? string.Empty),
                UnitId = ssd.UnitId ?? string.Empty,
                UnitTitle = ssd.Unit != null ? ssd.Unit.Title : (ssd.UnitId ?? string.Empty),
                Qty = ssd.Qty,
                Rate = (ssd.GrossRate ?? 0m) - (ssd.Discount ?? 0m),
                AddLess = ssd.AddLess ?? 0m,
                Amount = (ssd.Qty * ((ssd.GrossRate ?? 0m) - (ssd.Discount ?? 0m))) + (ssd.AddLess ?? 0m),
                SecQty = ssd.SecQty,
                SecRate = ssd.SecRate,
                QtyInPack = ssd.QtyInPack,
                SecUnitTitle = ssd.SecUnit != null ? ssd.SecUnit.Title : (!string.IsNullOrWhiteSpace(ssd.SecUnitId) && unitMap.ContainsKey(ssd.SecUnitId) ? unitMap[ssd.SecUnitId] : (ssd.SecUnitId ?? string.Empty))
            }).ToListAsync(cancellationToken);

        var lines = saleLines
            .Concat(supplyLines)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.VNo)
            .ToList();

        bool useClearingDate = string.Equals(filter.DateBasis, "ClearingDate", StringComparison.OrdinalIgnoreCase);

        var previousBalance = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == filter.Account || x.CrAccountId == filter.Account)
                        && (x.VType == "Op" || (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) < filter.FromDate))
            .Select(x => x.DrAccountId == filter.Account ? x.Amount : -x.Amount)
            .SumAsync(cancellationToken);

        var payment = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == filter.Account || x.CrAccountId == filter.Account)
                        && (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) >= filter.FromDate
                        && (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) <= filter.ToDate
                        && (x.VType == "PV" || x.VType == "RV" || x.VType == "JV"))
            .Select(x => x.DrAccountId == filter.Account ? x.Amount : -x.Amount)
            .SumAsync(cancellationToken);

        var balance = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == filter.Account || x.CrAccountId == filter.Account)
                        && (x.VType == "Op" || (useClearingDate ? (x.ClearingDate ?? x.VDate) : x.VDate) <= filter.ToDate))
            .Select(x => x.DrAccountId == filter.Account ? x.Amount : -x.Amount)
            .SumAsync(cancellationToken);

        return new CustomerBillResponse
        {
            Lines = lines,
            Summary = new CustomerBillSummaryResponse
            {
                PreviousBalance = previousBalance,
                Payment = payment,
                Balance = balance
            }
        };
    }

    public async Task<List<EnvelopeLineResponse>> GetEnvelopeAsync(EnvelopeFilter filter, CancellationToken cancellationToken)
    {
        var accountIds = (filter.Accounts ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (accountIds.Count == 0)
            return new List<EnvelopeLineResponse>();

        var companyName = await _companyDetailRepository.GetAll()
            .AsNoTracking()
            .Select(x => x.CompanyName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var customers = await (from c in _chartOfAccountRepository.GetAll().AsNoTracking()
                               join cd in _customerDetailRepository.GetAll().AsNoTracking() on c.Id equals cd.Id into cdj
                               from cd in cdj.DefaultIfEmpty()
                               where accountIds.Contains(c.Id)
                               select new EnvelopeLineResponse
                               {
                                   CustomerName = c.Title,
                                   Address = cd != null ? (cd.Address ?? string.Empty) : string.Empty,
                                   Cell = cd != null ? (cd.SmsNumber ?? string.Empty) : string.Empty,
                                   CompanyName = companyName
                               }).ToListAsync(cancellationToken);

        return customers;
    }

    public async Task<SaleBillResponse> GetSaleBillAsync(SaleBillFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.VoucherNo))
            throw new BadRequestException("VoucherNo is required.");

        var lines = await (
            from sd in _saleRepository.GetAll().AsNoTracking()
            where sd.VType == "SL" && sd.VNo == filter.VoucherNo
            select new SaleBillLineResponse
            {
                ItemName = sd.Item != null ? sd.Item.Title : string.Empty,
                Qty = sd.Qty,
                UnitId = sd.UnitId ?? string.Empty,
                UnitTitle = sd.Unit != null ? sd.Unit.Title : (sd.UnitId ?? string.Empty),
                Rate = (sd.GrossRate ?? 0m) - (sd.Discount ?? 0m),
                GrossRate = sd.GrossRate ?? 0m,
                Disc = sd.Discount ?? 0m,
                TAmount = sd.Qty * (sd.GrossRate ?? 0m)
            }).ToListAsync(cancellationToken);

        var master = await _saleMasterRepository.GetAll()
        .AsNoTracking()
        .Where(x => x.VType == "SL" && x.VNo == filter.VoucherNo)
        .Select(x => new
        {
            x.VDate,
            AccountTitle = x.Account != null ? x.Account.Title : string.Empty,
            x.Amount,
            x.Discount,
            x.NetAmount,
            x.CashReceipt,
            x.CashBack,
            x.Descr,
            x.AccountId
        })
        .FirstOrDefaultAsync(cancellationToken);

        if (master == null)
            return new SaleBillResponse();

        var balance = await _glRepository.GetAll()
        .AsNoTracking()
        .Where(x => (x.DrAccountId == master.AccountId || x.CrAccountId == master.AccountId)
                    && (x.VType == "Op" || x.VDate <= master.VDate))
        .Select(x => x.DrAccountId == master.AccountId ? x.Amount : -x.Amount)
        .SumAsync(cancellationToken);

        var accAddress = await _customerDetailRepository.GetAll()
        .AsNoTracking()
        .Where(x => x.Id == master.AccountId)
        .Select(x => x.Address)
        .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new SaleBillResponse
        {
            Lines = lines,
            Header = new SaleBillHeaderResponse
            {
                VDate = master.VDate,
                Title = master.AccountTitle ?? string.Empty,
                Amount = master.Amount ?? 0m,
                Discount = master.Discount ?? 0m,
                NetAmount = master.NetAmount ?? 0m,
                CashReceipt = master.CashReceipt,
                CashBack = master.CashBack ?? 0m,
                Descr = master.Descr ?? string.Empty,
                Balance = balance
            },
            AccAddress = accAddress
        };
    }

    public async Task<PurchaseBillResponse> GetPurchaseBillAsync(PurchaseBillFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.VoucherNo))
            throw new BadRequestException("VoucherNo is required.");

        var lines = await (
            from pd in _purchaseDetailRepository.GetAll().AsNoTracking()
            where pd.VType == "PU" && pd.VNo == filter.VoucherNo
            select new PurchaseBillLineResponse
            {
                ItemName = pd.Item != null ? pd.Item.Title : string.Empty,
                Qty = pd.Qty,
                QtyInPack = pd.QtyInPack ?? 0m,
                UnitId = pd.UnitId ?? string.Empty,
                UnitTitle = pd.Unit != null ? pd.Unit.Title : (pd.UnitId ?? string.Empty),
                Rate = pd.Rate,
                TAmount = pd.Qty * pd.Rate
            }).ToListAsync(cancellationToken);

        var master = await _purchaseMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PU" && x.VNo == filter.VoucherNo)
            .Select(x => new
            {
                x.VDate,
                AccountTitle = x.Account != null ? x.Account.Title : string.Empty,
                x.Amount,
                x.AccountId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (master == null)
            return new PurchaseBillResponse();

        var accAddress = await _supplierDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Id == master.AccountId)
            .Select(x => x.Address)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new PurchaseBillResponse
        {
            Lines = lines,
            Header = new PurchaseBillHeaderResponse
            {
                VDate = master.VDate,
                Title = master.AccountTitle ?? string.Empty,
                Amount = master.Amount ?? 0m,
                Discount = 0m,
                NetAmount = master.Amount ?? 0m
            },
            AccAddress = accAddress
        };
    }

    public async Task<PurchaseBillResponse> GetPurchaseRetBillAsync(PurchaseBillFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.VoucherNo))
            throw new BadRequestException("VoucherNo is required.");

        var lines = await (
            from pd in _purchaseRetDetailRepository.GetAll().AsNoTracking()
            where pd.VType == "PR" && pd.VNo == filter.VoucherNo
            select new PurchaseBillLineResponse
            {
                ItemName = pd.Item != null ? pd.Item.Title : string.Empty,
                Qty = pd.Qty,
                QtyInPack = pd.QtyInPack ?? 0m,
                UnitId = pd.UnitId ?? string.Empty,
                UnitTitle = pd.Unit != null ? pd.Unit.Title : (pd.UnitId ?? string.Empty),
                Rate = pd.Rate,
                TAmount = pd.Qty * pd.Rate
            }).ToListAsync(cancellationToken);

        var master = await _purchaseRetMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PR" && x.VNo == filter.VoucherNo)
            .Select(x => new
            {
                x.VDate,
                AccountTitle = x.Account != null ? x.Account.Title : string.Empty,
                x.Amount,
                x.AccountId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (master == null)
            return new PurchaseBillResponse();

        var accAddress = await _supplierDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Id == master.AccountId)
            .Select(x => x.Address)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return new PurchaseBillResponse
        {
            Lines = lines,
            Header = new PurchaseBillHeaderResponse
            {
                VDate = master.VDate,
                Title = master.AccountTitle ?? string.Empty,
                Amount = master.Amount ?? 0m,
                Discount = 0m,
                NetAmount = master.Amount ?? 0m
            },
            AccAddress = accAddress
        };
    }

    public async Task<SaleRetBillResponse> GetSaleRetBillAsync(SaleRetBillFilter filter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.VoucherNo))
            throw new BadRequestException("VoucherNo is required.");

        var lines = await (
            from sd in _saleRetDetailRepository.GetAll().AsNoTracking()
            where sd.VType == "SR" && sd.VNo == filter.VoucherNo
            select new SaleRetBillLineResponse
            {
                ItemName = sd.Item != null ? sd.Item.Title : string.Empty,
                Qty = sd.Qty,
                QtyInPack = sd.QtyInPack ?? 0m,
                UnitId = sd.UnitId ?? string.Empty,
                UnitTitle = sd.Unit != null ? sd.Unit.Title : (sd.UnitId ?? string.Empty),
                Rate = (sd.GrossRate ?? 0m) - (sd.Discount ?? 0m),
                GrossRate = sd.GrossRate ?? 0m,
                TAmount = sd.Qty * (sd.GrossRate ?? 0m)
            }).ToListAsync(cancellationToken);

        var master = await _saleRetMasterRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "SR" && x.VNo == filter.VoucherNo)
            .Select(x => new
            {
                x.VDate,
                AccountTitle = x.Account != null ? x.Account.Title : string.Empty,
                x.Amount,
                x.Discount,
                x.NetAmount,
                x.CashReceipt,
                x.CashBack,
                x.Descr,
                x.AccountId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (master == null)
            return new SaleRetBillResponse();

        var balance = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.DrAccountId == master.AccountId || x.CrAccountId == master.AccountId)
                        && (x.VType == "Op" || x.VDate <= master.VDate))
            .Select(x => x.DrAccountId == master.AccountId ? x.Amount : -x.Amount)
            .SumAsync(cancellationToken);

        var accAddress = await _vendorApiAddressLookup(master.AccountId, cancellationToken);

        return new SaleRetBillResponse
        {
            Lines = lines,
            Header = new SaleRetBillHeaderResponse
            {
                VDate = master.VDate,
                Title = master.AccountTitle ?? string.Empty,
                Amount = master.Amount ?? 0m,
                Discount = master.Discount ?? 0m,
                NetAmount = master.NetAmount ?? 0m,
                CashReceipt = master.CashReceipt,
                CashBack = master.CashBack ?? 0m,
                Descr = master.Descr ?? string.Empty,
                Balance = balance
            },
            AccAddress = accAddress
        };
    }

    private async Task<string> _vendorApiAddressLookup(string accountId, CancellationToken cancellationToken)
    {
        return await _supplierDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => x.Address)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
    }

    public async Task<PurchaseSupplyComparisonResponse> GetPurchaseSupplyComparisonAsync(PurchaseSupplyComparisonFilter filter, CancellationToken cancellationToken)
    {
        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        // 1. Fetch Purchase transactions in date range
        var purchaseQuery = from pd in _purchaseDetailRepository.GetAll().AsNoTracking()
                            join pm in _purchaseMasterRepository.GetAll().AsNoTracking()
                                on new { pd.VType, pd.VNo } equals new { pm.VType, VNo = pm.VNo }
                            where pm.VDate >= filter.FromDate && pm.VDate <= filter.ToDate
                                  && pm.VType == "PU"
                            select new
                            {
                                pm.VDate,
                                pd.ItemId,
                                ItemTitle = pd.Item != null ? pd.Item.Title : string.Empty,
                                UnitTitle = pd.Unit != null ? pd.Unit.Title : (pd.Item != null && pd.Item.PrimaryUnit != null ? pd.Item.PrimaryUnit.Title : string.Empty),
                                pd.Qty,
                                pd.Rate,
                                pd.AddLess,
                                Amount = (pd.Qty * pd.Rate) + pd.AddLess
                            };

        if (!string.IsNullOrWhiteSpace(filter.ItemId))
        {
            purchaseQuery = purchaseQuery.Where(x => x.ItemId == filter.ItemId);
        }

        var purchases = await purchaseQuery.ToListAsync(cancellationToken);

        // 2. Fetch Sale Supply transactions in date range
        var supplyQuery = from ssd in _saleSupplyDetailRepository.GetAll().AsNoTracking()
                          join ssm in _saleSupplyMasterRepository.GetAll().AsNoTracking()
                              on new { ssd.VType, ssd.VNo } equals new { ssm.VType, VNo = ssm.VNo }
                          where ssm.VDate >= filter.FromDate && ssm.VDate <= filter.ToDate
                                && (ssm.VType == "SP" || ssm.VType == "SS")
                          select new
                          {
                              ssm.VDate,
                              ssm.ItemId,
                              ItemTitle = ssm.Item != null ? ssm.Item.Title : string.Empty,
                              UnitTitle = ssm.Item != null && ssm.Item.PrimaryUnit != null ? ssm.Item.PrimaryUnit.Title : string.Empty,
                              ssd.Qty,
                              GrossRate = ssd.GrossRate ?? 0m,
                              Discount = ssd.Discount ?? 0m,
                              AddLess = ssd.AddLess ?? 0m,
                              Amount = (ssd.Qty * ((ssd.GrossRate ?? 0m) - (ssd.Discount ?? 0m))) + (ssd.AddLess ?? 0m)
                          };

        if (!string.IsNullOrWhiteSpace(filter.ItemId))
        {
            supplyQuery = supplyQuery.Where(x => x.ItemId == filter.ItemId);
        }

        var supplies = await supplyQuery.ToListAsync(cancellationToken);

        // 3. Fetch regular Sale transactions (if any) in date range
        var regularSaleQuery = from sd in _saleRepository.GetAll().AsNoTracking()
                               join sm in _saleMasterRepository.GetAll().AsNoTracking()
                                   on new { sd.VType, sd.VNo } equals new { sm.VType, VNo = sm.VNo }
                               where sm.VDate >= filter.FromDate && sm.VDate <= filter.ToDate
                                     && sm.VType == "SL"
                               select new
                               {
                                   sm.VDate,
                                   sd.ItemId,
                                   ItemTitle = sd.Item != null ? sd.Item.Title : string.Empty,
                                   UnitTitle = sd.Unit != null ? sd.Unit.Title : (sd.Item != null && sd.Item.PrimaryUnit != null ? sd.Item.PrimaryUnit.Title : string.Empty),
                                   sd.Qty,
                                   GrossRate = sd.GrossRate ?? 0m,
                                   Discount = sd.Discount ?? 0m,
                                   Amount = sd.Qty * ((sd.GrossRate ?? 0m) - (sd.Discount ?? 0m))
                               };

        if (!string.IsNullOrWhiteSpace(filter.ItemId))
        {
            regularSaleQuery = regularSaleQuery.Where(x => x.ItemId == filter.ItemId);
        }

        var regularSales = await regularSaleQuery.ToListAsync(cancellationToken);

        // Resolve item title and unit
        var itemTitle = "All Items";
        var unitTitle = string.Empty;
        if (!string.IsNullOrWhiteSpace(filter.ItemId))
        {
            itemTitle = purchases.FirstOrDefault(x => !string.IsNullOrEmpty(x.ItemTitle))?.ItemTitle
                        ?? supplies.FirstOrDefault(x => !string.IsNullOrEmpty(x.ItemTitle))?.ItemTitle
                        ?? regularSales.FirstOrDefault(x => !string.IsNullOrEmpty(x.ItemTitle))?.ItemTitle
                        ?? filter.ItemId;

            unitTitle = purchases.FirstOrDefault(x => !string.IsNullOrEmpty(x.UnitTitle))?.UnitTitle
                        ?? supplies.FirstOrDefault(x => !string.IsNullOrEmpty(x.UnitTitle))?.UnitTitle
                        ?? regularSales.FirstOrDefault(x => !string.IsNullOrEmpty(x.UnitTitle))?.UnitTitle
                        ?? string.Empty;
        }

        // Generate daily lines
        var lines = new List<PurchaseSupplyComparisonLineResponse>();
        var currentDate = filter.FromDate;

        while (currentDate <= filter.ToDate)
        {
            var dayPurchases = purchases.Where(x => x.VDate == currentDate).ToList();
            var daySupplies = supplies.Where(x => x.VDate == currentDate).ToList();
            var dayRegularSales = regularSales.Where(x => x.VDate == currentDate).ToList();

            var pQty = dayPurchases.Sum(x => x.Qty);
            var pAmount = dayPurchases.Sum(x => x.Amount);
            var pAvgRate = pQty > 0 ? Math.Round(pAmount / pQty, 2) : 0m;

            var sQty = daySupplies.Sum(x => x.Qty);
            var sAmount = daySupplies.Sum(x => x.Amount);
            var sAvgRate = sQty > 0 ? Math.Round(sAmount / sQty, 2) : 0m;

            var regSaleQty = dayRegularSales.Sum(x => x.Qty);
            var regSaleAmount = dayRegularSales.Sum(x => x.Amount);

            var totalDispatchedQty = sQty + regSaleQty;
            var diffQty = pQty - sQty;
            var diffAmount = sAmount - pAmount;
            var netDiffQty = pQty - totalDispatchedQty;

            string status;
            if (pQty == totalDispatchedQty)
                status = "Equal";
            else if (pQty > totalDispatchedQty)
                status = "Surplus";
            else
                status = "Shortage";

            lines.Add(new PurchaseSupplyComparisonLineResponse
            {
                Date = currentDate,
                DayName = currentDate.DayOfWeek.ToString(),
                PurchaseQty = pQty,
                PurchaseAvgRate = pAvgRate,
                PurchaseAmount = pAmount,
                SupplyQty = sQty,
                SupplyAvgRate = sAvgRate,
                SupplyAmount = sAmount,
                RegularSaleQty = regSaleQty,
                RegularSaleAmount = regSaleAmount,
                TotalDispatchedQty = totalDispatchedQty,
                DiffQty = diffQty,
                DiffAmount = diffAmount,
                NetDiffQty = netDiffQty,
                Status = status
            });

            currentDate = currentDate.AddDays(1);
        }

        var totalPurchaseQty = lines.Sum(x => x.PurchaseQty);
        var totalPurchaseAmount = lines.Sum(x => x.PurchaseAmount);
        var avgPurchaseRate = totalPurchaseQty > 0 ? Math.Round(totalPurchaseAmount / totalPurchaseQty, 2) : 0m;

        var totalSupplyQty = lines.Sum(x => x.SupplyQty);
        var totalSupplyAmount = lines.Sum(x => x.SupplyAmount);
        var avgSupplyRate = totalSupplyQty > 0 ? Math.Round(totalSupplyAmount / totalSupplyQty, 2) : 0m;

        var totalRegSaleQty = lines.Sum(x => x.RegularSaleQty);
        var totalRegSaleAmount = lines.Sum(x => x.RegularSaleAmount);
        var totalDispatched = lines.Sum(x => x.TotalDispatchedQty);

        var summary = new PurchaseSupplyComparisonSummaryResponse
        {
            TotalPurchaseQty = totalPurchaseQty,
            TotalPurchaseAmount = totalPurchaseAmount,
            AvgPurchaseRate = avgPurchaseRate,
            TotalSupplyQty = totalSupplyQty,
            TotalSupplyAmount = totalSupplyAmount,
            AvgSupplyRate = avgSupplyRate,
            TotalRegularSaleQty = totalRegSaleQty,
            TotalRegularSaleAmount = totalRegSaleAmount,
            TotalDispatchedQty = totalDispatched,
            TotalDiffQty = totalPurchaseQty - totalSupplyQty,
            TotalDiffAmount = totalSupplyAmount - totalPurchaseAmount,
            TotalNetDiffQty = totalPurchaseQty - totalDispatched
        };

        return new PurchaseSupplyComparisonResponse
        {
            ItemTitle = itemTitle,
            UnitTitle = unitTitle,
            Lines = lines,
            Summary = summary
        };
    }

    public async Task<CustomerBalanceRecoveryResponse> GetCustomerBalanceRecoveryAsync(CustomerBalanceRecoveryFilter filter, CancellationToken cancellationToken)
    {
        if (filter.ToDate < filter.FromDate)
            throw new BadRequestException("To date must be greater than or equal to from date.");

        bool useClearingDate = !string.Equals(filter.DateBasis, "VoucherDate", StringComparison.OrdinalIgnoreCase);

        // 1. Get Customer Account Prefix from DefaultAccount ("Customers")
        var defaultAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == "Customers")
            .Select(x => new { x.AccountId, x.MapAccountId })
            .FirstOrDefaultAsync(cancellationToken);

        var prefix = defaultAccount?.MapAccountId ?? defaultAccount?.AccountId;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new NotFoundException("Default account mapping for 'Customers' is not configured.");
        }

        // 2. Query ChartOfAccount for all Level 5 Customer Accounts
        var accountsQuery = _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.Id.StartsWith(prefix));

        if (!string.IsNullOrWhiteSpace(filter.CustomerAccountId))
        {
            accountsQuery = accountsQuery.Where(x => x.Id == filter.CustomerAccountId);
        }

        var accounts = await accountsQuery
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Title })
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
        {
            return new CustomerBalanceRecoveryResponse();
        }

        var accountIds = accounts.Select(x => x.Id).ToList();

        // 3. Left join with CustomerDetail for phone and address
        var customerDetails = await _customerDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Phone = x.Phone1 ?? x.Phone2 ?? x.SmsNumber,
                x.Address
            })
            .ToListAsync(cancellationToken);

        var detailMap = new Dictionary<string, (string? Phone, string? Address)>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in customerDetails)
        {
            if (!string.IsNullOrWhiteSpace(d.Id) && !detailMap.ContainsKey(d.Id))
            {
                detailMap[d.Id] = (d.Phone, d.Address);
            }
        }

        // 4. Fetch GL entries for these customer accounts
        var glEntries = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.DrAccountId!) || accountIds.Contains(x.CrAccountId!))
            .Select(x => new
            {
                x.DrAccountId,
                x.CrAccountId,
                x.Amount,
                x.VType,
                x.VDate,
                x.ClearingDate
            })
            .ToListAsync(cancellationToken);

        var prevBalanceMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var currentBillingMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var recoveryMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var gl in glEntries)
        {
            var effectiveDate = useClearingDate ? (gl.ClearingDate ?? gl.VDate) : gl.VDate;

            // A. Previous / Opening Balance
            if (gl.VType == "Op" || effectiveDate < filter.FromDate)
            {
                if (!string.IsNullOrWhiteSpace(gl.DrAccountId) && accountIds.Contains(gl.DrAccountId))
                {
                    prevBalanceMap.TryGetValue(gl.DrAccountId, out var cur);
                    prevBalanceMap[gl.DrAccountId] = cur + gl.Amount;
                }

                if (!string.IsNullOrWhiteSpace(gl.CrAccountId) && accountIds.Contains(gl.CrAccountId))
                {
                    prevBalanceMap.TryGetValue(gl.CrAccountId, out var cur);
                    prevBalanceMap[gl.CrAccountId] = cur - gl.Amount;
                }
            }
            // B. Current Period Activity
            else if (effectiveDate >= filter.FromDate && effectiveDate <= filter.ToDate)
            {
                if (!string.IsNullOrWhiteSpace(gl.DrAccountId) && accountIds.Contains(gl.DrAccountId))
                {
                    currentBillingMap.TryGetValue(gl.DrAccountId, out var cur);
                    currentBillingMap[gl.DrAccountId] = cur + gl.Amount;
                }

                if (!string.IsNullOrWhiteSpace(gl.CrAccountId) && accountIds.Contains(gl.CrAccountId))
                {
                    recoveryMap.TryGetValue(gl.CrAccountId, out var cur);
                    recoveryMap[gl.CrAccountId] = cur + gl.Amount;
                }
            }
        }

        // 5. Build Customer Lines
        var lines = new List<CustomerBalanceRecoveryLineResponse>();

        foreach (var acc in accounts)
        {
            var prevBalance = prevBalanceMap.GetValueOrDefault(acc.Id, 0m);
            var currentBilling = currentBillingMap.GetValueOrDefault(acc.Id, 0m);
            var totalDue = prevBalance + currentBilling;
            var recovery = recoveryMap.GetValueOrDefault(acc.Id, 0m);
            var discount = 0m;
            var closingBalance = totalDue - recovery - discount;

            var recoveryPercentage = totalDue > 0
                ? Math.Round(Math.Min(100m, Math.Max(0m, (recovery / totalDue) * 100m)), 1)
                : (closingBalance <= 0 && recovery > 0 ? 100m : 0m);

            string status;
            if (closingBalance == 0m && (totalDue > 0 || recovery > 0))
                status = "Cleared";
            else if (closingBalance < 0m)
                status = "Advance";
            else if (recovery > 0m && closingBalance > 0m)
                status = "Partial";
            else if (recovery == 0m && totalDue > 0m)
                status = "Unpaid";
            else
                status = "Cleared";

            detailMap.TryGetValue(acc.Id, out var det);

            lines.Add(new CustomerBalanceRecoveryLineResponse
            {
                CustomerAccountId = acc.Id,
                CustomerTitle = !string.IsNullOrWhiteSpace(acc.Title) ? acc.Title : acc.Id,
                Phone = det.Phone,
                Address = det.Address,
                PreviousBalance = prevBalance,
                CurrentBilling = currentBilling,
                TotalDue = totalDue,
                RecoveryAmount = recovery,
                Discount = discount,
                ClosingBalance = closingBalance,
                RecoveryPercentage = recoveryPercentage,
                Status = status
            });
        }

        // 6. Apply Balance Filter
        if (string.Equals(filter.BalanceFilter, "OutstandingOnly", StringComparison.OrdinalIgnoreCase))
        {
            lines = lines.Where(x => x.ClosingBalance > 0).ToList();
        }
        else if (string.Equals(filter.BalanceFilter, "ClearedOnly", StringComparison.OrdinalIgnoreCase))
        {
            lines = lines.Where(x => x.ClosingBalance <= 0).ToList();
        }
        else if (string.Equals(filter.BalanceFilter, "UnpaidOnly", StringComparison.OrdinalIgnoreCase))
        {
            lines = lines.Where(x => x.RecoveryAmount == 0 && x.TotalDue > 0).ToList();
        }

        lines = lines.OrderBy(x => x.CustomerTitle).ToList();

        var summary = new CustomerBalanceRecoverySummaryResponse
        {
            TotalCustomers = lines.Count,
            TotalPreviousBalance = lines.Sum(x => x.PreviousBalance),
            TotalCurrentBilling = lines.Sum(x => x.CurrentBilling),
            TotalDue = lines.Sum(x => x.TotalDue),
            TotalRecovery = lines.Sum(x => x.RecoveryAmount),
            TotalDiscount = lines.Sum(x => x.Discount),
            TotalClosingBalance = lines.Sum(x => x.ClosingBalance),
            OverallRecoveryRate = lines.Sum(x => x.TotalDue) > 0
                ? Math.Round(Math.Min(100m, Math.Max(0m, (lines.Sum(x => x.RecoveryAmount) / lines.Sum(x => x.TotalDue)) * 100m)), 1)
                : 0m
        };

        return new CustomerBalanceRecoveryResponse
        {
            Lines = lines,
            Summary = summary
        };
    }
}
