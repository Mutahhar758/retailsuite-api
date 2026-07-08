using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("PurchaseRetDetail")]
public class PurchaseRetDetail : AuditableEntity, IAggregateRoot
{
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public string? UnitId { get; set; }
    public decimal? QtyInPack { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public string? SecUnitId { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }

    public int? PurchaseRetMasterId { get; set; }
    public string? ItemId { get; set; }
    public PurchaseRetMaster? PurchaseRetMaster { get; set; }
    public ItemDetail? Item { get; set; }
    public Unit? Unit { get; set; }
    public Unit? SecUnit { get; set; }
}
