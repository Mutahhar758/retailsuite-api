using Retailer.Domain.Payroll;

namespace Retailer.Application.Payroll.Loans;

public class EmployeeLoanScheduleDto
{
    public int Id { get; set; }
    public int InstallmentNo { get; set; }
    public int DueYear { get; set; }
    public int DueMonth { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal DeductedAmount { get; set; }
    public LoanInstallmentStatus Status { get; set; }
    public int? PayrollRunItemId { get; set; }
    public string? Remarks { get; set; }
}

public class EmployeeLoanDto
{
    public int Id { get; set; }
    public string LoanNo { get; set; } = string.Empty;
    public string HrInfoId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public EmployeeLoanType LoanType { get; set; }
    public decimal PrincipalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public EmployeeLoanStatus Status { get; set; }
    public DateOnly? DisbursementDate { get; set; }
    public string? DisbursementAccountId { get; set; }
    public string? DisbursementAccountTitle { get; set; }
    public string? ReceivableAccountId { get; set; }
    public string? ReceivableAccountTitle { get; set; }
    public string? GlVoucherNo { get; set; }
    public string? Remarks { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public List<EmployeeLoanScheduleDto> Schedules { get; set; } = [];
}

public class EmployeeLoanFilter
{
    public string? HrInfoId { get; set; }
    public EmployeeLoanStatus? Status { get; set; }
    public EmployeeLoanType? LoanType { get; set; }
}

public class EmployeeLoanCreateRequest
{
    public string HrInfoId { get; set; } = string.Empty;
    public EmployeeLoanType LoanType { get; set; } = EmployeeLoanType.SalaryAdvance;
    public decimal PrincipalAmount { get; set; }
    public int InstallmentCount { get; set; } = 1;
    public int StartYear { get; set; }
    public int StartMonth { get; set; }
    public string? ReceivableAccountId { get; set; }
    public string? Remarks { get; set; }
}

public class EmployeeLoanDisburseRequest
{
    public DateOnly DisbursementDate { get; set; }
    public string DisbursementAccountId { get; set; } = string.Empty;
    public string? ReceivableAccountId { get; set; }
    public string? Remarks { get; set; }
}

public class LoanInstallmentActionRequest
{
    public string? Remarks { get; set; }
    public DateOnly? SettlementDate { get; set; }
    public string? SettlementAccountId { get; set; }
}

public interface IEmployeeLoanService : ITransientService
{
    Task<List<EmployeeLoanDto>> GetListAsync(EmployeeLoanFilter filter, CancellationToken cancellationToken);
    Task<EmployeeLoanDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> CreateAsync(EmployeeLoanCreateRequest request, CancellationToken cancellationToken);
    Task ApproveAsync(int id, CancellationToken cancellationToken);
    Task DisburseAsync(int id, EmployeeLoanDisburseRequest request, CancellationToken cancellationToken);
    Task CancelAsync(int id, string? reason, CancellationToken cancellationToken);
    Task SkipInstallmentAsync(int loanId, int scheduleId, LoanInstallmentActionRequest request, CancellationToken cancellationToken);
    Task SettleInstallmentManuallyAsync(int loanId, int scheduleId, LoanInstallmentActionRequest request, CancellationToken cancellationToken);
}
