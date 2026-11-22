namespace Retailer.Application.Legacy.Khata;

public class KhataPartyDto
{
    public string AccountId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string PartyType { get; set; } = "Customer"; // "Customer" | "Vendor"
    public string? Phone { get; set; }
    public decimal Balance { get; set; } // Dr - Cr: >0 Receivable (You will Get), <0 Payable (You will Give)
    public DateOnly? LastTransactionDate { get; set; }
    public decimal? LastTransactionAmount { get; set; }
    public string? LastTransactionType { get; set; } // "IN" | "OUT" | "OTHER"
    public string? LastTransactionNote { get; set; }
}

public class KhataSummaryResponse
{
    public decimal TotalReceivable { get; set; }
    public decimal TotalPayable { get; set; }
    public decimal NetBalance { get; set; }
    public List<KhataPartyDto> Parties { get; set; } = new();
}

public class KhataLedgerEntryDto
{
    public DateOnly VDate { get; set; }
    public string VType { get; set; } = default!;
    public string VoucherNo { get; set; } = default!;
    public int VSeq { get; set; }
    public string? CounterpartAccountId { get; set; }
    public string CounterpartTitle { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? Narration { get; set; }
    public decimal Dr { get; set; }
    public decimal Cr { get; set; }
    public decimal RunningBalance { get; set; }
    public bool IsEditable { get; set; }
    public bool IsMultiLineVoucher { get; set; }
}

public class KhataLedgerResponse
{
    public string AccountId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string PartyType { get; set; } = "Customer";
    public string? Phone { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public List<KhataLedgerEntryDto> Entries { get; set; } = new();
}
