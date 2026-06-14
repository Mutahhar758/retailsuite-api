using System.ComponentModel.DataAnnotations.Schema;

namespace Retailer.Domain.Legacy;

[Table("HRInfo")]
public class HrInfo : AuditableEntity<string>, IAggregateRoot
{
    public string Name { get; set; } = default!;
    public string? FatherName { get; set; }
    public string Gender { get; set; } = default!;
    public DateOnly Dob { get; set; }
    public string? MaritialStatus { get; set; }
    public string? Cnic { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public DateOnly JoiningDate { get; set; }
    public string? Designation { get; set; }
    public string SalaryType { get; set; } = default!;
    public decimal Salary { get; set; }
    public decimal LeaveCharges { get; set; }
    public decimal Overtime { get; set; }
    public string? ExpenseAccount { get; set; }
    public string? PayableAccount { get; set; }

    public string? ExpenseAccountId { get; set; }
    public string? PayableAccountId { get; set; }
    public ChartOfAccount? ExpenseAccountRef { get; set; }
    public ChartOfAccount? PayableAccountRef { get; set; }
}
