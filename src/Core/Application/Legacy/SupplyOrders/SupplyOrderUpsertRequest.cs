namespace Retailer.Application.Legacy.SupplyOrders;

public class SupplyOrderUpsertRequest
{
    public string Title { get; set; } = string.Empty;
    public List<SupplyOrderDetailUpsertRequest> Details { get; set; } = [];
}

public class SupplyOrderDetailUpsertRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
