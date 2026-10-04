using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("Brand")]
public class Brand : AuditableEntity<string>, IAggregateRoot
{
    public string Title { get; set; } = default!;
    public bool Active { get; set; } = true;
}
