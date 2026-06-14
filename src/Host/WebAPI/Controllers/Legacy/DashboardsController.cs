using Retailer.Application.Legacy.Dashboards;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class DashboardsController : VersionNeutralApiController
{
    private readonly IDashboardService _dashboardService;

    public DashboardsController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    [OpenApiOperation("Get dashboard summary statistics.", "")]
    public async Task<HttpResponseDto<DashboardStatsDto>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetStatsAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("sales-trend")]
    [OpenApiOperation("Get sales trend data for the last 7 months.", "")]
    public async Task<HttpResponseDto<List<SalesTrendDto>>> GetSalesTrendAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSalesTrendAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("cash-flow-trend")]
    [OpenApiOperation("Get cash flow trend data (In vs Out) for the last 7 months.", "")]
    public async Task<HttpResponseDto<List<CashFlowTrendDto>>> GetCashFlowTrendAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetCashFlowTrendAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("expenses-by-category")]
    [OpenApiOperation("Get breakdown of expenses by category for the current month.", "")]
    public async Task<HttpResponseDto<List<ExpenseCategoryDto>>> GetExpensesByCategoryAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetExpensesByCategoryAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("recent-expenses")]
    [OpenApiOperation("Get the most recent expense entries.", "")]
    public async Task<HttpResponseDto<List<RecentExpenseDto>>> GetRecentExpensesAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetRecentExpensesAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("stock-status")]
    [OpenApiOperation("Get top stock items by value and their status.", "")]
    public async Task<HttpResponseDto<List<StockStatusDto>>> GetStockStatusAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetStockStatusAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("recent-payments")]
    [OpenApiOperation("Get the most recent receipt and payment transactions.", "")]
    public async Task<HttpResponseDto<List<RecentPaymentDto>>> GetRecentPaymentsAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetRecentPaymentsAsync(cancellationToken);
        return result.ToInformationResponse();
    }
}
