using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Payroll.Loans;
using Retailer.Domain.Legacy;
using Retailer.Domain.Payroll;
using Retailer.Shared.Common;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.EnterprisePayroll;

internal class EmployeeLoanService : IEmployeeLoanService
{
    private const string PaymentVType = "PV";
    private const string ReceiptVType = "RV";
    private const string DefaultStaffLoanAccountId = "001002006001001";

    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanSchedule> _scheduleRepository;
    private readonly IRepository<HrInfo> _hrInfoRepository;
    private readonly IRepository<ChartOfAccount> _coaRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly ICurrentUser _currentUser;

    public EmployeeLoanService(
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanSchedule> scheduleRepository,
        IRepository<HrInfo> hrInfoRepository,
        IRepository<ChartOfAccount> coaRepository,
        IRepository<GlEntry> glRepository,
        ICurrentUser currentUser)
    {
        _loanRepository = loanRepository;
        _scheduleRepository = scheduleRepository;
        _hrInfoRepository = hrInfoRepository;
        _coaRepository = coaRepository;
        _glRepository = glRepository;
        _currentUser = currentUser;
    }

    public async Task<List<EmployeeLoanDto>> GetListAsync(EmployeeLoanFilter filter, CancellationToken cancellationToken)
    {
        var query = _loanRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.HrInfo)
            .Include(x => x.Schedules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.HrInfoId))
            query = query.Where(x => x.HrInfoId == filter.HrInfoId);
        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.LoanType.HasValue)
            query = query.Where(x => x.LoanType == filter.LoanType.Value);

        var loans = await query
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync(cancellationToken);

        var accountIds = loans
            .SelectMany(x => new[] { x.DisbursementAccountId, x.ReceivableAccountId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var accounts = accountIds.Count > 0
            ? await _coaRepository.GetAll().AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken)
            : new Dictionary<string, string>();

        return loans.Select(x => MapToDto(x, accounts)).ToList();
    }

    public async Task<EmployeeLoanDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var loan = await _loanRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.HrInfo)
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (loan is null)
            return null;

        var accountIds = new[] { loan.DisbursementAccountId, loan.ReceivableAccountId }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var accounts = accountIds.Count > 0
            ? await _coaRepository.GetAll().AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken)
            : new Dictionary<string, string>();

        return MapToDto(loan, accounts);
    }

    public async Task<int> CreateAsync(EmployeeLoanCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HrInfoId))
            throw new BadRequestException("Employee is required.");
        if (request.PrincipalAmount <= 0)
            throw new BadRequestException("Principal amount must be greater than zero.");
        if (request.InstallmentCount < 1 || request.InstallmentCount > 120)
            throw new BadRequestException("Installment count must be between 1 and 120.");
        if (request.StartMonth < 1 || request.StartMonth > 12 || request.StartYear < 2000)
            throw new BadRequestException("Valid start year and month are required.");

        var employee = await _hrInfoRepository.GetAll().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.HrInfoId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{request.HrInfoId}' not found.");

        var loanNo = await GenerateNextLoanNoAsync(cancellationToken);
        var baseInstallment = Math.Round(request.PrincipalAmount / request.InstallmentCount, 2, MidpointRounding.AwayFromZero);

        var receivableAccount = !string.IsNullOrWhiteSpace(request.ReceivableAccountId)
            ? request.ReceivableAccountId.Trim()
            : (!string.IsNullOrWhiteSpace(employee.LoanReceivableAccountId) ? employee.LoanReceivableAccountId : DefaultStaffLoanAccountId);

        var loan = new EmployeeLoan
        {
            LoanNo = loanNo,
            HrInfoId = request.HrInfoId,
            LoanType = request.LoanType,
            PrincipalAmount = Math.Round(request.PrincipalAmount, 2, MidpointRounding.AwayFromZero),
            InstallmentCount = request.InstallmentCount,
            InstallmentAmount = baseInstallment,
            RemainingBalance = Math.Round(request.PrincipalAmount, 2, MidpointRounding.AwayFromZero),
            StartYear = request.StartYear,
            StartMonth = request.StartMonth,
            Status = EmployeeLoanStatus.Pending,
            ReceivableAccountId = receivableAccount,
            Remarks = request.Remarks?.Trim()
        };

        // Generate equal installment schedule (adjusting last installment for rounding)
        decimal allocated = 0m;
        var currentDate = new DateOnly(request.StartYear, request.StartMonth, 1);
        for (int i = 1; i <= request.InstallmentCount; i++)
        {
            var amount = i == request.InstallmentCount
                ? loan.PrincipalAmount - allocated
                : baseInstallment;
            allocated += amount;

            loan.Schedules.Add(new EmployeeLoanSchedule
            {
                InstallmentNo = i,
                DueYear = currentDate.Year,
                DueMonth = currentDate.Month,
                ScheduledAmount = amount,
                DeductedAmount = 0m,
                Status = LoanInstallmentStatus.Scheduled
            });

            currentDate = currentDate.AddMonths(1);
        }

        await _loanRepository.AddAsync(loan, true);
        return loan.Id;
    }

    public async Task ApproveAsync(int id, CancellationToken cancellationToken)
    {
        var loan = await _loanRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Employee loan #{id} not found.");

        if (loan.Status != EmployeeLoanStatus.Pending)
            throw new BadRequestException($"Only Pending loans can be approved (current status: {loan.Status}).");

        loan.Status = EmployeeLoanStatus.Approved;
        loan.ApprovedBy = _currentUser.GetUserEmail() ?? _currentUser.Name ?? "system";
        loan.ApprovedOn = PakistanTime.Now;

        await _loanRepository.UpdateAsync(loan, true);
    }

    public async Task DisburseAsync(int id, EmployeeLoanDisburseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisbursementAccountId))
            throw new BadRequestException("Cash or Bank disbursement account is required.");

        var loan = await _loanRepository.GetAll()
            .Include(x => x.HrInfo)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Employee loan #{id} not found.");

        if (loan.Status != EmployeeLoanStatus.Approved && loan.Status != EmployeeLoanStatus.Pending)
            throw new BadRequestException($"Loan '{loan.LoanNo}' cannot be disbursed in status '{loan.Status}'.");

        var receivableAccount = !string.IsNullOrWhiteSpace(request.ReceivableAccountId)
            ? request.ReceivableAccountId.Trim()
            : (!string.IsNullOrWhiteSpace(loan.ReceivableAccountId) ? loan.ReceivableAccountId : DefaultStaffLoanAccountId);

        if (!await _coaRepository.GetAll().AnyAsync(x => x.Id == receivableAccount, cancellationToken))
            throw new BadRequestException($"Loan receivable account '{receivableAccount}' does not exist in Chart of Accounts.");
        if (!await _coaRepository.GetAll().AnyAsync(x => x.Id == request.DisbursementAccountId, cancellationToken))
            throw new BadRequestException($"Disbursement account '{request.DisbursementAccountId}' does not exist in Chart of Accounts.");

        var voucherNo = await GenerateNextGlVoucherNoAsync(PaymentVType, cancellationToken);

        await _glRepository.AddAsync(new GlEntry
        {
            VDate = request.DisbursementDate,
            VTime = PakistanTime.CurrentTime,
            VoucherNo = voucherNo,
            VType = PaymentVType,
            VSeq = 1,
            DrAccountId = receivableAccount,
            CrAccountId = request.DisbursementAccountId.Trim(),
            Amount = loan.PrincipalAmount,
            NarrationId = " ",
            Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                ? $"Employee {loan.LoanType} disbursement {loan.LoanNo} - {loan.HrInfo?.Name}"
                : request.Remarks.Trim(),
            Clear = 1
        }, false);

        if (loan.Status == EmployeeLoanStatus.Pending)
        {
            loan.ApprovedBy = _currentUser.GetUserEmail() ?? _currentUser.Name ?? "system";
            loan.ApprovedOn = PakistanTime.Now;
        }

        loan.Status = EmployeeLoanStatus.Active;
        loan.DisbursementDate = request.DisbursementDate;
        loan.DisbursementAccountId = request.DisbursementAccountId.Trim();
        loan.ReceivableAccountId = receivableAccount;
        loan.GlVoucherNo = voucherNo;

        await _loanRepository.UpdateAsync(loan, false);
        await _loanRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(int id, string? reason, CancellationToken cancellationToken)
    {
        var loan = await _loanRepository.GetAll()
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Employee loan #{id} not found.");

        if (loan.Status == EmployeeLoanStatus.Active || loan.Status == EmployeeLoanStatus.Completed)
            throw new BadRequestException("Active or completed loans cannot be cancelled. Settle remaining installments manually instead.");

        loan.Status = EmployeeLoanStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            loan.Remarks = string.IsNullOrWhiteSpace(loan.Remarks) ? reason.Trim() : $"{loan.Remarks} | Cancelled: {reason.Trim()}";

        await _loanRepository.UpdateAsync(loan, true);
    }

    public async Task SkipInstallmentAsync(int loanId, int scheduleId, LoanInstallmentActionRequest request, CancellationToken cancellationToken)
    {
        var loan = await _loanRepository.GetAll()
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x => x.Id == loanId, cancellationToken)
            ?? throw new NotFoundException($"Employee loan #{loanId} not found.");

        var schedule = loan.Schedules.FirstOrDefault(x => x.Id == scheduleId)
            ?? throw new NotFoundException($"Installment #{scheduleId} not found.");

        if (schedule.Status != LoanInstallmentStatus.Scheduled)
            throw new BadRequestException("Only Scheduled installments can be skipped.");

        schedule.Status = LoanInstallmentStatus.Skipped;
        schedule.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Skipped and deferred" : request.Remarks.Trim();

        // Append a deferred trailing installment one month after the last scheduled installment
        var lastSchedule = loan.Schedules.OrderByDescending(x => x.DueYear).ThenByDescending(x => x.DueMonth).First();
        var nextDue = new DateOnly(lastSchedule.DueYear, lastSchedule.DueMonth, 1).AddMonths(1);
        var nextInstallmentNo = loan.Schedules.Max(x => x.InstallmentNo) + 1;

        await _scheduleRepository.AddAsync(new EmployeeLoanSchedule
        {
            EmployeeLoanId = loan.Id,
            InstallmentNo = nextInstallmentNo,
            DueYear = nextDue.Year,
            DueMonth = nextDue.Month,
            ScheduledAmount = schedule.ScheduledAmount,
            DeductedAmount = 0m,
            Status = LoanInstallmentStatus.Scheduled,
            Remarks = $"Deferred from installment #{schedule.InstallmentNo}"
        }, false);

        await _scheduleRepository.UpdateAsync(schedule, false);
        await _loanRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SettleInstallmentManuallyAsync(int loanId, int scheduleId, LoanInstallmentActionRequest request, CancellationToken cancellationToken)
    {
        var loan = await _loanRepository.GetAll()
            .Include(x => x.HrInfo)
            .Include(x => x.Schedules)
            .FirstOrDefaultAsync(x => x.Id == loanId, cancellationToken)
            ?? throw new NotFoundException($"Employee loan #{loanId} not found.");

        var schedule = loan.Schedules.FirstOrDefault(x => x.Id == scheduleId)
            ?? throw new NotFoundException($"Installment #{scheduleId} not found.");

        if (schedule.Status != LoanInstallmentStatus.Scheduled)
            throw new BadRequestException("Only Scheduled installments can be settled manually.");

        // Optional GL Receipt entry if a Cash/Bank settlement account is supplied
        if (!string.IsNullOrWhiteSpace(request.SettlementAccountId))
        {
            var receivableAccount = !string.IsNullOrWhiteSpace(loan.ReceivableAccountId)
                ? loan.ReceivableAccountId
                : DefaultStaffLoanAccountId;

            var rvNo = await GenerateNextGlVoucherNoAsync(ReceiptVType, cancellationToken);
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = request.SettlementDate ?? DateOnly.FromDateTime(PakistanTime.Now),
                VTime = PakistanTime.CurrentTime,
                VoucherNo = rvNo,
                VType = ReceiptVType,
                VSeq = 1,
                DrAccountId = request.SettlementAccountId.Trim(),
                CrAccountId = receivableAccount,
                Amount = schedule.ScheduledAmount,
                NarrationId = " ",
                Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                    ? $"Manual loan settlement {loan.LoanNo} #{schedule.InstallmentNo} - {loan.HrInfo?.Name}"
                    : request.Remarks.Trim(),
                Clear = 1
            }, false);
        }

        schedule.Status = LoanInstallmentStatus.SettledManually;
        schedule.DeductedAmount = schedule.ScheduledAmount;
        schedule.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Settled manually" : request.Remarks.Trim();

        loan.RemainingBalance = Math.Max(0m, loan.RemainingBalance - schedule.ScheduledAmount);
        if (loan.RemainingBalance <= 0m)
            loan.Status = EmployeeLoanStatus.Completed;

        await _scheduleRepository.UpdateAsync(schedule, false);
        await _loanRepository.UpdateAsync(loan, false);
        await _loanRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateNextLoanNoAsync(CancellationToken cancellationToken)
    {
        var existingNos = await _loanRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Select(x => x.LoanNo)
            .ToListAsync(cancellationToken);

        long max = 0;
        foreach (var no in existingNos)
        {
            var numericPart = no.StartsWith("LN-", StringComparison.OrdinalIgnoreCase) ? no[3..] : no;
            if (long.TryParse(numericPart, out var val) && val > max)
                max = val;
        }

        return $"LN-{(max + 1):D5}";
    }

    private async Task<string> GenerateNextGlVoucherNoAsync(string vType, CancellationToken cancellationToken)
    {
        var maxVoucher = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == vType)
            .MaxAsync(x => (string?)x.VoucherNo, cancellationToken);

        long next = 1;
        if (!string.IsNullOrWhiteSpace(maxVoucher) && long.TryParse(maxVoucher, out var parsed))
            next = parsed + 1;

        return next.ToString("D5");
    }

    private static EmployeeLoanDto MapToDto(EmployeeLoan x, Dictionary<string, string> accounts) => new()
    {
        Id = x.Id,
        LoanNo = x.LoanNo,
        HrInfoId = x.HrInfoId,
        EmployeeName = x.HrInfo?.Name ?? string.Empty,
        Designation = x.HrInfo?.Designation,
        LoanType = x.LoanType,
        PrincipalAmount = x.PrincipalAmount,
        InstallmentCount = x.InstallmentCount,
        InstallmentAmount = x.InstallmentAmount,
        RemainingBalance = x.RemainingBalance,
        StartYear = x.StartYear,
        StartMonth = x.StartMonth,
        Status = x.Status,
        DisbursementDate = x.DisbursementDate,
        DisbursementAccountId = x.DisbursementAccountId,
        DisbursementAccountTitle = x.DisbursementAccountId != null && accounts.TryGetValue(x.DisbursementAccountId, out var d) ? d : null,
        ReceivableAccountId = x.ReceivableAccountId,
        ReceivableAccountTitle = x.ReceivableAccountId != null && accounts.TryGetValue(x.ReceivableAccountId, out var r) ? r : null,
        GlVoucherNo = x.GlVoucherNo,
        Remarks = x.Remarks,
        ApprovedBy = x.ApprovedBy,
        ApprovedOn = x.ApprovedOn,
        CreatedBy = x.CreatedBy,
        CreatedOn = x.CreatedOn,
        Schedules = x.Schedules
            .OrderBy(s => s.InstallmentNo)
            .Select(s => new EmployeeLoanScheduleDto
            {
                Id = s.Id,
                InstallmentNo = s.InstallmentNo,
                DueYear = s.DueYear,
                DueMonth = s.DueMonth,
                ScheduledAmount = s.ScheduledAmount,
                DeductedAmount = s.DeductedAmount,
                Status = s.Status,
                PayrollRunItemId = s.PayrollRunItemId,
                Remarks = s.Remarks
            }).ToList()
    };
}
