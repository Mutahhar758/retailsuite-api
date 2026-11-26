using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Legacy;

namespace Retailer.Domain.Payroll;

[Table("PayrollComponents")]
public class PayrollComponent : AuditableEntity, IAggregateRoot
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public PayrollComponentType ComponentType { get; set; }
    public PayrollCalculationType CalculationType { get; set; } = PayrollCalculationType.Fixed;
    public decimal DefaultValue { get; set; }
    public string? FormulaExpression { get; set; }
    public bool IsSystem { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool ShowOnPayslip { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    public string? ExpenseAccountId { get; set; }
    public string? LiabilityAccountId { get; set; }

    public ChartOfAccount? ExpenseAccount { get; set; }
    public ChartOfAccount? LiabilityAccount { get; set; }
}
