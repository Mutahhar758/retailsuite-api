namespace Retailer.Application.Legacy.DailyStockClosings;

public class DailyStockClosingListItemResponse
{
    public string ClosingKey { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string? PurchaseVoucherNo { get; set; }
    public decimal PurchaseAmount { get; set; }
    public string? SaleVoucherNo { get; set; }
    public decimal SaleAmount { get; set; }
    public string? Remarks { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class DailyStockClosingSheetResponse
{
    public DateOnly Date { get; set; }
    public bool IsExisting { get; set; }
    public string? PurchaseVoucherNo { get; set; }
    public string? SaleVoucherNo { get; set; }
    public string? CustomerAccountId { get; set; }
    public string? VendorAccountId { get; set; }
    public string? CashAccountId { get; set; }
    public string ReconciliationMode { get; set; } = "DiscountAddLess";
    public decimal? EnteredSaleAmount { get; set; }
    public decimal? EnteredPurchaseAmount { get; set; }
    public string? Remarks { get; set; }
    public List<DailyStockClosingItemLineDto> Items { get; set; } = [];
    public List<DailyStockClosingCreditLineDto> CreditCustomers { get; set; } = [];
    public List<DailyStockClosingCreditLineDto> CreditSuppliers { get; set; } = [];
}

public class DailyStockClosingItemLineDto
{
    public string ItemId { get; set; } = default!;
    public string? ItemKey { get; set; }
    public string? Barcode { get; set; }
    public string Title { get; set; } = default!;
    public string CategoryCode { get; set; } = string.Empty;
    public string? CategoryTitle { get; set; }
    public string? UnitId { get; set; }
    public string? UnitTitle { get; set; }
    public decimal OpeningQty { get; set; }
    public decimal ClosingQty { get; set; }
    public decimal PurchasedQty { get; set; }
    public decimal SoldQty { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
}

public class DailyStockClosingCreditLineDto
{
    public int Seq { get; set; }
    public string AccountId { get; set; } = default!;
    public string? AccountTitle { get; set; }
    public decimal Amount { get; set; }
    public string? Remarks { get; set; }
}

public class DailyStockClosingSaveRequest
{
    public DateOnly Date { get; set; }
    public string CustomerAccountId { get; set; } = default!;
    public string VendorAccountId { get; set; } = default!;
    public string? CashAccountId { get; set; }
    public string ReconciliationMode { get; set; } = "DiscountAddLess";
    public decimal? EnteredSaleAmount { get; set; }
    public decimal? EnteredPurchaseAmount { get; set; }
    public string? Remarks { get; set; }
    public List<DailyStockClosingSaveLineRequest> Lines { get; set; } = [];
    public List<DailyStockClosingCreditLineDto>? CreditCustomers { get; set; }
    public List<DailyStockClosingCreditLineDto>? CreditSuppliers { get; set; }
}

public class DailyStockClosingSaveLineRequest
{
    public string ItemId { get; set; } = default!;
    public decimal OpeningQty { get; set; }
    public decimal ClosingQty { get; set; }
    public decimal PurchasedQty { get; set; }
    public decimal SoldQty { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
}

public class DailyStockClosingSaveResponse
{
    public DateOnly Date { get; set; }
    public string? PurchaseVoucherNo { get; set; }
    public string? SaleVoucherNo { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public decimal TotalSaleAmount { get; set; }
    public decimal CashReceived { get; set; }
    public decimal CashPaid { get; set; }
}
