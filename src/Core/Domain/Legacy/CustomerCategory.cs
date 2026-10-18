using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("CustomerCategory")]
public class CustomerCategory : AuditableEntity<string>, IAggregateRoot
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
}
