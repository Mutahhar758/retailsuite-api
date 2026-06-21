namespace Retailer.Application.Legacy.HumanResources;

public class HRInfoResponse
{
    public string Id { get; set; } = string.Empty;
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
    public string? MediaUrl { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
