using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Payrolls;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Payrolls;

internal class PayrollService : IPayrollService
{
    private const string VType = "PL";

    private readonly IRepository<Payroll> _payrollRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<HrInfo> _hrInfoRepository;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;

    public PayrollService(
        IRepository<Payroll> payrollRepository,
        IRepository<GlEntry> glRepository,
        IRepository<HrInfo> hrInfoRepository,
        IRepository<ChartOfAccount> chartOfAccountRepository)
    {
        _payrollRepository = payrollRepository;
        _glRepository = glRepository;
        _hrInfoRepository = hrInfoRepository;
        _chartOfAccountRepository = chartOfAccountRepository;
    }

    public async Task<List<PayrollResponse>> GetListAsync(PayrollListFilter filter, CancellationToken cancellationToken)
    {
        var query = _payrollRepository.GetAll().AsNoTracking();

        if (filter.FromDate.HasValue)
            query = query.Where(x => x.VDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(x => x.VDate <= filter.ToDate.Value);

        return await query
            .GroupBy(x => new { x.VoucherNo, x.VDate, x.SalaryType })
            .Select(g => new PayrollResponse
            {
                VoucherNo = g.Key.VoucherNo,
                Date = g.Key.VDate,
                SalaryType = g.Key.SalaryType,
                Amount = g.Sum(x => x.NetSalary),
                CreatedBy = g.Min(x => x.CreatedBy) ?? string.Empty,
                CreatedOn = g.Min(x => x.CreatedOn),
                LastModifiedBy = g.Max(x => x.LastModifiedBy),
                LastModifiedOn = g.Max(x => x.LastModifiedOn)
            })
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.VoucherNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PayrollLineResponse>> GetDetailAsync(string voucherNo, CancellationToken cancellationToken)
    {
        return await (
            from p in _payrollRepository.GetAll().AsNoTracking()
            join h in _hrInfoRepository.GetAll().AsNoTracking() on p.HrInfoId equals h.Id into hrJoin
            from h in hrJoin.DefaultIfEmpty()
            where p.VoucherNo == voucherNo
            orderby p.Seq
            select new PayrollLineResponse
            {
                Seq = p.Seq,
                Date = p.VDate,
                VoucherNo = p.VoucherNo,
                SalaryType = p.SalaryType,
                Description = p.Description,
                HrId = p.HrInfoId!,
                HrName = h != null ? h.Name : null,
                PayableAccount = p.PayableAccountId!,
                ExpenseAccount = p.ExpenseAccountId!,
                Salary = p.Salary,
                NoOfLeaves = p.NoOfLeaves,
                LeaveCharges = p.LeaveCharges,
                Overtime = p.Overtime,
                OvertimeCharges = p.OvertimeCharges,
                Bonus = p.Bonus,
                NetSalary = p.NetSalary,
                Remarks = p.Remarks,
                CreatedBy = p.CreatedBy,
                CreatedOn = p.CreatedOn,
                LastModifiedBy = p.LastModifiedBy,
                LastModifiedOn = p.LastModifiedOn
            }).ToListAsync(cancellationToken);
    }

    public async Task<PayrollLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken)
    {
        var employees = await _hrInfoRepository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new PayrollEmployeeLookupResponse
            {
                Id = x.Id,
                Name = x.Name,
                SalaryType = x.SalaryType,
                Salary = x.Salary,
                LeaveCharges = x.LeaveCharges,
                Overtime = x.Overtime,
                PayableAccount = x.PayableAccount,
                ExpenseAccount = x.ExpenseAccount
            })
            .ToListAsync(cancellationToken);

        var expenseAccounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.Id.StartsWith("004"))
            .OrderBy(x => x.Title)
            .Select(x => new PayrollLookupItemResponse
            {
                Code = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);

        var payableAccounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.Id.StartsWith("002"))
            .OrderBy(x => x.Title)
            .Select(x => new PayrollLookupItemResponse
            {
                Code = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);

        return new PayrollLookupsResponse
        {
            Employees = employees,
            ExpenseAccounts = expenseAccounts,
            PayableAccounts = payableAccounts
        };
    }

    public async Task<string> CreateAsync(PayrollUpsertRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var maxVoucherNo = await _payrollRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .MaxAsync(x => (string?)x.VoucherNo, cancellationToken);

        var nextNum = maxVoucherNo == null ? 1L : long.Parse(maxVoucherNo) + 1;
        var voucherNo = nextNum.ToString("D5");

        foreach (var line in request.Lines)
        {
            await _payrollRepository.AddAsync(new Payroll
            {
                VoucherNo = voucherNo,
                VDate = request.Date,
                SalaryType = request.SalaryType,
                Description = request.Description,
                Seq = line.Seq,
                HrInfoId = line.HrId,
                PayableAccountId = line.PayableAccount,
                ExpenseAccountId = line.ExpenseAccount,
                Salary = line.Salary,
                NoOfLeaves = line.NoOfLeaves,
                LeaveCharges = line.LeaveCharges,
                Overtime = line.Overtime,
                OvertimeCharges = line.OvertimeCharges,
                Bonus = line.Bonus,
                NetSalary = line.NetSalary,
                Remarks = line.Remarks
            }, false);

            await UpsertGlEntryAsync(voucherNo, request, line, cancellationToken);
        }

        await _payrollRepository.SaveChangesAsync(cancellationToken);
        return voucherNo;
    }

    public async Task UpdateAsync(string voucherNo, PayrollUpsertRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var hasVoucher = await _payrollRepository.GetAll()
            .AnyAsync(x => x.VoucherNo == voucherNo, cancellationToken);

        if (!hasVoucher)
            throw new NotFoundException($"Payroll voucher '{voucherNo}' not found.");

        foreach (var line in request.Lines)
        {
            var existing = await _payrollRepository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .FirstOrDefaultAsync(x => x.VoucherNo == voucherNo && x.Seq == line.Seq, cancellationToken);

            if (existing is null)
            {
                await _payrollRepository.AddAsync(new Payroll
                {
                    VoucherNo = voucherNo,
                    VDate = request.Date,
                    SalaryType = request.SalaryType,
                    Description = request.Description,
                    Seq = line.Seq,
                    HrInfoId = line.HrId,
                    PayableAccountId = line.PayableAccount,
                    ExpenseAccountId = line.ExpenseAccount,
                    Salary = line.Salary,
                    NoOfLeaves = line.NoOfLeaves,
                    LeaveCharges = line.LeaveCharges,
                    Overtime = line.Overtime,
                    OvertimeCharges = line.OvertimeCharges,
                    Bonus = line.Bonus,
                    NetSalary = line.NetSalary,
                    Remarks = line.Remarks
                }, false);
            }
            else
            {
                existing.DeletedOn = null;
                existing.DeletedBy = null;
                existing.VDate = request.Date;
                existing.SalaryType = request.SalaryType;
                existing.Description = request.Description;
                existing.HrInfoId = line.HrId;
                existing.PayableAccountId = line.PayableAccount;
                existing.ExpenseAccountId = line.ExpenseAccount;
                existing.Salary = line.Salary;
                existing.NoOfLeaves = line.NoOfLeaves;
                existing.LeaveCharges = line.LeaveCharges;
                existing.Overtime = line.Overtime;
                existing.OvertimeCharges = line.OvertimeCharges;
                existing.Bonus = line.Bonus;
                existing.NetSalary = line.NetSalary;
                existing.Remarks = line.Remarks;

                await _payrollRepository.UpdateAsync(existing, false);
            }

            await UpsertGlEntryAsync(voucherNo, request, line, cancellationToken);
        }

        await _payrollRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string voucherNo, CancellationToken cancellationToken)
    {
        var lines = await _payrollRepository.GetAll()
            .Where(x => x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        var glEntries = await _glRepository.GetAll()
            .Where(x => x.VType == VType && x.VoucherNo == voucherNo)
            .ToListAsync(cancellationToken);

        await _payrollRepository.DeleteRangeAsync(lines, true);
        await _glRepository.DeleteRangeAsync(glEntries, true);
    }

    public async Task DeleteLineAsync(string voucherNo, long seq, CancellationToken cancellationToken)
    {
        var line = await _payrollRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VoucherNo == voucherNo && x.Seq == seq, cancellationToken);

        if (line is not null)
            await _payrollRepository.DeleteAsync(line, false);

        var gl = await _glRepository.GetAll()
            .FirstOrDefaultAsync(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == seq, cancellationToken);

        if (gl is not null)
            await _glRepository.DeleteAsync(gl, false);

        await _payrollRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertGlEntryAsync(
        string voucherNo,
        PayrollUpsertRequest request,
        PayrollLineRequest line,
        CancellationToken cancellationToken)
    {
        var gl = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .FirstOrDefaultAsync(x => x.VType == VType && x.VoucherNo == voucherNo && x.VSeq == line.Seq, cancellationToken);

        if (gl is null)
        {
            await _glRepository.AddAsync(new GlEntry
            {
                VDate = request.Date,
                VTime = TimeOnly.FromDateTime(DateTime.Now),
                VoucherNo = voucherNo,
                VType = VType,
                VSeq = (int)line.Seq,
                DrAccountId = line.ExpenseAccount,
                CrAccountId = line.PayableAccount,
                Amount = line.NetSalary,
                NarrationId = " ",
                Remarks = string.IsNullOrWhiteSpace(line.Remarks) ? request.Description : line.Remarks,
                Clear = 1
            }, false);
        }
        else
        {
            gl.DeletedOn = null;
            gl.DeletedBy = null;
            gl.VDate = request.Date;
            gl.VTime = TimeOnly.FromDateTime(DateTime.Now);
            gl.DrAccountId = line.ExpenseAccount;
            gl.CrAccountId = line.PayableAccount;
            gl.Amount = line.NetSalary;
            gl.NarrationId = " ";
            gl.Remarks = request.Description;
            gl.Clear = 1;

            await _glRepository.UpdateAsync(gl, false);
        }
    }

    private static void ValidateRequest(PayrollUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SalaryType))
            throw new BadRequestException("Salary type is required.");

        if (request.Lines is null || request.Lines.Count == 0)
            throw new BadRequestException("At least one payroll line is required.");
    }
}
