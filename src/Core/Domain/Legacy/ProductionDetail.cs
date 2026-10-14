using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("ProductionDetail")]
public class ProductionDetail : AuditableEntity, IAggregateRoot
{
    public int ProductionMasterId { get; set; }
    public string VType { get; set; } = "MF";
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public string RawItemId { get; set; } = default!;
    public decimal PlannedQty { get; set; }
    public decimal ActualQty { get; set; }
    public string? UnitId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsIssued { get; set; } = true;

    public ProductionMaster? ProductionMaster { get; set; }
    public ItemDetail? RawItem { get; set; }
    public Unit? Unit { get; set; }
}
