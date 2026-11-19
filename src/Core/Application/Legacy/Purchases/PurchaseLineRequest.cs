using Retailer.Application.Legacy.Inventory;

namespace Retailer.Application.Legacy.Purchases;

public class PurchaseLineRequest
{
    public int Seq { get; set; }
    public string ItemId { get; set; } = default!;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal AddLess { get; set; }
    public string? SecUnit { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }
    public decimal? QtyInPack { get; set; }
    public decimal? Packing { get; set; }
    public string? Imei { get; set; }
    public string? Imei2 { get; set; }
    public string? PtaStatus { get; set; }
    public string? ConditionNote { get; set; }
    public int? BatteryHealth { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? SecSalePrice { get; set; }
    public decimal? MinSalePrice { get; set; }
    public List<ItemCustomerCategoryPriceUpsertRequest>? CustomerCategoryPrices { get; set; }
}
