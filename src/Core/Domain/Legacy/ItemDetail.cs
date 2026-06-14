using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Common.Enums;

namespace Retailer.Domain.Legacy;

[Table("ItemDetail")]
public class ItemDetail : AuditableEntity<string>, IAggregateRoot
{
    public ItemType? ItemType { get; set; }
    public string? Barcode { get; set; }
    public string Title { get; set; } = default!;
    public string? ItemKey { get; set; }
    public decimal PriRate { get; set; }
    public decimal SecRate { get; set; }
    public string? PrimaryUnitId { get; set; }
    public string? SecondaryUnitId { get; set; }
    public string? DefaultUnitId { get; set; }
    public decimal? QtyInPack { get; set; }
    public bool? Alert { get; set; }
    public bool? LowStockAlert { get; set; }
    public decimal? OpnStock { get; set; }
    public decimal? OpnRate { get; set; }
    public string? MediaId { get; set; }

    public string? ItemCategoryId { get; set; }
    public ItemCategory? ItemCategory { get; set; }
    public Unit? PrimaryUnit { get; set; }
    public Unit? SecondaryUnit { get; set; }
    public Unit? DefaultUnit { get; set; }
}
