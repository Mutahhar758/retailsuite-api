using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Legacy;

namespace Retailer.Domain.Payroll;

[Table("EmployeeLoans")]
public class EmployeeLoan : AuditableEntity, IAggregateRoot
{
    public string LoanNo { get; set; } = default!;
    public string HrInfoId { get; set; } = default!;
    public EmployeeLoanType LoanType { get; set; } = EmployeeLoanType.SalaryAdvance;
    public decimal PrincipalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public EmployeeLoanStatus Status { get; set; } = EmployeeLoanStatus.Pending;

    public DateOnly? DisbursementDate { get; set; }
    public string? DisbursementAccountId { get; set; }
    public string? ReceivableAccountId { get; set; }
    public string? GlVoucherNo { get; set; }
    public string? Remarks { get; set; }

    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }

    public HrInfo HrInfo { get; set; } = default!;
    public ChartOfAccount? DisbursementAccount { get; set; }
    public ChartOfAccount? ReceivableAccount { get; set; }
    public ICollection<EmployeeLoanSchedule> Schedules { get; set; } = new List<EmployeeLoanSchedule>();
}

[Table("EmployeeLoanSchedules")]
public class EmployeeLoanSchedule : AuditableEntity, IAggregateRoot
{
    public int EmployeeLoanId { get; set; }
    public int InstallmentNo { get; set; }
    public int DueYear { get; set; }
    public int DueMonth { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal DeductedAmount { get; set; }
    public LoanInstallmentStatus Status { get; set; } = LoanInstallmentStatus.Scheduled;
    public int? PayrollRunItemId { get; set; }
    public string? Remarks { get; set; }

    public EmployeeLoan EmployeeLoan { get; set; } = default!;
    public PayrollRunItem? PayrollRunItem { get; set; }
}
