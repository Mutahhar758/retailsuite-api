using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("RepairJobPart")]
public class RepairJobPart : BaseEntity<long>, IAggregateRoot
{
    public string JobNo { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public decimal Qty { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal CostRate { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public RepairJob? RepairJob { get; set; }
    public ItemDetail? Item { get; set; }
}
