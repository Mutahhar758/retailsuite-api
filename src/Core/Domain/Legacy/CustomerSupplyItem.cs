using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("CustomerSupplyItem")]
public class CustomerSupplyItem : AuditableEntity, IAggregateRoot
{
    public string CustomerAccountId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public decimal Qty { get; set; } = 1;
    public decimal? SecQty { get; set; }
    /// <summary>Override rate for this customer+item. Null means use the item's default rate.</summary>
    public decimal? Rate { get; set; }
    /// <summary>Override add/less amount for this customer+item. Null means no override (use 0).</summary>
    public decimal? AddLess { get; set; }
    /// <summary>Override discount (Rs) for this customer+item. Null means no override (use 0).</summary>
    public decimal? Discount { get; set; }

    public ChartOfAccount? CustomerAccount { get; set; }
    public ItemDetail? Item { get; set; }
}

