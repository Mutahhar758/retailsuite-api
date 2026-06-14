using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Dashboards;
using Retailer.Domain.Legacy;
using Retailer.Domain.Common.Enums;
using System.Globalization;

namespace Retailer.Infrastructure.Legacy.Dashboards;

internal class DashboardService : IDashboardService
{
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<SaleMaster> _saleMasterRepository;
    private readonly IRepository<PurchaseMaster> _purchaseMasterRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<ItemDetail> _itemDetailRepository;

    public DashboardService(
        IRepository<GlEntry> glRepository,
        IRepository<SaleMaster> saleMasterRepository,
        IRepository<PurchaseMaster> purchaseMasterRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<ItemDetail> itemDetailRepository)
    {
        _glRepository = glRepository;
        _saleMasterRepository = saleMasterRepository;
        _purchaseMasterRepository = purchaseMasterRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
        _itemDetailRepository = itemDetailRepository;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);

        var totalSalesMTD = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => ((x.VType == "SL" && x.VSeq == 1) || x.VType == "SP") && x.VDate >= firstDayOfMonth && x.VDate <= today)
            .SumAsync(x => x.Amount, cancellationToken);

        var cashInMTD = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "RV" && x.VDate >= firstDayOfMonth && x.VDate <= today)
            .SumAsync(x => x.Amount, cancellationToken);

        var cashOutMTD = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PV" && x.VDate >= firstDayOfMonth && x.VDate <= today)
            .SumAsync(x => x.Amount, cancellationToken);

        var stockData = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate <= today || x.VType == "Op")
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .Where(x => x.Item != null && x.Item.ItemType != ItemType.Service)
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                QtyBal = g.Sum(x => x.QtyIn - x.QtyOut),
                PosQtyBal = g.Where(x => (x.QtyIn - x.QtyOut) > 0).Sum(x => (decimal?)x.QtyIn - (decimal?)x.QtyOut) ?? 0,
                Amt = g.Where(x => x.TranType == "in").Sum(x => ((decimal?)x.QtyIn - (decimal?)x.QtyOut) * (decimal?)x.Rate) ?? 0
            })
            .ToListAsync(cancellationToken);

        var totalStockValue = stockData.Sum(x => x.QtyBal * (x.PosQtyBal == 0 ? 0 : x.Amt / x.PosQtyBal));

        return new DashboardStatsDto
        {
            TotalSalesMTD = totalSalesMTD,
            CashInMTD = cashInMTD,
            CashOutMTD = cashOutMTD,
            TotalStockValue = totalStockValue
        };
    }

    public async Task<List<SalesTrendDto>> GetSalesTrendAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var last7Months = Enumerable.Range(0, 7)
            .Select(i => today.AddMonths(-i))
            .OrderBy(d => d)
            .ToList();
        
        var startOfRange = new DateOnly(last7Months[0].Year, last7Months[0].Month, 1);

        var salesTrendData = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => ((x.VType == "SL" && x.VSeq == 1) || x.VType == "SP") && x.VDate >= startOfRange && x.VDate <= today)
            .GroupBy(x => new { x.VDate.Year, x.VDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return last7Months.Select(m => new SalesTrendDto
        {
            Month = m.ToString("MMM"),
            Sales = salesTrendData.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Total ?? 0
        }).ToList();
    }

    public async Task<List<CashFlowTrendDto>> GetCashFlowTrendAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var last7Months = Enumerable.Range(0, 7)
            .Select(i => today.AddMonths(-i))
            .OrderBy(d => d)
            .ToList();
        
        var startOfRange = new DateOnly(last7Months[0].Year, last7Months[0].Month, 1);

        var cashInTrendData = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "RV" && x.VDate >= startOfRange && x.VDate <= today)
            .GroupBy(x => new { x.VDate.Year, x.VDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var cashOutTrendData = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PV" && x.VDate >= startOfRange && x.VDate <= today)
            .GroupBy(x => new { x.VDate.Year, x.VDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return last7Months.Select(m => new CashFlowTrendDto
        {
            Month = m.ToString("MMM"),
            CashIn = cashInTrendData.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Total ?? 0,
            CashOut = cashOutTrendData.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Total ?? 0
        }).ToList();
    }

    public async Task<List<ExpenseCategoryDto>> GetExpensesByCategoryAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);
        var allAccounts = await _chartOfAccountRepository.GetAll().AsNoTracking().ToListAsync(cancellationToken);
        
        var expenseDetailAccounts = allAccounts
            .Where(x => IsDescendantOf(x, "004", allAccounts) && x.AccLevel == 5)
            .ToList();
        var expenseAccountIds = expenseDetailAccounts.Select(x => x.Id).ToList();

        var expenseEntries = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate >= firstDayOfMonth && x.VDate <= today)
            .Where(x => expenseAccountIds.Contains(x.DrAccountId!))
            .GroupBy(x => x.DrAccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return expenseEntries
            .Select(g => new ExpenseCategoryDto
            {
                Category = allAccounts.First(a => a.Id == g.AccountId).Title,
                Value = g.Total
            })
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToList();
    }

    public async Task<List<RecentExpenseDto>> GetRecentExpensesAsync(CancellationToken cancellationToken)
    {
        var allAccounts = await _chartOfAccountRepository.GetAll().AsNoTracking().ToListAsync(cancellationToken);
        var expenseDetailAccounts = allAccounts
            .Where(x => IsDescendantOf(x, "004", allAccounts) && x.AccLevel == 5)
            .ToList();
        var expenseAccountIds = expenseDetailAccounts.Select(x => x.Id).ToList();

        return await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => expenseAccountIds.Contains(x.DrAccountId!))
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.CreatedOn)
            .Take(5)
            .Select(x => new RecentExpenseDto
            {
                Id = x.VoucherNo,
                Category = x.DrAccount != null ? x.DrAccount.Title : x.DrAccountId!,
                Date = x.VDate,
                Amount = x.Amount
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<StockStatusDto>> GetStockStatusAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var stockData = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VDate <= today || x.VType == "Op")
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .Where(x => x.Item != null && x.Item.ItemType != ItemType.Service)
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                QtyBal = g.Sum(x => x.QtyIn - x.QtyOut),
                PosQtyBal = g.Where(x => (x.QtyIn - x.QtyOut) > 0).Sum(x => (decimal?)x.QtyIn - (decimal?)x.QtyOut) ?? 0,
                Amt = g.Where(x => x.TranType == "in").Sum(x => ((decimal?)x.QtyIn - (decimal?)x.QtyOut) * (decimal?)x.Rate) ?? 0
            })
            .ToListAsync(cancellationToken);

        var itemInfos = await _itemDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.ItemType != ItemType.Service)
            .Select(x => new { x.Id, x.Title })
            .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        return stockData
            .Where(x => itemInfos.ContainsKey(x.ItemId!))
            .Select(x =>
            {
                var info = itemInfos[x.ItemId!];
                var rate = x.PosQtyBal == 0 ? 0 : x.Amt / x.PosQtyBal;
                return new StockStatusDto
                {
                    Name = info.Title,
                    Qty = x.QtyBal,
                    Value = x.QtyBal * rate,
                    Status = x.QtyBal > 10 ? "In Stock" : (x.QtyBal > 0 ? "Low Stock" : "Out of Stock")
                };
            })
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToList();
    }

    public async Task<List<RecentPaymentDto>> GetRecentPaymentsAsync(CancellationToken cancellationToken)
    {
        return await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "PV" || x.VType == "RV")
            .OrderByDescending(x => x.VDate)
            .ThenByDescending(x => x.CreatedOn)
            .Take(5)
            .Select(x => new RecentPaymentDto
            {
                Id = x.VType + "-" + x.VoucherNo,
                Date = x.VDate,
                Party = x.VType == "PV" 
                    ? (x.DrAccount != null ? x.DrAccount.Title : x.DrAccountId!)
                    : (x.CrAccount != null ? x.CrAccount.Title : x.CrAccountId!),
                Type = x.VType == "RV" ? "In" : "Out",
                Amount = x.Amount,
                Status = "Completed"
            })
            .ToListAsync(cancellationToken);
    }

    private bool IsDescendantOf(ChartOfAccount account, string rootId, List<ChartOfAccount> allAccounts)
    {
        var current = account;
        while (current != null)
        {
            if (current.Id == rootId) return true;
            if (string.IsNullOrEmpty(current.ParentId)) break;
            current = allAccounts.FirstOrDefault(a => a.Id == current.ParentId);
        }
        return false;
    }
}
