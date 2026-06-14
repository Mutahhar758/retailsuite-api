namespace Retailer.Application.Legacy.SupplyOrders;

public class SupplyOrderResponse
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public List<SupplyOrderDetailResponse> Details { get; set; } = [];
}

public class SupplyOrderDetailResponse
{
    public string? CustomerId { get; set; }
    public int SortOrder { get; set; }
}
