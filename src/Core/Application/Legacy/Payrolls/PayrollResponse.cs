namespace Retailer.Application.Legacy.Payrolls;

public class PayrollResponse
{
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public decimal Amount { get; set; }
    public string SalaryType { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}

public class PayrollLineResponse
{
    public long Seq { get; set; }
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = default!;
    public string SalaryType { get; set; } = default!;
    public string? Description { get; set; }
    public string HrId { get; set; } = default!;
    public string? HrName { get; set; }
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
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}

public class PayrollLookupItemResponse
{
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
}

public class PayrollEmployeeLookupResponse
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string SalaryType { get; set; } = default!;
    public decimal Salary { get; set; }
    public decimal LeaveCharges { get; set; }
    public decimal Overtime { get; set; }
    public string? PayableAccount { get; set; }
    public string? ExpenseAccount { get; set; }
}

public class PayrollLookupsResponse
{
    public List<PayrollEmployeeLookupResponse> Employees { get; set; } = new();
    public List<PayrollLookupItemResponse> ExpenseAccounts { get; set; } = new();
    public List<PayrollLookupItemResponse> PayableAccounts { get; set; } = new();
}
