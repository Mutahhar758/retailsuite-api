namespace Retailer.Application.Legacy.BankReconciliations;

public class BankReconciliationSaveRequest
{
    public List<BankReconciliationLineSaveRequest> Lines { get; set; } = [];
}

public class BankReconciliationLineSaveRequest
{
    public string VoucherNo { get; set; } = default!;
    public int VSeq { get; set; }
    public bool Clear { get; set; }
    public DateOnly? ReconcileDate { get; set; }
}
