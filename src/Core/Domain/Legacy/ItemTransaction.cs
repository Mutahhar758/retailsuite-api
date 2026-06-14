using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("ItemTransaction")]
public class ItemTransaction : AuditableEntity, IAggregateRoot
{
    public DateOnly VDate { get; set; }
    public TimeOnly? VTime { get; set; }
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public string TranType { get; set; } = default!;
    public string? AccountId { get; set; }
    public string? ItemId { get; set; }
    public string? UnitId { get; set; }
    public decimal QtyIn { get; set; }
    public decimal QtyOut { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string? Counter { get; set; }

    public ChartOfAccount? Account { get; set; }
    public ItemDetail? Item { get; set; }
    public Unit? Unit { get; set; }
}
