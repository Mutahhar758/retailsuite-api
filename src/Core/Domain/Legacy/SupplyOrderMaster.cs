using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("SupplyOrderMaster")]
public class SupplyOrderMaster : AuditableEntity, IAggregateRoot
{
    public string? Title { get; set; }

    public ICollection<SupplyOrderDetail> Details { get; set; } = new List<SupplyOrderDetail>();
}
