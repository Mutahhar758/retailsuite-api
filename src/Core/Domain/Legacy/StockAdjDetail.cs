using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("StockAdjDetail")]
public class StockAdjDetail : AuditableEntity, IAggregateRoot
{
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal Rate { get; set; }
    public int? StockAdjMasterId { get; set; }
    public string? CategoryId { get; set; }
    public string? ItemId { get; set; }
    public StockAdjMaster? StockAdjMaster { get; set; }
    public ItemCategory? Category { get; set; }
    public ItemDetail? Item { get; set; }
}
