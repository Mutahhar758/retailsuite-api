namespace Retailer.Application.Legacy.Payrolls;

public class PayrollUpsertRequest
{
    public DateOnly Date { get; set; }
    public string SalaryType { get; set; } = default!;
    public string? Description { get; set; }
    public List<PayrollLineRequest> Lines { get; set; } = [];
}

public class PayrollLineRequest
{
    public long Seq { get; set; }
    public string HrId { get; set; } = default!;
    public string PayableAccount { get; set; } = default!;
    public string ExpenseAccount { get; set; } = default!;
    public decimal Salary { get; set; }
    public decimal NoOfLeaves { get; set; }
    public decimal LeaveCharges { get; set; }
    public decimal Overtime { get; set; }
    public decimal OvertimeCharges { get; set; }
    public decimal Bonus { get; set; }
    public decimal NetSalary { get; set; }
    public string? Remarks { get; set; }
}
