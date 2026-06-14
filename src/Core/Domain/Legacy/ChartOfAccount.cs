using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("ChartOfAccount")]
public class ChartOfAccount : AuditableEntity<string>, IAggregateRoot
{
    public string Title { get; set; } = default!;
    public string? ParentId { get; set; } = default!;
    public string AccType { get; set; } = default!;
    public int AccLevel { get; set; }
    public ChartOfAccount? ParentAccount { get; set; }
    public ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
}
