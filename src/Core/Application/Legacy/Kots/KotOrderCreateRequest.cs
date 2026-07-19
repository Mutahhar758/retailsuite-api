namespace Retailer.Application.Legacy.Kots;

public class KotOrderItemRequest
{
    public string ItemId { get; set; } = default!;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public string? Notes { get; set; }
}

public class KotOrderCreateRequest
{
    public string OrderType { get; set; } = "Takeaway"; // Takeaway, DineIn, Delivery
    public int? TableId { get; set; }
    public string? CustomerId { get; set; }
    public string? Remarks { get; set; }
    public List<KotOrderItemRequest> Lines { get; set; } = [];
}
