using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("ItemCatagory")]
public class ItemCategory : AuditableEntity<string>, IAggregateRoot
{
    public string Title { get; set; } = default!;
    public string? ItemType { get; set; }
    public bool Active { get; set; }
}
