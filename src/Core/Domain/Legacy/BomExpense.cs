using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Common.Enums;

namespace Retailer.Domain.Legacy;

[Table("BomExpense")]
public class BomExpense : AuditableEntity, IAggregateRoot
{
    public int BomMasterId { get; set; }
    public int Seq { get; set; }
    public string Title { get; set; } = default!;
    public string? AccountId { get; set; }
    public ExpenseCalculationType CalculationType { get; set; } = ExpenseCalculationType.PerUnit;
    public decimal Rate { get; set; }
    public string? Remarks { get; set; }

    public BomMaster? BomMaster { get; set; }
    public ChartOfAccount? Account { get; set; }
}
