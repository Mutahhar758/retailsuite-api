using Retailer.Domain.Payroll;

namespace Retailer.Application.Payroll.Runs;

public class PayrollRunFilter
{
    public int? PayYear { get; set; }
    public int? PayMonth { get; set; }
    public PayrollRunStatus? Status { get; set; }
    public string? SalaryType { get; set; }
}

public class PayrollRunSummaryDto
{
    public int Id { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public int PayYear { get; set; }
    public int PayMonth { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly PayDate { get; set; }
    public string SalaryType { get; set; } = string.Empty;
    public PayrollRunStatus Status { get; set; }
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
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

public class PayrollRunItemComponentDto
{
    public int Id { get; set; }
    public int? PayrollComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public PayrollComponentType ComponentType { get; set; }
    public PayrollCalculationType CalculationType { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public bool IsManualOverride { get; set; }
    public bool ShowOnPayslip { get; set; }
    public int DisplayOrder { get; set; }
    public string? GlAccountId { get; set; }
}

public class PayrollRunItemDto
{
    public int Id { get; set; }
    public string HrInfoId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? Cnic { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankAccountTitle { get; set; }
    public string? BankAccountNo { get; set; }
    public string? Iban { get; set; }
    public string? ExpenseAccountId { get; set; }
    public string? PayableAccountId { get; set; }

    public decimal WorkingDays { get; set; }
    public decimal PresentDays { get; set; }
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
    public decimal BalanceAmount => Math.Max(0m, NetSalary - PaidAmount);

    public bool IsOnHold { get; set; }
    public string? Remarks { get; set; }
    public List<PayrollRunItemComponentDto> Components { get; set; } = [];
}

public class PayrollRunDetailDto : PayrollRunSummaryDto
{
    public List<PayrollRunItemDto> Items { get; set; } = [];
}

public class PayrollRunCreateRequest
{
    public int PayYear { get; set; }
    public int PayMonth { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public DateOnly PayDate { get; set; }
    public string SalaryType { get; set; } = "Monthly";
    public string? Description { get; set; }
    public bool AutoCalculate { get; set; } = true;
}

public class PayrollRunManualComponentInput
{
    public int? PayrollComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public PayrollComponentType ComponentType { get; set; }
    public decimal Amount { get; set; }
    public string? GlAccountId { get; set; }
}

public class PayrollRunItemUpdateRequest
{
    public decimal WorkingDays { get; set; } = 30;
    public decimal PresentDays { get; set; } = 30;
    public decimal UnpaidLeaveDays { get; set; }
    public decimal LeaveChargeRate { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal BonusAmount { get; set; }
    public bool IsOnHold { get; set; }
    public string? Remarks { get; set; }
    public string? ExpenseAccountId { get; set; }
    public string? PayableAccountId { get; set; }
    public List<PayrollRunManualComponentInput>? AdHocComponents { get; set; }
}

public class PayrollGlPreviewLineDto
{
    public string DrAccountId { get; set; } = string.Empty;
    public string DrAccountTitle { get; set; } = string.Empty;
    public string CrAccountId { get; set; } = string.Empty;
    public string CrAccountTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class PayrollGlPreviewDto
{
    public int PayrollRunId { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
    public List<string> ValidationErrors { get; set; } = [];
    public List<PayrollGlPreviewLineDto> Lines { get; set; } = [];
}

public class PayrollDashboardKpiDto
{
    public decimal CurrentMonthNetPayroll { get; set; }
    public int PendingApprovalCount { get; set; }
    public decimal UnpaidPayableBalance { get; set; }
    public decimal ActiveLoanPortfolio { get; set; }
    public int ActiveEmployeeCount { get; set; }
}

public interface IPayrollRunService : ITransientService
{
    Task<PayrollDashboardKpiDto> GetDashboardKpisAsync(CancellationToken cancellationToken);
    Task<List<PayrollRunSummaryDto>> GetListAsync(PayrollRunFilter filter, CancellationToken cancellationToken);
    Task<PayrollRunDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<int> CreateAsync(PayrollRunCreateRequest request, CancellationToken cancellationToken);
    Task<PayrollRunDetailDto> CalculateAsync(int id, CancellationToken cancellationToken);
    Task<PayrollRunItemDto> UpdateItemAsync(int runId, int itemId, PayrollRunItemUpdateRequest request, CancellationToken cancellationToken);
    Task SubmitForApprovalAsync(int id, CancellationToken cancellationToken);
    Task ApproveAsync(int id, CancellationToken cancellationToken);
    Task RejectAsync(int id, string reason, CancellationToken cancellationToken);
    Task<PayrollGlPreviewDto> GetGlPreviewAsync(int id, CancellationToken cancellationToken);
    Task<string> PostAsync(int id, CancellationToken cancellationToken);
    Task<string> ReverseAsync(int id, string reason, CancellationToken cancellationToken);
    Task CancelAsync(int id, CancellationToken cancellationToken);
    Task<byte[]> GeneratePayslipPdfAsync(int runId, int itemId, CancellationToken cancellationToken);
    Task<byte[]> GenerateBulkPayslipsPdfAsync(int runId, CancellationToken cancellationToken);
    Task<byte[]> GeneratePayrollRegisterPdfAsync(int runId, CancellationToken cancellationToken);
}
