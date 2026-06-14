using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("StockAdjMaster")]
public class StockAdjMaster : AuditableEntity, IAggregateRoot
{
    public DateOnly VDate { get; set; }
    public TimeOnly VTime { get; set; }
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public string? Descr { get; set; }
    public string? NarrationId { get; set; }
    public string? Terminal { get; set; }

    public Narration? Narration { get; set; }
    public ICollection<StockAdjDetail> Details { get; set; } = new List<StockAdjDetail>();
}
