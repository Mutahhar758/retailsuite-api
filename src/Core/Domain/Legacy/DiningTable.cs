using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("DiningTables")]
public class DiningTable : AuditableEntity, IAggregateRoot
{
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public string Status { get; set; } = "Available"; // Available, Occupied, Reserved
    public bool Active { get; set; }
}
