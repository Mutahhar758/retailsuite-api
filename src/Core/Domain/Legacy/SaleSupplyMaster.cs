using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("SaleSupplyMaster")]
public class SaleSupplyMaster : AuditableEntity, IAggregateRoot
{
    public DateOnly VDate { get; set; }
    public TimeOnly VTime { get; set; }
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public string? Descr { get; set; }
    public string? NarrationId { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Discount { get; set; }
    public decimal? NetAmount { get; set; }
    public string? Counter { get; set; }

    public string? ItemId { get; set; }
    public ItemDetail? Item { get; set; }
    public Narration? Narration { get; set; }
    public ICollection<SaleSupplyDetail> Details { get; set; } = new List<SaleSupplyDetail>();
}
