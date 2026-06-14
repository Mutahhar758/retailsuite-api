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
}
