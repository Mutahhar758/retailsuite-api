namespace Retailer.Application.Legacy.BankReconciliations;

public class BankReconciliationLineResponse
{
    public string VoucherNo { get; set; } = default!;
    public DateOnly Date { get; set; }
    public DateOnly? CheckDate { get; set; }
    public string? CheckNum { get; set; }
    public DateOnly? ReconcileDate { get; set; }
    public string Title { get; set; } = default!;
    public decimal Dr { get; set; }
    public decimal Cr { get; set; }
    public bool Clear { get; set; }
    public int VSeq { get; set; }
}

public class BankReconciliationSnapshotResponse
{
    public List<BankReconciliationLineResponse> Lines { get; set; } = [];
    public decimal ReconcileBalance { get; set; }
    public decimal StatementBalance { get; set; }
}
