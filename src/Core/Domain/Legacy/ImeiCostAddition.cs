using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("ImeiCostAddition")]
public class ImeiCostAddition : BaseEntity<long>, IAggregateRoot
{
    public string Imei { get; set; } = default!;
    public DateOnly Date { get; set; }
    public string ExpenseType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? PaidFromAccount { get; set; }
    public string? ConsumedItemId { get; set; }
    public decimal? ConsumedQty { get; set; } = 1;

    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public ChartOfAccount? PaidFrom { get; set; }
    public ItemDetail? ConsumedItem { get; set; }
}
