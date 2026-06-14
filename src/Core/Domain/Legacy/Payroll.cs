using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("Payroll")]
public class Payroll : AuditableEntity, IAggregateRoot
{
    public string VoucherNo { get; set; } = default!;
    public DateOnly VDate { get; set; }
    public string SalaryType { get; set; } = default!;
    public string? Description { get; set; }
    public long Seq { get; set; }
    public decimal Salary { get; set; }
    public decimal NoOfLeaves { get; set; }
    public decimal LeaveCharges { get; set; }
    public decimal Overtime { get; set; }
    public decimal OvertimeCharges { get; set; }
    public decimal Bonus { get; set; }
    public decimal NetSalary { get; set; }
    public string? Remarks { get; set; }

    public string? HrInfoId { get; set; }
    public string? PayableAccountId { get; set; }
    public string? ExpenseAccountId { get; set; }
    public HrInfo? HrInfo { get; set; }
    public ChartOfAccount? PayableAccount { get; set; }
    public ChartOfAccount? ExpenseAccount { get; set; }
}
