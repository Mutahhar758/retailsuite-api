namespace Retailer.Application.Legacy.AccountingPeriods;

public class PeriodLockStatusResponse
{
    public DateOnly? LockDate { get; set; }
    public int? LastClosedPeriodId { get; set; }
    public string? LastClosedPeriodName { get; set; }
    public DateOnly? NextOpenDate { get; set; }
    public string DefaultTargetEquityAccountId { get; set; } = default!;
    public string DefaultTargetEquityAccountTitle { get; set; } = default!;
}

public class AccountingPeriodCloseResponse
{
    public int Id { get; set; }
    public string PeriodName { get; set; } = default!;
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly ClosingDate { get; set; }
    public string Status { get; set; } = default!;
    public string ClosingVoucherNo { get; set; } = default!;
    public string TargetEquityAccountId { get; set; } = default!;
    public string TargetEquityAccountTitle { get; set; } = default!;
    public bool ClosedDrawings { get; set; }

    public decimal OpeningStockValue { get; set; }
    public decimal ClosingStockValue { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfitLoss { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public bool IsBalanced { get; set; }
    public bool CanReopen { get; set; }

    public string? Notes { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime ClosedOn { get; set; }
    public string? ReopenedBy { get; set; }
    public DateTime? ReopenedOn { get; set; }
    public string? ReopenReason { get; set; }
}

public class AccountingPeriodCloseDetailResponse : AccountingPeriodCloseResponse
{
    public List<ClosingVoucherLineDto> ClosingEntries { get; set; } = new();
}

public class ClosingVoucherLineDto
{
    public int Seq { get; set; }
    public string DrAccountId { get; set; } = default!;
    public string DrAccountTitle { get; set; } = default!;
    public string CrAccountId { get; set; } = default!;
    public string CrAccountTitle { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Remarks { get; set; } = default!;
}

public class PeriodClosePreviewRequest
{
    public DateOnly ClosingDate { get; set; }
    public string? TargetEquityAccountId { get; set; }
    public bool CloseDrawings { get; set; } = true;
}

public class PeriodClosePreviewResponse
{
    public DateOnly PeriodStartDate { get; set; }
    public DateOnly ClosingDate { get; set; }
    public string TargetEquityAccountId { get; set; } = default!;
    public string TargetEquityAccountTitle { get; set; } = default!;
    public bool CloseDrawings { get; set; }

    public decimal OpeningStockValue { get; set; }
    public decimal ClosingStockValue { get; set; }
    public decimal CurrentGlInventoryBalance { get; set; }
    public decimal InventoryAdjustment { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalCogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfitLoss { get; set; }
    public decimal TotalDrawingsClosed { get; set; }

    public decimal PostCloseTotalAssets { get; set; }
    public decimal PostCloseTotalLiabilities { get; set; }
    public decimal PostCloseTotalEquity { get; set; }
    public decimal BalanceSheetDifference { get; set; }
    public bool IsBalanced { get; set; }

    public bool CanClose { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public List<ClosingVoucherLineDto> ProposedEntries { get; set; } = new();
}

public class PeriodCloseExecuteRequest
{
    public string PeriodName { get; set; } = default!;
    public DateOnly ClosingDate { get; set; }
    public string? TargetEquityAccountId { get; set; }
    public bool CloseDrawings { get; set; } = true;
    public string? Notes { get; set; }
}

public class PeriodReopenRequest
{
    public string Reason { get; set; } = default!;
}
