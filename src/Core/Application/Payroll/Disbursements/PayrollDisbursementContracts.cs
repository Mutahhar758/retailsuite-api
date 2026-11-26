namespace Retailer.Application.Payroll.Disbursements;

public class PayrollDisbursementLineDto
{
    public int Id { get; set; }
    public int PayrollRunItemId { get; set; }
    public string HrInfoId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? BankNameSnapshot { get; set; }
    public string? BankAccountSnapshot { get; set; }
    public string? IbanSnapshot { get; set; }
}

public class PayrollDisbursementDto
{
    public int Id { get; set; }
    public string DisbursementNo { get; set; } = string.Empty;
    public int PayrollRunId { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string SourceAccountId { get; set; } = string.Empty;
    public string SourceAccountTitle { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? GlVoucherNo { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public List<PayrollDisbursementLineDto> Lines { get; set; } = [];
}

public class PayrollDisbursementCreateRequest
{
    public int PayrollRunId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string SourceAccountId { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
    /// <summary>Optional subset of PayrollRunItem IDs; if empty/null, disburses all unpaid non-held items matching PaymentMethod.</summary>
    public List<int>? PayrollRunItemIds { get; set; }
}

public interface IPayrollDisbursementService : ITransientService
{
    Task<List<PayrollDisbursementDto>> GetByRunIdAsync(int payrollRunId, CancellationToken cancellationToken);
    Task<PayrollDisbursementDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<PayrollDisbursementDto> CreateAsync(PayrollDisbursementCreateRequest request, CancellationToken cancellationToken);
    Task VoidAsync(int id, CancellationToken cancellationToken);
    Task<(byte[] Content, string FileName)> ExportBankAdviceCsvAsync(int disbursementId, CancellationToken cancellationToken);
}
