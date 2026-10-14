using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("BomMaster")]
public class BomMaster : AuditableEntity, IAggregateRoot
{
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string FinishedItemId { get; set; } = default!;
    public decimal OutputQty { get; set; } = 1.0m;
    public string? UnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = true;
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackagingCost { get; set; }
    public string? Notes { get; set; }

    public ItemDetail? FinishedItem { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<BomDetail> Details { get; set; } = new List<BomDetail>();
}
