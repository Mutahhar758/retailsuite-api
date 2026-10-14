using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("BomDetail")]
public class BomDetail : AuditableEntity, IAggregateRoot
{
    public int BomMasterId { get; set; }
    public int Seq { get; set; }
    public string RawItemId { get; set; } = default!;
    public decimal Qty { get; set; }
    public string? UnitId { get; set; }
    public decimal ScrapPercentage { get; set; }
    public bool IsOptional { get; set; }

    public BomMaster? BomMaster { get; set; }
    public ItemDetail? RawItem { get; set; }
    public Unit? Unit { get; set; }
}
