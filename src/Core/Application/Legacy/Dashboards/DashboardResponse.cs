namespace Retailer.Application.Legacy.Dashboards;

public class DashboardResponse
{
    public decimal TotalSalesMTD { get; set; }
    public decimal CashInMTD { get; set; }
    public decimal CashOutMTD { get; set; }
    public decimal TotalStockValue { get; set; }

    public List<SalesTrendDto> SalesTrend { get; set; } = new();
    public List<CashFlowTrendDto> CashFlowTrend { get; set; } = new();
    public List<ExpenseCategoryDto> ExpensesByCategory { get; set; } = new();
    public List<RecentExpenseDto> RecentExpenses { get; set; } = new();
    public List<StockStatusDto> StockStatus { get; set; } = new();
    public List<RecentPaymentDto> RecentPayments { get; set; } = new();
}

public class SalesTrendDto
{
    public string Month { get; set; } = default!;
    public decimal Sales { get; set; }
}

public class CashFlowTrendDto
{
    public string Month { get; set; } = default!;
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
}

public class ExpenseCategoryDto
{
    public string Category { get; set; } = default!;
    public decimal Value { get; set; }
}

public class RecentExpenseDto
{
    public string Id { get; set; } = default!;
    public string Category { get; set; } = default!;
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
}

public class StockStatusDto
{
    public string Name { get; set; } = default!;
    public decimal Qty { get; set; }
    public decimal Value { get; set; }
    public string Status { get; set; } = default!;
}

public class RecentPaymentDto
{
    public string Id { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string Party { get; set; } = default!;
    public string Type { get; set; } = default!; // In or Out
    public decimal Amount { get; set; }
    public string Status { get; set; } = default!;
}
