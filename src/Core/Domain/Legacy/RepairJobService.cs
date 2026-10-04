using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("RepairJobService")]
public class RepairJobServiceItem : BaseEntity<long>, IAggregateRoot
{
    public string JobNo { get; set; } = default!;
    public string? ServiceItemId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public decimal TechnicianShare { get; set; }

    public RepairJob? RepairJob { get; set; }
    public ItemDetail? ServiceItem { get; set; }
}
