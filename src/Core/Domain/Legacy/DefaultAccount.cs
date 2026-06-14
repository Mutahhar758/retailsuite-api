using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("DefaultAccount")]
public class DefaultAccount : AuditableEntity, IAggregateRoot
{
    public string? Title { get; set; }
    public string? A { get; set; }
    public string? AccountId { get; set; }
    public string? MapAccountId { get; set; }
    public ChartOfAccount? Account { get; set; }
    public ChartOfAccount? MapAccount { get; set; }
}
