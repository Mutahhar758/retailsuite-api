namespace Retailer.Application.Legacy.Reports;

public class PurchaseSupplyComparisonResponse
{
    public string ItemTitle { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public List<PurchaseSupplyComparisonLineResponse> Lines { get; set; } = new();
    public PurchaseSupplyComparisonSummaryResponse Summary { get; set; } = new();
}

public class PurchaseSupplyComparisonLineResponse
{
    public DateOnly Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    
    // Purchase metrics
    public decimal PurchaseQty { get; set; }
    public decimal PurchaseAvgRate { get; set; }
    public decimal PurchaseAmount { get; set; }
    
    // Supply metrics
    public decimal SupplyQty { get; set; }
    public decimal SupplyAvgRate { get; set; }
    public decimal SupplyAmount { get; set; }
    
    // Regular counter sales (if any)
    public decimal RegularSaleQty { get; set; }
    public decimal RegularSaleAmount { get; set; }

    // Total dispatched = SupplyQty + RegularSaleQty
    public decimal TotalDispatchedQty { get; set; }
    
    // Comparison metrics: Purchase - Supply
    public decimal DiffQty { get; set; }
    public decimal DiffAmount { get; set; }
    
    // Net difference: Purchase - TotalDispatched
    public decimal NetDiffQty { get; set; }
    
    // Status label: "Balanced", "Surplus", "Shortage"
    public string Status { get; set; } = string.Empty;
}

public class PurchaseSupplyComparisonSummaryResponse
{
    public decimal TotalPurchaseQty { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public decimal AvgPurchaseRate { get; set; }

    public decimal TotalSupplyQty { get; set; }
    public decimal TotalSupplyAmount { get; set; }
    public decimal AvgSupplyRate { get; set; }

    public decimal TotalRegularSaleQty { get; set; }
    public decimal TotalRegularSaleAmount { get; set; }

    public decimal TotalDispatchedQty { get; set; }

    public decimal TotalDiffQty { get; set; }
    public decimal TotalDiffAmount { get; set; }
    public decimal TotalNetDiffQty { get; set; }
}
