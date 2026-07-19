namespace Retailer.Application.Legacy.Kots;

public class KotOrderItemResponse
{
    public int Id { get; set; }
    public string ItemId { get; set; } = default!;
    public string ItemTitle { get; set; } = default!;
    public string? ItemCategoryCode { get; set; }
    public string? PrepStationId { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = default!;
}

public class KotOrderResponse
{
    public int Id { get; set; }
    public int TokenNo { get; set; }
    public DateOnly OrderDate { get; set; }
    public TimeOnly OrderTime { get; set; }
    public string OrderType { get; set; } = default!;
    public int? TableId { get; set; }
    public string? TableName { get; set; }
    public string Status { get; set; } = default!;
    public string? SaleVoucherNo { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Remarks { get; set; }
    
    public List<KotOrderItemResponse> Lines { get; set; } = [];
    
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
}
