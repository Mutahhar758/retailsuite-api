using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Dashboards;

public interface IDashboardService : ITransientService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken);
    Task<List<SalesTrendDto>> GetSalesTrendAsync(CancellationToken cancellationToken);
    Task<List<CashFlowTrendDto>> GetCashFlowTrendAsync(CancellationToken cancellationToken);
    Task<List<ExpenseCategoryDto>> GetExpensesByCategoryAsync(CancellationToken cancellationToken);
    Task<List<RecentExpenseDto>> GetRecentExpensesAsync(CancellationToken cancellationToken);
    Task<List<StockStatusDto>> GetStockStatusAsync(CancellationToken cancellationToken);
    Task<List<RecentPaymentDto>> GetRecentPaymentsAsync(CancellationToken cancellationToken);
}

public class DashboardStatsDto
{
    public decimal TotalSalesMTD { get; set; }
    public decimal CashInMTD { get; set; }
    public decimal CashOutMTD { get; set; }
    public decimal TotalStockValue { get; set; }
}
