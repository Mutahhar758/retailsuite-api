using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Common.Enums;

namespace Retailer.Domain.Legacy;

[Table("ProductionMaster")]
public class ProductionMaster : AuditableEntity, IAggregateRoot
{
    public DateOnly VDate { get; set; }
    public TimeOnly VTime { get; set; }
    public string VType { get; set; } = "MF";
    public string VNo { get; set; } = default!;
    public int? BomMasterId { get; set; }
    public string FinishedItemId { get; set; } = default!;
    public decimal TargetQty { get; set; }
    public decimal ProducedQty { get; set; }
    public string? UnitId { get; set; }
    public string? BatchNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalOverheadCost { get; set; }
    public decimal TotalProductionCost { get; set; }
    public decimal UnitProductionCost { get; set; }
    public string? NarrationId { get; set; }
    public string? Remarks { get; set; }
    public ProductionStatus Status { get; set; } = ProductionStatus.Completed;
    public string? CurrentStage { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public BomMaster? BomMaster { get; set; }
    public ItemDetail? FinishedItem { get; set; }
    public Unit? Unit { get; set; }
    public Narration? Narration { get; set; }
    public ICollection<ProductionDetail> Details { get; set; } = new List<ProductionDetail>();
}
