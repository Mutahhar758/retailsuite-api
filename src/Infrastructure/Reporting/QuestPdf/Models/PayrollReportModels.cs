namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class PayslipComponentLine
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class PayslipLoanSummary
{
    public string LoanNo { get; set; } = string.Empty;
    public decimal Principal { get; set; }
    public decimal DeductedThisPeriod { get; set; }
    public decimal RemainingBalance { get; set; }
}

public class PayslipModel
{
    public string CompanyName { get; set; } = "RetailSuite Enterprise";
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }

    public string RunNo { get; set; } = string.Empty;
    public string PayPeriodLabel { get; set; } = string.Empty;
    public DateOnly PayDate { get; set; }
    public string SalaryType { get; set; } = "Monthly";

    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? Cnic { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? Iban { get; set; }

    public decimal WorkingDays { get; set; }
    public decimal PresentDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal OvertimeHours { get; set; }

    public List<PayslipComponentLine> Earnings { get; set; } = [];
    public List<PayslipComponentLine> Deductions { get; set; } = [];
    public List<PayslipLoanSummary> ActiveLoans { get; set; } = [];

    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal LoanRecoveryAmount { get; set; }
    public decimal NetSalary { get; set; }
    public string? Remarks { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class PayrollRegisterHeader
{
    public string CompanyName { get; set; } = "RetailSuite Enterprise";
    public string RunNo { get; set; } = string.Empty;
    public string PayPeriodLabel { get; set; } = string.Empty;
    public DateOnly PayDate { get; set; }
    public string SalaryType { get; set; } = "Monthly";
    public string Status { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class PayrollRegisterRow
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = "General";
    public string Designation { get; set; } = "-";
    public string PaymentMethod { get; set; } = "Cash";
    public decimal BasicSalary { get; set; }
    public decimal AllowancesAndBonus { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal LoanRecovery { get; set; }
    public decimal NetSalary { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsOnHold { get; set; }
}
