using Retailer.Domain.Common.Enums;

namespace Retailer.Application.Legacy.Inventory;

public class InventoryItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string ItemCategoryCode { get; set; } = string.Empty;
    public string? ItemCategoryTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ItemKey { get; set; }
    public decimal PriRate { get; set; }
    public decimal SecRate { get; set; }
    public string? PrimaryUnit { get; set; }
    public string? SecondaryUnit { get; set; }
    public string? DefaultUnit { get; set; }
    public decimal? QtyInPack { get; set; }
    public bool Alert { get; set; }
    public bool? LowStockAlert { get; set; }
    public decimal? OpnStock { get; set; }
    public decimal? OpnRate { get; set; }
    public ItemType? ItemType { get; set; }
    public string? MediaId { get; set; }
    public string? MediaUrl { get; set; }
}


