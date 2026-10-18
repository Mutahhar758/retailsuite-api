using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("ItemCustomerCategoryPrice")]
public class ItemCustomerCategoryPrice : AuditableEntity, IAggregateRoot
{
    public string ItemId { get; set; } = default!;
    public string CustomerCategoryId { get; set; } = default!;
    public decimal PriRate { get; set; }
    public decimal? SecRate { get; set; }

    public ItemDetail? Item { get; set; }
    public CustomerCategory? CustomerCategory { get; set; }
}
