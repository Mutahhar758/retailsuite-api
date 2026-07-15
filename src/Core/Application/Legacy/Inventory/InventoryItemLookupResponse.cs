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
    public string? PrimaryUnit { get; set; }
    public string? SecondaryUnit { get; set; }
    public string? DefaultUnit { get; set; }
}
