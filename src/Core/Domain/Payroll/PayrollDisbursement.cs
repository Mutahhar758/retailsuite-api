using System.ComponentModel.DataAnnotations.Schema;
using Retailer.Domain.Legacy;

namespace Retailer.Domain.Payroll;

[Table("PayrollDisbursements")]
public class PayrollDisbursement : AuditableEntity, IAggregateRoot
{
    public string DisbursementNo { get; set; } = default!;
    public int PayrollRunId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string SourceAccountId { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string? GlVoucherNo { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }

    public PayrollRun PayrollRun { get; set; } = default!;
    public ChartOfAccount? SourceAccount { get; set; }
    public ICollection<PayrollDisbursementLine> Lines { get; set; } = new List<PayrollDisbursementLine>();
}

[Table("PayrollDisbursementLines")]
public class PayrollDisbursementLine : AuditableEntity, IAggregateRoot
{
    public int PayrollDisbursementId { get; set; }
    public int PayrollRunItemId { get; set; }
    public string HrInfoId { get; set; } = default!;
    public string EmployeeName { get; set; } = default!;
    public decimal Amount { get; set; }
    public string? BankNameSnapshot { get; set; }
    public string? BankAccountSnapshot { get; set; }
    public string? IbanSnapshot { get; set; }

    public PayrollDisbursement PayrollDisbursement { get; set; } = default!;
    public PayrollRunItem PayrollRunItem { get; set; } = default!;
    public HrInfo HrInfo { get; set; } = default!;
}
