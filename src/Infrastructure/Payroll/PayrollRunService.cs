using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Payroll.Engine;
using Retailer.Application.Payroll.Runs;
using Retailer.Domain.Legacy;
using Retailer.Domain.Payroll;
using Retailer.Infrastructure.Reporting.QuestPdf.Documents;
using Retailer.Infrastructure.Reporting.QuestPdf.Models;
using Retailer.Shared.Common;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.EnterprisePayroll;

internal class PayrollRunService : IPayrollRunService
{
    private const string PayrollVType = "PR";
    private const string DefaultExpenseAccountId = "004002001007001";
    private const string DefaultPayableAccountId = "002001005001001";
    private const string DefaultLoanReceivableAccountId = "001002006001001";

    private readonly IRepository<PayrollRun> _runRepository;
    private readonly IRepository<PayrollRunItem> _itemRepository;
    private readonly IRepository<PayrollRunItemComponent> _itemComponentRepository;
    private readonly IRepository<PayrollComponent> _componentRepository;
    private readonly IRepository<SalaryStructure> _structureRepository;
    private readonly IRepository<EmployeeSalaryComponent> _employeeComponentRepository;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanSchedule> _loanScheduleRepository;
    private readonly IRepository<PayrollDisbursement> _disbursementRepository;
    private readonly IRepository<HrInfo> _hrInfoRepository;
    private readonly IRepository<ChartOfAccount> _coaRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<CompanyDetail> _companyRepository;
    private readonly ICurrentUser _currentUser;

    public PayrollRunService(
        IRepository<PayrollRun> runRepository,
        IRepository<PayrollRunItem> itemRepository,
        IRepository<PayrollRunItemComponent> itemComponentRepository,
        IRepository<PayrollComponent> componentRepository,
        IRepository<SalaryStructure> structureRepository,
        IRepository<EmployeeSalaryComponent> employeeComponentRepository,
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanSchedule> loanScheduleRepository,
        IRepository<PayrollDisbursement> disbursementRepository,
        IRepository<HrInfo> hrInfoRepository,
        IRepository<ChartOfAccount> coaRepository,
        IRepository<GlEntry> glRepository,
        IRepository<CompanyDetail> companyRepository,
        ICurrentUser currentUser)
    {
        _runRepository = runRepository;
        _itemRepository = itemRepository;
        _itemComponentRepository = itemComponentRepository;
        _componentRepository = componentRepository;
        _structureRepository = structureRepository;
        _employeeComponentRepository = employeeComponentRepository;
        _loanRepository = loanRepository;
        _loanScheduleRepository = loanScheduleRepository;
        _disbursementRepository = disbursementRepository;
        _hrInfoRepository = hrInfoRepository;
        _coaRepository = coaRepository;
        _glRepository = glRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<PayrollDashboardKpiDto> GetDashboardKpisAsync(CancellationToken cancellationToken)
    {
        var now = PakistanTime.Now;
        var runs = await _runRepository.GetAll().AsNoTracking()
            .Where(x => x.Status != PayrollRunStatus.Cancelled && x.Status != PayrollRunStatus.Reversed)
            .ToListAsync(cancellationToken);

        var currentMonthNet = runs
            .Where(x => x.PayYear == now.Year && x.PayMonth == now.Month)
            .Sum(x => x.TotalNetSalary);

        var pendingApprovals = runs.Count(x => x.Status == PayrollRunStatus.PendingApproval);

        var unpaidPayable = runs
            .Where(x => x.Status == PayrollRunStatus.Posted || x.Status == PayrollRunStatus.PartiallyPaid)
            .Sum(x => Math.Max(0m, x.TotalNetSalary - x.TotalPaidAmount));

        var activeLoans = await _loanRepository.GetAll().AsNoTracking()
            .Where(x => x.Status == EmployeeLoanStatus.Active)
            .SumAsync(x => (decimal?)x.RemainingBalance, cancellationToken) ?? 0m;

        var activeEmployees = await _hrInfoRepository.GetAll().AsNoTracking()
            .CountAsync(x => x.EmploymentStatus == "Active" || x.EmploymentStatus == null || x.EmploymentStatus == "", cancellationToken);

        return new PayrollDashboardKpiDto
        {
            CurrentMonthNetPayroll = currentMonthNet,
            PendingApprovalCount = pendingApprovals,
            UnpaidPayableBalance = unpaidPayable,
            ActiveLoanPortfolio = activeLoans,
            ActiveEmployeeCount = activeEmployees
        };
    }

    public async Task<List<PayrollRunSummaryDto>> GetListAsync(PayrollRunFilter filter, CancellationToken cancellationToken)
    {
        var query = _runRepository.GetAll().AsNoTracking();

        if (filter.PayYear.HasValue)
            query = query.Where(x => x.PayYear == filter.PayYear.Value);
        if (filter.PayMonth.HasValue)
            query = query.Where(x => x.PayMonth == filter.PayMonth.Value);
        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.SalaryType))
            query = query.Where(x => x.SalaryType == filter.SalaryType);

        var runs = await query
            .OrderByDescending(x => x.PayYear)
            .ThenByDescending(x => x.PayMonth)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return runs.Select(MapSummary).ToList();
    }

    public async Task<PayrollRunDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(i => i.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return run is null ? null : MapDetail(run);
    }

    public async Task<int> CreateAsync(PayrollRunCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.PayYear < 2000 || request.PayMonth < 1 || request.PayMonth > 12)
            throw new BadRequestException("Valid pay year and pay month (1-12) are required.");

        var salaryType = string.IsNullOrWhiteSpace(request.SalaryType) ? "Monthly" : request.SalaryType.Trim();
        var periodStart = request.PeriodStart ?? new DateOnly(request.PayYear, request.PayMonth, 1);
        var periodEnd = request.PeriodEnd ?? new DateOnly(request.PayYear, request.PayMonth, DateTime.DaysInMonth(request.PayYear, request.PayMonth));

        if (periodEnd < periodStart)
            throw new BadRequestException("Period end date cannot be before period start date.");

        var runNo = await GenerateNextRunNoAsync(request.PayYear, request.PayMonth, cancellationToken);

        var run = new PayrollRun
        {
            RunNo = runNo,
            PayYear = request.PayYear,
            PayMonth = request.PayMonth,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PayDate = request.PayDate == default ? periodEnd : request.PayDate,
            SalaryType = salaryType,
            Status = PayrollRunStatus.Draft,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(request.PayMonth)} {request.PayYear} ({salaryType}) Payroll"
                : request.Description.Trim()
        };

        await _runRepository.AddAsync(run, true);

        if (request.AutoCalculate)
            await CalculateAsync(run.Id, cancellationToken);

        return run.Id;
    }

    public async Task<PayrollRunDetailDto> CalculateAsync(int id, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .Include(x => x.Items)
            .ThenInclude(i => i.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        if (run.Status != PayrollRunStatus.Draft && run.Status != PayrollRunStatus.Calculated)
            throw new BadRequestException($"Payroll run '{run.RunNo}' cannot be recalculated in status '{run.Status}'.");

        var employees = await _hrInfoRepository.GetAll()
            .AsNoTracking()
            .Where(x => (x.EmploymentStatus == "Active" || x.EmploymentStatus == null || x.EmploymentStatus == "")
                        && (string.IsNullOrEmpty(run.SalaryType) || x.SalaryType == run.SalaryType))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var allComponents = await _componentRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var structures = await _structureRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var defaultStructure = structures.FirstOrDefault(s => s.IsDefault && s.SalaryType.Equals(run.SalaryType, StringComparison.OrdinalIgnoreCase));

        var empOverrides = await _employeeComponentRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.IsActive
                        && (!x.EffectiveFrom.HasValue || x.EffectiveFrom.Value <= run.PeriodEnd)
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= run.PeriodStart))
            .ToListAsync(cancellationToken);

        var dueLoanSchedules = await _loanScheduleRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.EmployeeLoan)
            .Where(x => x.Status == LoanInstallmentStatus.Scheduled
                        && x.EmployeeLoan.Status == EmployeeLoanStatus.Active
                        && x.DueYear == run.PayYear
                        && x.DueMonth == run.PayMonth)
            .ToListAsync(cancellationToken);

        var dueLoanByEmp = dueLoanSchedules
            .GroupBy(x => x.EmployeeLoan.HrInfoId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.ScheduledAmount));

        var existingByEmp = run.Items.ToDictionary(x => x.HrInfoId);
        var activeEmpIds = employees.Select(x => x.Id).ToHashSet();

        // Remove items for employees no longer active
        foreach (var stale in run.Items.Where(i => !activeEmpIds.Contains(i.HrInfoId)).ToList())
        {
            await _itemRepository.DeleteAsync(stale, false);
        }

        var workingDays = (decimal)(run.PeriodEnd.DayNumber - run.PeriodStart.DayNumber + 1);
        if (workingDays <= 0) workingDays = 30m;

        foreach (var emp in employees)
        {
            existingByEmp.TryGetValue(emp.Id, out var existingItem);

            var structure = emp.SalaryStructureId.HasValue
                ? structures.FirstOrDefault(s => s.Id == emp.SalaryStructureId.Value)
                : defaultStructure;

            var specs = BuildComponentSpecs(allComponents, structure, empOverrides.Where(o => o.HrInfoId == emp.Id).ToList(), existingItem);

            var empWorkingDays = existingItem?.WorkingDays > 0 ? existingItem.WorkingDays : workingDays;
            var empPresentDays = existingItem != null ? existingItem.PresentDays : workingDays;
            var unpaidLeaves = existingItem?.UnpaidLeaveDays ?? 0m;
            var leaveRate = existingItem != null && existingItem.LeaveChargeRate > 0 ? existingItem.LeaveChargeRate : emp.LeaveCharges;
            var otHours = existingItem?.OvertimeHours ?? 0m;
            var otRate = existingItem != null && existingItem.OvertimeRate > 0 ? existingItem.OvertimeRate : emp.Overtime;
            var bonus = existingItem?.BonusAmount ?? 0m;
            var scheduledLoan = dueLoanByEmp.GetValueOrDefault(emp.Id, 0m);

            var calc = PayrollCalculationEngine.Calculate(new PayrollCalculationInput(
                BaseSalary: emp.Salary,
                WorkingDays: empWorkingDays,
                PresentDays: empPresentDays,
                UnpaidLeaveDays: unpaidLeaves,
                LeaveChargeRate: leaveRate,
                OvertimeHours: otHours,
                OvertimeRate: otRate,
                BonusAmount: bonus,
                ScheduledLoanRecovery: scheduledLoan,
                ProrateBasicByPresentDays: false,
                Components: specs));

            var expenseAccount = ResolveAccountId(existingItem?.ExpenseAccountId, emp.ExpenseAccountId, emp.ExpenseAccount, DefaultExpenseAccountId);
            var payableAccount = ResolveAccountId(existingItem?.PayableAccountId, emp.PayableAccountId, emp.PayableAccount, DefaultPayableAccountId);

            if (existingItem is null)
            {
                var newItem = new PayrollRunItem
                {
                    PayrollRunId = run.Id,
                    HrInfoId = emp.Id,
                    EmployeeName = emp.Name,
                    Designation = emp.Designation,
                    Department = emp.Department,
                    Cnic = emp.Cnic,
                    PaymentMethod = string.IsNullOrWhiteSpace(emp.PaymentMethod) ? "Cash" : emp.PaymentMethod,
                    BankName = emp.BankName,
                    BankAccountTitle = emp.BankAccountTitle,
                    BankAccountNo = emp.BankAccountNo,
                    Iban = emp.Iban,
                    ExpenseAccountId = expenseAccount,
                    PayableAccountId = payableAccount,
                    WorkingDays = empWorkingDays,
                    PresentDays = empPresentDays,
                    UnpaidLeaveDays = unpaidLeaves,
                    LeaveChargeRate = leaveRate,
                    OvertimeHours = otHours,
                    OvertimeRate = otRate,
                    BonusAmount = bonus,
                    BasicSalary = calc.BasicSalary,
                    GrossEarnings = calc.GrossEarnings,
                    TotalDeductions = calc.TotalDeductions,
                    LoanRecoveryAmount = calc.LoanRecoveryAmount,
                    NetSalary = calc.NetSalary,
                    PaidAmount = 0m,
                    IsOnHold = false
                };

                foreach (var c in calc.Components)
                {
                    newItem.Components.Add(MapCalculatedComponent(c));
                }

                await _itemRepository.AddAsync(newItem, false);
                run.Items.Add(newItem);
            }
            else
            {
                existingItem.EmployeeName = emp.Name;
                existingItem.Designation = emp.Designation;
                existingItem.Department = emp.Department;
                existingItem.Cnic = emp.Cnic;
                existingItem.PaymentMethod = string.IsNullOrWhiteSpace(emp.PaymentMethod) ? "Cash" : emp.PaymentMethod;
                existingItem.BankName = emp.BankName;
                existingItem.BankAccountTitle = emp.BankAccountTitle;
                existingItem.BankAccountNo = emp.BankAccountNo;
                existingItem.Iban = emp.Iban;
                existingItem.ExpenseAccountId = expenseAccount;
                existingItem.PayableAccountId = payableAccount;
                existingItem.WorkingDays = empWorkingDays;
                existingItem.PresentDays = empPresentDays;
                existingItem.LeaveChargeRate = leaveRate;
                existingItem.OvertimeRate = otRate;
                existingItem.BasicSalary = calc.BasicSalary;
                existingItem.GrossEarnings = calc.GrossEarnings;
                existingItem.TotalDeductions = calc.TotalDeductions;
                existingItem.LoanRecoveryAmount = calc.LoanRecoveryAmount;
                existingItem.NetSalary = calc.NetSalary;

                if (existingItem.Components.Count > 0)
                    await _itemComponentRepository.DeleteRangeAsync(existingItem.Components.ToList(), false);

                existingItem.Components.Clear();
                foreach (var c in calc.Components)
                {
                    var compEntity = MapCalculatedComponent(c);
                    compEntity.PayrollRunItemId = existingItem.Id;
                    await _itemComponentRepository.AddAsync(compEntity, false);
                    existingItem.Components.Add(compEntity);
                }

                await _itemRepository.UpdateAsync(existingItem, false);
            }
        }

        RecalculateRunTotals(run);
        run.Status = PayrollRunStatus.Calculated;
        run.RejectionReason = null;

        await _runRepository.UpdateAsync(run, false);
        await _runRepository.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(run.Id, cancellationToken))!;
    }

    public async Task<PayrollRunItemDto> UpdateItemAsync(int runId, int itemId, PayrollRunItemUpdateRequest request, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .Include(x => x.Items)
            .ThenInclude(i => i.Components)
            .FirstOrDefaultAsync(x => x.Id == runId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{runId} not found.");

        if (run.Status != PayrollRunStatus.Draft && run.Status != PayrollRunStatus.Calculated)
            throw new BadRequestException($"Items cannot be edited when run is in status '{run.Status}'.");

        var item = run.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException($"Payroll run item #{itemId} not found.");

        var emp = await _hrInfoRepository.GetAll().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == item.HrInfoId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{item.HrInfoId}' not found.");

        var allComponents = await _componentRepository.GetAll().AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
        var structure = emp.SalaryStructureId.HasValue
            ? await _structureRepository.GetAll().AsNoTracking().Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == emp.SalaryStructureId.Value, cancellationToken)
            : await _structureRepository.GetAll().AsNoTracking().Include(s => s.Lines).FirstOrDefaultAsync(s => s.IsDefault && s.SalaryType == run.SalaryType, cancellationToken);

        var empOverrides = await _employeeComponentRepository.GetAll().AsNoTracking()
            .Where(x => x.HrInfoId == emp.Id && x.IsActive
                        && (!x.EffectiveFrom.HasValue || x.EffectiveFrom.Value <= run.PeriodEnd)
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= run.PeriodStart))
            .ToListAsync(cancellationToken);

        var scheduledLoan = await _loanScheduleRepository.GetAll().AsNoTracking()
            .Include(x => x.EmployeeLoan)
            .Where(x => x.Status == LoanInstallmentStatus.Scheduled
                        && x.EmployeeLoan.Status == EmployeeLoanStatus.Active
                        && x.EmployeeLoan.HrInfoId == emp.Id
                        && x.DueYear == run.PayYear
                        && x.DueMonth == run.PayMonth)
            .SumAsync(x => (decimal?)x.ScheduledAmount, cancellationToken) ?? 0m;

        item.WorkingDays = request.WorkingDays > 0 ? request.WorkingDays : item.WorkingDays;
        item.PresentDays = request.PresentDays >= 0 ? request.PresentDays : item.PresentDays;
        item.UnpaidLeaveDays = Math.Max(0m, request.UnpaidLeaveDays);
        item.LeaveChargeRate = Math.Max(0m, request.LeaveChargeRate);
        item.OvertimeHours = Math.Max(0m, request.OvertimeHours);
        item.OvertimeRate = Math.Max(0m, request.OvertimeRate);
        item.BonusAmount = Math.Max(0m, request.BonusAmount);
        item.IsOnHold = request.IsOnHold;
        item.Remarks = request.Remarks?.Trim();
        if (!string.IsNullOrWhiteSpace(request.ExpenseAccountId))
            item.ExpenseAccountId = request.ExpenseAccountId.Trim();
        if (!string.IsNullOrWhiteSpace(request.PayableAccountId))
            item.PayableAccountId = request.PayableAccountId.Trim();

        var specs = BuildComponentSpecs(allComponents, structure, empOverrides, request.AdHocComponents is null ? item : null);
        if (request.AdHocComponents is { Count: > 0 })
        {
            foreach (var adHoc in request.AdHocComponents)
            {
                if (string.IsNullOrWhiteSpace(adHoc.ComponentName) || adHoc.Amount <= 0)
                    continue;
                specs.Add(new PayrollComponentSpec(
                    adHoc.PayrollComponentId,
                    string.IsNullOrWhiteSpace(adHoc.ComponentCode) ? "ADHOC" : adHoc.ComponentCode.Trim().ToUpperInvariant(),
                    adHoc.ComponentName.Trim(),
                    adHoc.ComponentType,
                    PayrollCalculationType.Fixed,
                    adHoc.Amount,
                    null,
                    true,
                    99,
                    adHoc.GlAccountId,
                    IsManualOverride: true));
            }
        }

        var calc = PayrollCalculationEngine.Calculate(new PayrollCalculationInput(
            BaseSalary: emp.Salary,
            WorkingDays: item.WorkingDays,
            PresentDays: item.PresentDays,
            UnpaidLeaveDays: item.UnpaidLeaveDays,
            LeaveChargeRate: item.LeaveChargeRate,
            OvertimeHours: item.OvertimeHours,
            OvertimeRate: item.OvertimeRate,
            BonusAmount: item.BonusAmount,
            ScheduledLoanRecovery: scheduledLoan,
            ProrateBasicByPresentDays: false,
            Components: specs));

        item.BasicSalary = calc.BasicSalary;
        item.GrossEarnings = calc.GrossEarnings;
        item.TotalDeductions = calc.TotalDeductions;
        item.LoanRecoveryAmount = calc.LoanRecoveryAmount;
        item.NetSalary = calc.NetSalary;

        if (item.Components.Count > 0)
            await _itemComponentRepository.DeleteRangeAsync(item.Components.ToList(), false);

        item.Components.Clear();
        foreach (var c in calc.Components)
        {
            var compEntity = MapCalculatedComponent(c);
            compEntity.PayrollRunItemId = item.Id;
            await _itemComponentRepository.AddAsync(compEntity, false);
            item.Components.Add(compEntity);
        }

        RecalculateRunTotals(run);
        await _itemRepository.UpdateAsync(item, false);
        await _runRepository.UpdateAsync(run, false);
        await _runRepository.SaveChangesAsync(cancellationToken);

        return MapItem(item);
    }

    public async Task SubmitForApprovalAsync(int id, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        if (run.Status != PayrollRunStatus.Calculated)
            throw new BadRequestException("Only Calculated payroll runs can be submitted for approval.");

        if (run.Items.Count == 0)
            throw new BadRequestException("Cannot submit an empty payroll run with zero employees.");

        run.Status = PayrollRunStatus.PendingApproval;
        run.SubmittedBy = _currentUser.GetUserEmail() ?? _currentUser.Name ?? "system";
        run.SubmittedOn = PakistanTime.Now;
        run.RejectionReason = null;

        await _runRepository.UpdateAsync(run, true);
    }

    public async Task ApproveAsync(int id, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        if (run.Status != PayrollRunStatus.PendingApproval)
            throw new BadRequestException("Only PendingApproval payroll runs can be approved.");

        run.Status = PayrollRunStatus.Approved;
        run.ApprovedBy = _currentUser.GetUserEmail() ?? _currentUser.Name ?? "system";
        run.ApprovedOn = PakistanTime.Now;

        await _runRepository.UpdateAsync(run, true);
    }

    public async Task RejectAsync(int id, string reason, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        if (run.Status != PayrollRunStatus.PendingApproval && run.Status != PayrollRunStatus.Approved)
            throw new BadRequestException("Only PendingApproval or Approved runs can be rejected back to Calculated.");

        run.Status = PayrollRunStatus.Calculated;
        run.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Rejected by approver" : reason.Trim();

        await _runRepository.UpdateAsync(run, true);
    }

    public async Task<PayrollGlPreviewDto> GetGlPreviewAsync(int id, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(i => i.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        return await BuildGlPreviewAsync(run, cancellationToken);
    }

    public async Task<string> PostAsync(int id, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .Include(x => x.Items)
            .ThenInclude(i => i.Components)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        if (run.Status != PayrollRunStatus.Approved)
            throw new BadRequestException($"Only Approved payroll runs can be posted to GL (current status: {run.Status}).");

        var preview = await BuildGlPreviewAsync(run, cancellationToken);
        if (preview.ValidationErrors.Count > 0)
            throw new BadRequestException(string.Join(" | ", preview.ValidationErrors));

        if (!preview.IsBalanced)
            throw new BadRequestException("GL posting preview is not balanced.");

        var voucherNo = await GenerateNextGlVoucherNoAsync(PayrollVType, cancellationToken);
        int seq = 1;

        foreach (var line in preview.Lines)
        {
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = run.PayDate,
                VTime = PakistanTime.CurrentTime,
                VoucherNo = voucherNo,
                VType = PayrollVType,
                VSeq = seq++,
                DrAccountId = line.DrAccountId,
                CrAccountId = line.CrAccountId,
                Amount = line.Amount,
                NarrationId = " ",
                Remarks = line.Remarks,
                Clear = 1
            }, false);
        }

        // Apply loan schedule deductions for each posted item with loan recovery
        var empIdsWithLoan = run.Items.Where(i => i.LoanRecoveryAmount > 0).Select(i => i.HrInfoId).ToHashSet();
        if (empIdsWithLoan.Count > 0)
        {
            var dueSchedules = await _loanScheduleRepository.GetAll()
                .Include(s => s.EmployeeLoan)
                .Where(s => s.Status == LoanInstallmentStatus.Scheduled
                            && s.EmployeeLoan.Status == EmployeeLoanStatus.Active
                            && empIdsWithLoan.Contains(s.EmployeeLoan.HrInfoId)
                            && s.DueYear == run.PayYear
                            && s.DueMonth == run.PayMonth)
                .OrderBy(s => s.EmployeeLoanId)
                .ThenBy(s => s.InstallmentNo)
                .ToListAsync(cancellationToken);

            foreach (var item in run.Items.Where(i => i.LoanRecoveryAmount > 0))
            {
                decimal remainingToAllocate = item.LoanRecoveryAmount;
                var empSchedules = dueSchedules.Where(s => s.EmployeeLoan.HrInfoId == item.HrInfoId).ToList();
                foreach (var sched in empSchedules)
                {
                    if (remainingToAllocate <= 0m)
                        break;

                    var deduct = Math.Min(sched.ScheduledAmount, remainingToAllocate);
                    sched.DeductedAmount = deduct;
                    sched.Status = LoanInstallmentStatus.Deducted;
                    sched.PayrollRunItemId = item.Id;
                    remainingToAllocate -= deduct;

                    sched.EmployeeLoan.RemainingBalance = Math.Max(0m, Math.Round(sched.EmployeeLoan.RemainingBalance - deduct, 2, MidpointRounding.AwayFromZero));
                    if (sched.EmployeeLoan.RemainingBalance <= 0m)
                        sched.EmployeeLoan.Status = EmployeeLoanStatus.Completed;

                    await _loanScheduleRepository.UpdateAsync(sched, false);
                    await _loanRepository.UpdateAsync(sched.EmployeeLoan, false);
                }
            }
        }

        run.GlVoucherNo = voucherNo;
        run.Status = PayrollRunStatus.Posted;
        run.PostedBy = _currentUser.GetUserEmail() ?? _currentUser.Name ?? "system";
        run.PostedOn = PakistanTime.Now;

        await _runRepository.UpdateAsync(run, false);
        await _runRepository.SaveChangesAsync(cancellationToken);

        return voucherNo;
    }

    public async Task<string> ReverseAsync(int id, string reason, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        if (run.Status != PayrollRunStatus.Posted)
            throw new BadRequestException("Only Posted payroll runs (with no active disbursements) can be reversed.");

        var hasDisbursements = await _disbursementRepository.GetAll()
            .AnyAsync(x => x.PayrollRunId == id, cancellationToken);
        if (hasDisbursements)
            throw new BadRequestException("Void all salary disbursements for this run before reversing the payroll posting.");

        var originalGl = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == PayrollVType && x.VoucherNo == run.GlVoucherNo)
            .OrderBy(x => x.VSeq)
            .ToListAsync(cancellationToken);

        var revVoucherNo = await GenerateNextGlVoucherNoAsync(PayrollVType, cancellationToken);
        int seq = 1;
        var revDate = DateOnly.FromDateTime(PakistanTime.Now);

        foreach (var entry in originalGl)
        {
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = revDate,
                VTime = PakistanTime.CurrentTime,
                VoucherNo = revVoucherNo,
                VType = PayrollVType,
                VSeq = seq++,
                DrAccountId = entry.CrAccountId,
                CrAccountId = entry.DrAccountId,
                Amount = entry.Amount,
                NarrationId = " ",
                Remarks = $"REVERSAL of PR-{run.GlVoucherNo} ({run.RunNo}): {reason}",
                Clear = 1
            }, false);
        }

        // Restore any loan schedules linked to this run's items
        var itemIds = run.Items.Select(i => i.Id).ToHashSet();
        var deductedSchedules = await _loanScheduleRepository.GetAll()
            .Include(s => s.EmployeeLoan)
            .Where(s => s.PayrollRunItemId.HasValue && itemIds.Contains(s.PayrollRunItemId.Value))
            .ToListAsync(cancellationToken);

        foreach (var sched in deductedSchedules)
        {
            sched.EmployeeLoan.RemainingBalance = Math.Round(sched.EmployeeLoan.RemainingBalance + sched.DeductedAmount, 2, MidpointRounding.AwayFromZero);
            if (sched.EmployeeLoan.Status == EmployeeLoanStatus.Completed && sched.EmployeeLoan.RemainingBalance > 0m)
                sched.EmployeeLoan.Status = EmployeeLoanStatus.Active;

            sched.DeductedAmount = 0m;
            sched.Status = LoanInstallmentStatus.Scheduled;
            sched.PayrollRunItemId = null;

            await _loanScheduleRepository.UpdateAsync(sched, false);
            await _loanRepository.UpdateAsync(sched.EmployeeLoan, false);
        }

        run.ReversalGlVoucherNo = revVoucherNo;
        run.Status = PayrollRunStatus.Reversed;
        run.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Reversed" : reason.Trim();

        await _runRepository.UpdateAsync(run, false);
        await _runRepository.SaveChangesAsync(cancellationToken);

        return revVoucherNo;
    }

    public async Task CancelAsync(int id, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{id} not found.");

        if (run.Status != PayrollRunStatus.Draft && run.Status != PayrollRunStatus.Calculated)
            throw new BadRequestException("Only Draft or Calculated payroll runs can be cancelled.");

        run.Status = PayrollRunStatus.Cancelled;
        await _runRepository.UpdateAsync(run, true);
    }

    public async Task<byte[]> GeneratePayslipPdfAsync(int runId, int itemId, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(i => i.Components)
            .FirstOrDefaultAsync(x => x.Id == runId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{runId} not found.");

        var item = run.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException($"Payroll item #{itemId} not found.");

        var company = await _companyRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var model = await BuildPayslipModelAsync(run, item, company, cancellationToken);
        return new PayslipDocument(model).GeneratePdf();
    }

    public async Task<byte[]> GenerateBulkPayslipsPdfAsync(int runId, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(i => i.Components)
            .FirstOrDefaultAsync(x => x.Id == runId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{runId} not found.");

        if (run.Items.Count == 0)
            throw new BadRequestException("Payroll run has no employees.");

        var company = await _companyRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var models = new List<PayslipModel>();
        foreach (var item in run.Items.OrderBy(x => x.EmployeeName))
        {
            models.Add(await BuildPayslipModelAsync(run, item, company, cancellationToken));
        }

        return new PayslipDocument(models).GeneratePdf();
    }

    public async Task<byte[]> GeneratePayrollRegisterPdfAsync(int runId, CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == runId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{runId} not found.");

        var company = await _companyRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var header = new PayrollRegisterHeader
        {
            CompanyName = company?.CompanyName ?? "RetailSuite Enterprise",
            RunNo = run.RunNo,
            PayPeriodLabel = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(run.PayMonth)} {run.PayYear}",
            PayDate = run.PayDate,
            SalaryType = run.SalaryType,
            Status = run.Status.ToString(),
            GeneratedAt = PakistanTime.Now
        };

        var rows = run.Items.OrderBy(x => x.Department).ThenBy(x => x.EmployeeName).Select(i => new PayrollRegisterRow
        {
            EmployeeId = i.HrInfoId,
            EmployeeName = i.EmployeeName,
            Department = string.IsNullOrWhiteSpace(i.Department) ? "General" : i.Department,
            Designation = i.Designation ?? "-",
            PaymentMethod = i.PaymentMethod,
            BasicSalary = i.BasicSalary,
            AllowancesAndBonus = Math.Max(0m, i.GrossEarnings - i.BasicSalary),
            GrossEarnings = i.GrossEarnings,
            OtherDeductions = i.TotalDeductions,
            LoanRecovery = i.LoanRecoveryAmount,
            NetSalary = i.NetSalary,
            PaidAmount = i.PaidAmount,
            IsOnHold = i.IsOnHold
        }).ToList();

        return new PayrollRegisterDocument(header, rows).GeneratePdf();
    }

    private async Task<PayslipModel> BuildPayslipModelAsync(
        PayrollRun run,
        PayrollRunItem item,
        CompanyDetail? company,
        CancellationToken cancellationToken)
    {
        var activeLoans = await _loanRepository.GetAll()
            .AsNoTracking()
            .Where(l => l.HrInfoId == item.HrInfoId && (l.Status == EmployeeLoanStatus.Active || l.Status == EmployeeLoanStatus.Completed))
            .OrderByDescending(l => l.CreatedOn)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new PayslipModel
        {
            CompanyName = company?.CompanyName ?? "RetailSuite Enterprise",
            CompanyAddress = company?.Address,
            CompanyPhone = company?.Phone ?? company?.Cell,
            RunNo = run.RunNo,
            PayPeriodLabel = $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(run.PayMonth)} {run.PayYear}",
            PayDate = run.PayDate,
            SalaryType = run.SalaryType,
            EmployeeId = item.HrInfoId,
            EmployeeName = item.EmployeeName,
            Designation = item.Designation,
            Department = item.Department,
            Cnic = item.Cnic,
            PaymentMethod = item.PaymentMethod,
            BankName = item.BankName,
            BankAccountNo = item.BankAccountNo,
            Iban = item.Iban,
            WorkingDays = item.WorkingDays,
            PresentDays = item.PresentDays,
            UnpaidLeaveDays = item.UnpaidLeaveDays,
            OvertimeHours = item.OvertimeHours,
            Earnings = item.Components
                .Where(c => c.ComponentType == PayrollComponentType.Earning && c.ShowOnPayslip)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new PayslipComponentLine { Code = c.ComponentCode, Name = c.ComponentName, Quantity = c.Quantity, Rate = c.Rate, Amount = c.Amount })
                .ToList(),
            Deductions = item.Components
                .Where(c => c.ComponentType == PayrollComponentType.Deduction && c.ShowOnPayslip)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new PayslipComponentLine { Code = c.ComponentCode, Name = c.ComponentName, Quantity = c.Quantity, Rate = c.Rate, Amount = c.Amount })
                .ToList(),
            ActiveLoans = activeLoans
                .Where(l => l.RemainingBalance > 0 || item.LoanRecoveryAmount > 0)
                .Select(l => new PayslipLoanSummary
                {
                    LoanNo = l.LoanNo,
                    Principal = l.PrincipalAmount,
                    DeductedThisPeriod = item.LoanRecoveryAmount,
                    RemainingBalance = l.RemainingBalance
                }).ToList(),
            GrossEarnings = item.GrossEarnings,
            TotalDeductions = item.TotalDeductions,
            LoanRecoveryAmount = item.LoanRecoveryAmount,
            NetSalary = item.NetSalary,
            Remarks = item.Remarks,
            GeneratedAt = PakistanTime.Now
        };
    }

    private async Task<PayrollGlPreviewDto> BuildGlPreviewAsync(PayrollRun run, CancellationToken cancellationToken)
    {
        var preview = new PayrollGlPreviewDto
        {
            PayrollRunId = run.Id,
            RunNo = run.RunNo
        };

        var allCoa = await _coaRepository.GetAll().AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var empLoanAccounts = await _hrInfoRepository.GetAll().AsNoTracking()
            .Where(x => x.LoanReceivableAccountId != null)
            .ToDictionaryAsync(x => x.Id, x => x.LoanReceivableAccountId!, cancellationToken);

        foreach (var item in run.Items.OrderBy(x => x.EmployeeName))
        {
            var expenseAcc = !string.IsNullOrWhiteSpace(item.ExpenseAccountId) ? item.ExpenseAccountId : DefaultExpenseAccountId;
            var payableAcc = !string.IsNullOrWhiteSpace(item.PayableAccountId) ? item.PayableAccountId : DefaultPayableAccountId;

            if (!allCoa.ContainsKey(expenseAcc))
                preview.ValidationErrors.Add($"Expense account '{expenseAcc}' for {item.EmployeeName} not found in Chart of Accounts.");
            if (!allCoa.ContainsKey(payableAcc))
                preview.ValidationErrors.Add($"Payable account '{payableAcc}' for {item.EmployeeName} not found in Chart of Accounts.");

            if (item.NetSalary > 0m)
            {
                preview.Lines.Add(new PayrollGlPreviewLineDto
                {
                    DrAccountId = expenseAcc,
                    DrAccountTitle = allCoa.GetValueOrDefault(expenseAcc, expenseAcc),
                    CrAccountId = payableAcc,
                    CrAccountTitle = allCoa.GetValueOrDefault(payableAcc, payableAcc),
                    Amount = item.NetSalary,
                    Remarks = $"Payroll {run.RunNo} ({run.PayYear}-{run.PayMonth:D2}) Net Salary - {item.EmployeeName}"
                });
            }

            if (item.LoanRecoveryAmount > 0m)
            {
                var loanRecAcc = empLoanAccounts.GetValueOrDefault(item.HrInfoId, DefaultLoanReceivableAccountId);
                if (!allCoa.ContainsKey(loanRecAcc))
                    preview.ValidationErrors.Add($"Loan receivable account '{loanRecAcc}' for {item.EmployeeName} not found in Chart of Accounts.");

                preview.Lines.Add(new PayrollGlPreviewLineDto
                {
                    DrAccountId = expenseAcc,
                    DrAccountTitle = allCoa.GetValueOrDefault(expenseAcc, expenseAcc),
                    CrAccountId = loanRecAcc,
                    CrAccountTitle = allCoa.GetValueOrDefault(loanRecAcc, loanRecAcc),
                    Amount = item.LoanRecoveryAmount,
                    Remarks = $"Payroll {run.RunNo} Loan Recovery - {item.EmployeeName}"
                });
            }
        }

        preview.TotalDebit = Math.Round(preview.Lines.Sum(l => l.Amount), 2, MidpointRounding.AwayFromZero);
        preview.TotalCredit = preview.TotalDebit;
        return preview;
    }

    private static List<PayrollComponentSpec> BuildComponentSpecs(
        List<PayrollComponent> catalog,
        SalaryStructure? structure,
        List<EmployeeSalaryComponent> overrides,
        PayrollRunItem? existingItem)
    {
        var specs = new Dictionary<string, PayrollComponentSpec>(StringComparer.OrdinalIgnoreCase);

        // Register system components for metadata lookup
        foreach (var sys in catalog.Where(c => c.IsSystem))
        {
            specs[sys.Code] = new PayrollComponentSpec(
                sys.Id, sys.Code, sys.Name, sys.ComponentType, sys.CalculationType,
                sys.DefaultValue, sys.FormulaExpression, sys.ShowOnPayslip, sys.DisplayOrder,
                sys.ComponentType == PayrollComponentType.Earning ? sys.ExpenseAccountId : sys.LiabilityAccountId);
        }

        // Apply Salary Structure lines
        if (structure is not null)
        {
            foreach (var line in structure.Lines)
            {
                var comp = catalog.FirstOrDefault(c => c.Id == line.PayrollComponentId);
                if (comp is null) continue;

                specs[comp.Code] = new PayrollComponentSpec(
                    comp.Id,
                    comp.Code,
                    comp.Name,
                    comp.ComponentType,
                    line.CalculationTypeOverride ?? comp.CalculationType,
                    line.Value,
                    line.FormulaOverride ?? comp.FormulaExpression,
                    comp.ShowOnPayslip,
                    line.DisplayOrder > 0 ? line.DisplayOrder : comp.DisplayOrder,
                    comp.ComponentType == PayrollComponentType.Earning ? comp.ExpenseAccountId : comp.LiabilityAccountId);
            }
        }

        // Apply per-employee overrides
        foreach (var ov in overrides)
        {
            var comp = catalog.FirstOrDefault(c => c.Id == ov.PayrollComponentId);
            if (comp is null) continue;

            specs[comp.Code] = new PayrollComponentSpec(
                comp.Id,
                comp.Code,
                comp.Name,
                comp.ComponentType,
                ov.CalculationTypeOverride ?? comp.CalculationType,
                ov.Value,
                ov.FormulaOverride ?? comp.FormulaExpression,
                comp.ShowOnPayslip,
                comp.DisplayOrder,
                comp.ComponentType == PayrollComponentType.Earning ? comp.ExpenseAccountId : comp.LiabilityAccountId);
        }

        var result = specs.Values.ToList();

        // Preserve any ad-hoc manual components from existing item during recalculation
        if (existingItem is not null)
        {
            foreach (var manual in existingItem.Components.Where(c => c.IsManualOverride && !c.ComponentCode.Equals("BONUS", StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(new PayrollComponentSpec(
                    manual.PayrollComponentId,
                    manual.ComponentCode,
                    manual.ComponentName,
                    manual.ComponentType,
                    PayrollCalculationType.Fixed,
                    manual.Amount,
                    null,
                    manual.ShowOnPayslip,
                    manual.DisplayOrder,
                    manual.GlAccountId,
                    IsManualOverride: true));
            }
        }

        return result;
    }

    private static PayrollRunItemComponent MapCalculatedComponent(PayrollCalculatedComponent c) => new()
    {
        PayrollComponentId = c.PayrollComponentId,
        ComponentCode = c.ComponentCode,
        ComponentName = c.ComponentName,
        ComponentType = c.ComponentType,
        CalculationType = c.CalculationType,
        Quantity = c.Quantity,
        Rate = c.Rate,
        Amount = c.Amount,
        IsManualOverride = c.IsManualOverride,
        ShowOnPayslip = c.ShowOnPayslip,
        DisplayOrder = c.DisplayOrder,
        GlAccountId = c.GlAccountId
    };

    private static void RecalculateRunTotals(PayrollRun run)
    {
        run.EmployeeCount = run.Items.Count;
        run.TotalGrossEarnings = Math.Round(run.Items.Sum(x => x.GrossEarnings), 2, MidpointRounding.AwayFromZero);
        run.TotalDeductions = Math.Round(run.Items.Sum(x => x.TotalDeductions), 2, MidpointRounding.AwayFromZero);
        run.TotalLoanRecovery = Math.Round(run.Items.Sum(x => x.LoanRecoveryAmount), 2, MidpointRounding.AwayFromZero);
        run.TotalNetSalary = Math.Round(run.Items.Sum(x => x.NetSalary), 2, MidpointRounding.AwayFromZero);
        run.TotalPaidAmount = Math.Round(run.Items.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero);
    }

    private static string ResolveAccountId(params string?[] candidates)
    {
        foreach (var c in candidates)
        {
            if (!string.IsNullOrWhiteSpace(c))
                return c.Trim();
        }
        return string.Empty;
    }

    private async Task<string> GenerateNextRunNoAsync(int year, int month, CancellationToken cancellationToken)
    {
        var prefix = $"PR-{year:D4}{month:D2}-";
        var existing = await _runRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.RunNo.StartsWith(prefix))
            .Select(x => x.RunNo)
            .ToListAsync(cancellationToken);

        int max = 0;
        foreach (var no in existing)
        {
            var suffix = no[prefix.Length..];
            if (int.TryParse(suffix, out var val) && val > max)
                max = val;
        }

        return $"{prefix}{(max + 1):D3}";
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

    private static PayrollRunSummaryDto MapSummary(PayrollRun x) => new()
    {
        Id = x.Id,
        RunNo = x.RunNo,
        PayYear = x.PayYear,
        PayMonth = x.PayMonth,
        PeriodStart = x.PeriodStart,
        PeriodEnd = x.PeriodEnd,
        PayDate = x.PayDate,
        SalaryType = x.SalaryType,
        Status = x.Status,
        Description = x.Description,
        EmployeeCount = x.EmployeeCount,
        TotalGrossEarnings = x.TotalGrossEarnings,
        TotalDeductions = x.TotalDeductions,
        TotalLoanRecovery = x.TotalLoanRecovery,
        TotalNetSalary = x.TotalNetSalary,
        TotalPaidAmount = x.TotalPaidAmount,
        GlVoucherNo = x.GlVoucherNo,
        ReversalGlVoucherNo = x.ReversalGlVoucherNo,
        SubmittedBy = x.SubmittedBy,
        SubmittedOn = x.SubmittedOn,
        ApprovedBy = x.ApprovedBy,
        ApprovedOn = x.ApprovedOn,
        PostedBy = x.PostedBy,
        PostedOn = x.PostedOn,
        RejectionReason = x.RejectionReason,
        CreatedBy = x.CreatedBy,
        CreatedOn = x.CreatedOn
    };

    private static PayrollRunDetailDto MapDetail(PayrollRun x) => new()
    {
        Id = x.Id,
        RunNo = x.RunNo,
        PayYear = x.PayYear,
        PayMonth = x.PayMonth,
        PeriodStart = x.PeriodStart,
        PeriodEnd = x.PeriodEnd,
        PayDate = x.PayDate,
        SalaryType = x.SalaryType,
        Status = x.Status,
        Description = x.Description,
        EmployeeCount = x.EmployeeCount,
        TotalGrossEarnings = x.TotalGrossEarnings,
        TotalDeductions = x.TotalDeductions,
        TotalLoanRecovery = x.TotalLoanRecovery,
        TotalNetSalary = x.TotalNetSalary,
        TotalPaidAmount = x.TotalPaidAmount,
        GlVoucherNo = x.GlVoucherNo,
        ReversalGlVoucherNo = x.ReversalGlVoucherNo,
        SubmittedBy = x.SubmittedBy,
        SubmittedOn = x.SubmittedOn,
        ApprovedBy = x.ApprovedBy,
        ApprovedOn = x.ApprovedOn,
        PostedBy = x.PostedBy,
        PostedOn = x.PostedOn,
        RejectionReason = x.RejectionReason,
        CreatedBy = x.CreatedBy,
        CreatedOn = x.CreatedOn,
        Items = x.Items.OrderBy(i => i.EmployeeName).Select(MapItem).ToList()
    };

    private static PayrollRunItemDto MapItem(PayrollRunItem i) => new()
    {
        Id = i.Id,
        HrInfoId = i.HrInfoId,
        EmployeeName = i.EmployeeName,
        Designation = i.Designation,
        Department = i.Department,
        Cnic = i.Cnic,
        PaymentMethod = i.PaymentMethod,
        BankName = i.BankName,
        BankAccountTitle = i.BankAccountTitle,
        BankAccountNo = i.BankAccountNo,
        Iban = i.Iban,
        ExpenseAccountId = i.ExpenseAccountId,
        PayableAccountId = i.PayableAccountId,
        WorkingDays = i.WorkingDays,
        PresentDays = i.PresentDays,
        UnpaidLeaveDays = i.UnpaidLeaveDays,
        LeaveChargeRate = i.LeaveChargeRate,
        OvertimeHours = i.OvertimeHours,
        OvertimeRate = i.OvertimeRate,
        BonusAmount = i.BonusAmount,
        BasicSalary = i.BasicSalary,
        GrossEarnings = i.GrossEarnings,
        TotalDeductions = i.TotalDeductions,
        LoanRecoveryAmount = i.LoanRecoveryAmount,
        NetSalary = i.NetSalary,
        PaidAmount = i.PaidAmount,
        IsOnHold = i.IsOnHold,
        Remarks = i.Remarks,
        Components = i.Components
            .OrderBy(c => c.ComponentType)
            .ThenBy(c => c.DisplayOrder)
            .Select(c => new PayrollRunItemComponentDto
            {
                Id = c.Id,
                PayrollComponentId = c.PayrollComponentId,
                ComponentCode = c.ComponentCode,
                ComponentName = c.ComponentName,
                ComponentType = c.ComponentType,
                CalculationType = c.CalculationType,
                Quantity = c.Quantity,
                Rate = c.Rate,
                Amount = c.Amount,
                IsManualOverride = c.IsManualOverride,
                ShowOnPayslip = c.ShowOnPayslip,
                DisplayOrder = c.DisplayOrder,
                GlAccountId = c.GlAccountId
            }).ToList()
    };
}
