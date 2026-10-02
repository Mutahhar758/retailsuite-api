namespace Retailer.Application.Legacy.MobileShop;

// ---------------------------------------------------------------------------
// IMEI Stock & History DTOs
// ---------------------------------------------------------------------------

public class ImeiStockResponse
{
    public string Imei { get; set; } = default!;
    public string? Imei2 { get; set; }
    public string ItemId { get; set; } = default!;
    public string ItemTitle { get; set; } = default!;
    public string? BrandTitle { get; set; }
    public string? ModelName { get; set; }
    public string? Storage { get; set; }
    public string? Ram { get; set; }
    public string? Color { get; set; }
    public string? PtaStatus { get; set; }
    public string? ConditionNote { get; set; }
    public int? BatteryHealth { get; set; }
    public string PurchaseVNo { get; set; } = default!;
    public DateOnly PurchaseDate { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal AddedCost { get; set; }
    public decimal TotalLandedCost => PurchaseRate + AddedCost;
    public bool IsInStock { get; set; } = true;
    public string? SaleVNo { get; set; }
    public DateOnly? SaleDate { get; set; }
    public decimal? SaleRate { get; set; }
}

public class ImeiHistoryResponse
{
    public string Imei { get; set; } = default!;
    public string? Imei2 { get; set; }
    public string ItemTitle { get; set; } = default!;
    public string? BrandTitle { get; set; }
    public string? ModelName { get; set; }
    public string? CurrentPtaStatus { get; set; }
    public string? CurrentCondition { get; set; }
    public bool IsInStock { get; set; }

    public ImeiTransactionInfo? PurchasedIn { get; set; }
    public ImeiTransactionInfo? SoldIn { get; set; }
    public ImeiTransactionInfo? ReturnedIn { get; set; }
    public List<ImeiCostAdditionResponse> CostAdditions { get; set; } = new();
    public List<RepairJobSummaryResponse> RepairJobs { get; set; } = new();
}

public class ImeiTransactionInfo
{
    public string VoucherType { get; set; } = default!;
    public string VoucherNo { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string AccountId { get; set; } = default!;
    public string? AccountTitle { get; set; }
    public decimal Rate { get; set; }
    public string? PtaStatus { get; set; }
    public string? ConditionNote { get; set; }
    public int? WarrantyMonths { get; set; }
    public DateOnly? WarrantyExpiryDate { get; set; }
}

public class ImeiCostAdditionRequest
{
    public string Imei { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string ExpenseType { get; set; } = "PtaTax"; // PtaTax, CpidServer, SparePart, Labour
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? PaidFromAccount { get; set; }
    public string? ConsumedItemId { get; set; }
    public decimal? ConsumedQty { get; set; } = 1;
    public string? NewPtaStatus { get; set; } // If PTA tax paid, update to Official or CPID
    public string? NewCondition { get; set; } // e.g. Refurbished / Good
}

public class ImeiCostAdditionResponse
{
    public long Id { get; set; }
    public string Imei { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string ExpenseType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? PaidFromAccount { get; set; }
    public string? PaidFromTitle { get; set; }
    public string? ConsumedItemId { get; set; }
    public string? ConsumedItemTitle { get; set; }
    public decimal? ConsumedQty { get; set; }
    public DateTime CreatedOn { get; set; }
}

// ---------------------------------------------------------------------------
// Repair Job DTOs
// ---------------------------------------------------------------------------

public class RepairJobListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Status { get; set; }
    public string? CustomerAcc { get; set; }
    public string? SearchText { get; set; } // Searches JobNo, IMEI, CustomerName, Phone
}

public class RepairJobResponse
{
    public string JobNo { get; set; } = default!;
    public DateOnly JobDate { get; set; }
    public string CustomerAcc { get; set; } = default!;
    public string? CustomerAccountTitle { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? BrandId { get; set; }
    public string? BrandTitle { get; set; }
    public string DeviceModel { get; set; } = default!;
    public string? Imei { get; set; }
    public string? PasscodeOrPattern { get; set; }
    public string FaultDescription { get; set; } = default!;
    public string? PhysicalCondition { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AdvancePaid { get; set; }
    public string Status { get; set; } = "Received";
    public string? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public DateTime? DeliveredOn { get; set; }
    public string? SaleVNo { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedOn { get; set; }

    public List<RepairJobPartResponse> Parts { get; set; } = new();
    public List<RepairJobServiceResponse> Services { get; set; } = new();

    public decimal TotalPartsAmount => Parts.Sum(p => p.Amount);
    public decimal TotalServicesAmount => Services.Sum(s => s.Amount);
    public decimal TotalAmount => TotalPartsAmount + TotalServicesAmount;
    public decimal BalanceDue => TotalAmount - AdvancePaid;
}

public class RepairJobSummaryResponse
{
    public string JobNo { get; set; } = default!;
    public DateOnly JobDate { get; set; }
    public string DeviceModel { get; set; } = default!;
    public string FaultDescription { get; set; } = default!;
    public string Status { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string? SaleVNo { get; set; }
}

public class RepairJobPartResponse
{
    public long Id { get; set; }
    public string ItemId { get; set; } = default!;
    public string ItemTitle { get; set; } = default!;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount => Qty * Rate;
}

public class RepairJobServiceResponse
{
    public long Id { get; set; }
    public string? ServiceItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TechnicianShare { get; set; }
}

public class RepairJobCreateRequest
{
    public DateOnly JobDate { get; set; }
    public string CustomerAcc { get; set; } = default!;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? BrandId { get; set; }
    public string DeviceModel { get; set; } = default!;
    public string? Imei { get; set; }
    public string? PasscodeOrPattern { get; set; }
    public string FaultDescription { get; set; } = default!;
    public string? PhysicalCondition { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AdvancePaid { get; set; }
    public string? AssignedTechnicianId { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public string? Remarks { get; set; }

    public List<RepairJobPartRequest> Parts { get; set; } = new();
    public List<RepairJobServiceRequest> Services { get; set; } = new();
}

public class RepairJobUpdateRequest
{
    public DateOnly JobDate { get; set; }
    public string CustomerAcc { get; set; } = default!;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? BrandId { get; set; }
    public string DeviceModel { get; set; } = default!;
    public string? Imei { get; set; }
    public string? PasscodeOrPattern { get; set; }
    public string FaultDescription { get; set; } = default!;
    public string? PhysicalCondition { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal AdvancePaid { get; set; }
    public string Status { get; set; } = "Received";
    public string? AssignedTechnicianId { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public string? Remarks { get; set; }

    public List<RepairJobPartRequest> Parts { get; set; } = new();
    public List<RepairJobServiceRequest> Services { get; set; } = new();
}

public class RepairJobPartRequest
{
    public string ItemId { get; set; } = default!;
    public decimal Qty { get; set; } = 1;
    public decimal Rate { get; set; }
}

public class RepairJobServiceRequest
{
    public string? ServiceItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TechnicianShare { get; set; }
}

public class RepairJobStatusUpdateRequest
{
    public string Status { get; set; } = default!;
    public string? Remarks { get; set; }
}

public class RepairJobBillRequest
{
    public decimal CashReceipt { get; set; }
    public decimal Discount { get; set; }
    public string? NarrationId { get; set; }
    public string? Description { get; set; }
}
