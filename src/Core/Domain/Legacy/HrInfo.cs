using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Payroll;

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
    public string? MediaId { get; set; }

    // Enterprise Payroll additive fields
    public string? Department { get; set; }
    public string EmploymentStatus { get; set; } = "Active";
    public DateOnly? TerminationDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? BankName { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? BankAccountNo { get; set; }
    public string? Iban { get; set; }
    public int? SalaryStructureId { get; set; }
    public string? LoanReceivableAccountId { get; set; }

    public SalaryStructure? SalaryStructure { get; set; }
    public ChartOfAccount? LoanReceivableAccountRef { get; set; }
}
