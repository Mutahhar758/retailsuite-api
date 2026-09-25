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
    public string? SecUnitId { get; set; }
    public decimal? SecQtyIn { get; set; }
    public decimal? SecQtyOut { get; set; }
    public decimal? SecRate { get; set; }

    public decimal? CostPrice { get; set; }
    public decimal? CostAmount { get; set; }
    public decimal RemainingQty { get; set; }

    /// <summary>
    /// Running cumulative stock balance for this item after this transaction, ordered by (VDate, TranType in before out, Id).
    /// Maintained by FifoCostingService.RebuildItemFifoAsync — enables O(1) stock balance lookups
    /// instead of summing all transactions up to a date.
    /// </summary>
    public decimal RunningQtyBalance { get; set; }

    public ChartOfAccount? Account { get; set; }
    public ItemDetail? Item { get; set; }
    public Unit? Unit { get; set; }
    public Unit? SecUnit { get; set; }

    public ICollection<TransactionFifoMapping> OutFifoMappings { get; set; } = new List<TransactionFifoMapping>();
    public ICollection<TransactionFifoMapping> InFifoMappings { get; set; } = new List<TransactionFifoMapping>();
}
