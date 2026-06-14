using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("SaleRetDetail")]
public class SaleRetDetail : AuditableEntity, IAggregateRoot
{
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public string? UnitId { get; set; }
    public decimal? QtyInPack { get; set; }
    public decimal Qty { get; set; }
    public decimal? GrossRate { get; set; }
    public decimal? Discount { get; set; }

    public int? SaleRetMasterId { get; set; }
    public string? ItemId { get; set; }
    public SaleRetMaster? SaleRetMaster { get; set; }
    public ItemDetail? Item { get; set; }
    public Unit? Unit { get; set; }
}
