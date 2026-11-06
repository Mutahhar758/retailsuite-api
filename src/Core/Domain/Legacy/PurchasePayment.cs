using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("PurchasePayment")]
public class PurchasePayment : AuditableEntity, IAggregateRoot
{
    public string VType { get; set; } = "PU";
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public string AccountId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? CheckNum { get; set; }
    public DateOnly? CheckDate { get; set; }

    public ChartOfAccount? Account { get; set; }
}
