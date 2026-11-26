namespace Retailer.Domain.Payroll;

public enum PayrollComponentType
{
    Earning = 1,
    Deduction = 2
}

public enum PayrollCalculationType
{
    Fixed = 1,
    PercentageOfBasic = 2,
    PerUnit = 3,
    Formula = 4
}

public enum PayrollRunStatus
{
    Draft = 1,
    Calculated = 2,
    PendingApproval = 3,
    Approved = 4,
    Posted = 5,
    PartiallyPaid = 6,
    Paid = 7,
    Reversed = 8,
    Cancelled = 9
}

public enum EmployeeLoanType
{
    Loan = 1,
    SalaryAdvance = 2
}

public enum EmployeeLoanStatus
{
    Pending = 1,
    Approved = 2,
    Active = 3,
    Completed = 4,
    Cancelled = 5
}

public enum LoanInstallmentStatus
{
    Scheduled = 1,
    Deducted = 2,
    Skipped = 3,
    SettledManually = 4
}

public enum PayrollPaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Cheque = 3
}
