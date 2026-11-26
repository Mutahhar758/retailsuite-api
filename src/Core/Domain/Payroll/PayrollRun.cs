using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Legacy;

namespace Retailer.Domain.Payroll;

[Table("PayrollRuns")]
public class PayrollRun : AuditableEntity, IAggregateRoot
{
    public string RunNo { get; set; } = default!;
    public int PayYear { get; set; }
    public int PayMonth { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly PayDate { get; set; }
    public string SalaryType { get; set; } = "Monthly";
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public string? Description { get; set; }

    public int EmployeeCount { get; set; }
    public decimal TotalGrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalLoanRecovery { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal TotalPaidAmount { get; set; }

    public string? GlVoucherNo { get; set; }
    public string? ReversalGlVoucherNo { get; set; }

    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedOn { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string? PostedBy { get; set; }
    public DateTime? PostedOn { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<PayrollRunItem> Items { get; set; } = new List<PayrollRunItem>();
    public ICollection<PayrollDisbursement> Disbursements { get; set; } = new List<PayrollDisbursement>();
}

[Table("PayrollRunItems")]
public class PayrollRunItem : AuditableEntity, IAggregateRoot
{
    public int PayrollRunId { get; set; }
    public string HrInfoId { get; set; } = default!;

    // Snapshots at calculation/post time for immutable historical payslips
    public string EmployeeName { get; set; } = default!;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? Cnic { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? BankName { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? BankAccountNo { get; set; }
    public string? Iban { get; set; }
    public string? ExpenseAccountId { get; set; }
    public string? PayableAccountId { get; set; }

    public decimal WorkingDays { get; set; } = 30;
    public decimal PresentDays { get; set; } = 30;
    public decimal UnpaidLeaveDays { get; set; }
    public decimal LeaveChargeRate { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal BonusAmount { get; set; }

    public decimal BasicSalary { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal LoanRecoveryAmount { get; set; }
    public decimal NetSalary { get; set; }
    public decimal PaidAmount { get; set; }

    public bool IsOnHold { get; set; }
    public string? Remarks { get; set; }

    public PayrollRun PayrollRun { get; set; } = default!;
    public HrInfo HrInfo { get; set; } = default!;
    public ICollection<PayrollRunItemComponent> Components { get; set; } = new List<PayrollRunItemComponent>();
}

[Table("PayrollRunItemComponents")]
public class PayrollRunItemComponent : AuditableEntity, IAggregateRoot
{
    public int PayrollRunItemId { get; set; }
    public int? PayrollComponentId { get; set; }
    public string ComponentCode { get; set; } = default!;
    public string ComponentName { get; set; } = default!;
    public PayrollComponentType ComponentType { get; set; }
    public PayrollCalculationType CalculationType { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public bool IsManualOverride { get; set; }
    public bool ShowOnPayslip { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? GlAccountId { get; set; }

    public PayrollRunItem PayrollRunItem { get; set; } = default!;
    public PayrollComponent? PayrollComponent { get; set; }
}
