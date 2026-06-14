using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("PurchaseMaster")]
public class PurchaseMaster : AuditableEntity, IAggregateRoot
{
    public DateOnly VDate { get; set; }
    public TimeOnly VTime { get; set; }
    public string VType { get; set; } = default!;
    public string VNo { get; set; } = default!;
    public string? Descr { get; set; }
    public string? NarrationId { get; set; }
    public decimal? Amount { get; set; }
    public string? Counter { get; set; }

    public string? AccountId { get; set; }
    public ChartOfAccount? Account { get; set; }
    public Narration? Narration { get; set; }
    public ICollection<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
}
