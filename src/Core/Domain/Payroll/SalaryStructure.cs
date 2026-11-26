using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Legacy;

namespace Retailer.Domain.Payroll;

[Table("SalaryStructures")]
public class SalaryStructure : AuditableEntity, IAggregateRoot
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string SalaryType { get; set; } = "Monthly";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SalaryStructureLine> Lines { get; set; } = new List<SalaryStructureLine>();
}

[Table("SalaryStructureLines")]
public class SalaryStructureLine : AuditableEntity, IAggregateRoot
{
    public int SalaryStructureId { get; set; }
    public int PayrollComponentId { get; set; }
    public PayrollCalculationType? CalculationTypeOverride { get; set; }
    public decimal Value { get; set; }
    public string? FormulaOverride { get; set; }
    public int DisplayOrder { get; set; }

    public SalaryStructure SalaryStructure { get; set; } = default!;
    public PayrollComponent PayrollComponent { get; set; } = default!;
}

[Table("EmployeeSalaryComponents")]
public class EmployeeSalaryComponent : AuditableEntity, IAggregateRoot
{
    public string HrInfoId { get; set; } = default!;
    public int PayrollComponentId { get; set; }
    public PayrollCalculationType? CalculationTypeOverride { get; set; }
    public decimal Value { get; set; }
    public string? FormulaOverride { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public HrInfo HrInfo { get; set; } = default!;
    public PayrollComponent PayrollComponent { get; set; } = default!;
}
