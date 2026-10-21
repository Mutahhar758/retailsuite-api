namespace Retailer.Application.Legacy.Inventory;

public class InventoryItemLookupResponse
{
    public string Id { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ItemCategoryCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? ItemKey { get; set; }
    public decimal PriRate { get; set; }
    public decimal SecRate { get; set; }
    /// <summary>Only populated when the caller asks for it and has purchase view permission.</summary>
    public decimal? LastPurchaseRate { get; set; }
    public decimal? LastPurchaseSecRate { get; set; }
    public string? PrimaryUnit { get; set; }
    public string? PrimaryUnitTitle { get; set; }
    public string? SecondaryUnit { get; set; }
    public string? DefaultUnit { get; set; }
    public string? DefaultUnitTitle { get; set; }
    public decimal? QtyInPack { get; set; }
    public decimal? LowStockQty { get; set; }
    public decimal? MinSalePrice { get; set; }
    public string? MediaId { get; set; }
    public string? MediaUrl { get; set; }
    public string? QuickQtyPresets { get; set; }
    public string? AdditionalUnits { get; set; }
    public bool? RequireImei { get; set; }
    public string? BrandId { get; set; }
    public string? BrandTitle { get; set; }
    public string? ModelName { get; set; }
    public string? Storage { get; set; }
    public string? Ram { get; set; }
    public string? Color { get; set; }
}
