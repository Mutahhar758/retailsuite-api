using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("CustomerSupplyItem")]
public class CustomerSupplyItem : AuditableEntity, IAggregateRoot
{
    public string CustomerAccountId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public decimal Qty { get; set; } = 1;
    public decimal? SecQty { get; set; }

    public ChartOfAccount? CustomerAccount { get; set; }
    public ItemDetail? Item { get; set; }
}
