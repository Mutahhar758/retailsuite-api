using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("PrepStations")]
public class PrepStation : AuditableEntity<string>, IAggregateRoot
{
    public string Name { get; set; } = default!;
    public bool Active { get; set; }
}
