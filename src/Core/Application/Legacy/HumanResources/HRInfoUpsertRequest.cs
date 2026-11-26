namespace Retailer.Application.Legacy.HumanResources;

public class HRInfoUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly Dob { get; set; }
    public string? MaritialStatus { get; set; }
    public string? Cnic { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public DateOnly JoiningDate { get; set; }
    public string? Designation { get; set; }
    public string SalaryType { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public decimal LeaveCharges { get; set; }
    public decimal Overtime { get; set; }
    public string? ExpenseAccount { get; set; }
    public string? PayableAccount { get; set; }
    public string? MediaId { get; set; }

    // Enterprise Payroll additive fields
    public string? Department { get; set; }
    public string? EmploymentStatus { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? BankAccountNo { get; set; }
    public string? Iban { get; set; }
    public int? SalaryStructureId { get; set; }
    public string? LoanReceivableAccountId { get; set; }
}
