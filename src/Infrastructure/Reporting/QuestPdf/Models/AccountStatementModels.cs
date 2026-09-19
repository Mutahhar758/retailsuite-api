using System;
using System.Collections.Generic;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class AccountStatementReportItem
{
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public string Particular { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class AccountStatementHeader
{
    public string CompanyName { get; set; } = string.Empty;
    public string AccountTitle { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public string DateBasis { get; set; } = "Voucher Date";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class AccountStatementWithDueReportItem
{
    public DateOnly Date { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public string Particular { get; set; } = string.Empty;
    public int? DueDays { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}

public class AccountStatementWithDueHeader
{
    public string CompanyName { get; set; } = string.Empty;
    public string AccountTitle { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
    public string DateBasis { get; set; } = "Voucher Date";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
