using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Common.Enums;

namespace Retailer.Domain.Legacy;

[Table("ProductionExpense")]
public class ProductionExpense : AuditableEntity, IAggregateRoot
{
    public int ProductionMasterId { get; set; }
    public string VType { get; set; } = "MF";
    public string VNo { get; set; } = default!;
    public int Seq { get; set; }
    public string Title { get; set; } = default!;
    public string? AccountId { get; set; }
    public ExpenseCalculationType CalculationType { get; set; } = ExpenseCalculationType.PerUnit;
    public decimal Rate { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public string? Remarks { get; set; }

    public ProductionMaster? ProductionMaster { get; set; }
    public ChartOfAccount? Account { get; set; }
}
