using System.Text;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Payroll.Disbursements;
using Retailer.Domain.Legacy;
using Retailer.Domain.Payroll;
using Retailer.Shared.Common;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.EnterprisePayroll;

internal class PayrollDisbursementService : IPayrollDisbursementService
{
    private const string PaymentVType = "PV";
    private const string DefaultSalariesPayableAccountId = "002001005001001";

    private readonly IRepository<PayrollDisbursement> _disbursementRepository;
    private readonly IRepository<PayrollRun> _runRepository;
    private readonly IRepository<PayrollRunItem> _runItemRepository;
    private readonly IRepository<ChartOfAccount> _coaRepository;
    private readonly IRepository<GlEntry> _glRepository;

    public PayrollDisbursementService(
        IRepository<PayrollDisbursement> disbursementRepository,
        IRepository<PayrollRun> runRepository,
        IRepository<PayrollRunItem> runItemRepository,
        IRepository<ChartOfAccount> coaRepository,
        IRepository<GlEntry> glRepository)
    {
        _disbursementRepository = disbursementRepository;
        _runRepository = runRepository;
        _runItemRepository = runItemRepository;
        _coaRepository = coaRepository;
        _glRepository = glRepository;
    }

    public async Task<List<PayrollDisbursementDto>> GetByRunIdAsync(int payrollRunId, CancellationToken cancellationToken)
    {
        var list = await _disbursementRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.PayrollRun)
            .Include(x => x.Lines)
            .Where(x => x.PayrollRunId == payrollRunId)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return await MapListAsync(list, cancellationToken);
    }

    public async Task<PayrollDisbursementDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _disbursementRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.PayrollRun)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        var mapped = await MapListAsync([entity], cancellationToken);
        return mapped.FirstOrDefault();
    }

    public async Task<PayrollDisbursementDto> CreateAsync(PayrollDisbursementCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceAccountId))
            throw new BadRequestException("Source Cash/Bank account is required.");

        if (!await _coaRepository.GetAll().AnyAsync(x => x.Id == request.SourceAccountId, cancellationToken))
            throw new BadRequestException($"Source account '{request.SourceAccountId}' not found in Chart of Accounts.");

        var run = await _runRepository.GetAll()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.PayrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{request.PayrollRunId} not found.");

        if (run.Status != PayrollRunStatus.Posted && run.Status != PayrollRunStatus.PartiallyPaid)
            throw new BadRequestException($"Only Posted or PartiallyPaid payroll runs can be disbursed (current status: {run.Status}).");

        var candidateItems = run.Items
            .Where(x => !x.IsOnHold && x.NetSalary > x.PaidAmount)
            .ToList();

        if (request.PayrollRunItemIds is { Count: > 0 })
        {
            var idSet = request.PayrollRunItemIds.ToHashSet();
            candidateItems = candidateItems.Where(x => idSet.Contains(x.Id)).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(request.PaymentMethod) && !request.PaymentMethod.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            candidateItems = candidateItems
                .Where(x => x.PaymentMethod.Equals(request.PaymentMethod, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (candidateItems.Count == 0)
            throw new BadRequestException("No eligible unpaid, non-held employees found for this disbursement.");

        var disbursementNo = await GenerateNextDisbursementNoAsync(cancellationToken);
        var glVoucherNo = await GenerateNextGlVoucherNoAsync(PaymentVType, cancellationToken);

        var disbursement = new PayrollDisbursement
        {
            DisbursementNo = disbursementNo,
            PayrollRunId = run.Id,
            PaymentDate = request.PaymentDate,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "BankTransfer" : request.PaymentMethod.Trim(),
            SourceAccountId = request.SourceAccountId.Trim(),
            GlVoucherNo = glVoucherNo,
            ReferenceNo = request.ReferenceNo?.Trim(),
            Remarks = request.Remarks?.Trim()
        };

        decimal totalDisbursed = 0m;
        int seq = 1;

        foreach (var item in candidateItems)
        {
            var remaining = Math.Round(item.NetSalary - item.PaidAmount, 2, MidpointRounding.AwayFromZero);
            if (remaining <= 0m)
                continue;

            var payableAccountId = !string.IsNullOrWhiteSpace(item.PayableAccountId)
                ? item.PayableAccountId
                : DefaultSalariesPayableAccountId;

            disbursement.Lines.Add(new PayrollDisbursementLine
            {
                PayrollRunItemId = item.Id,
                HrInfoId = item.HrInfoId,
                EmployeeName = item.EmployeeName,
                Amount = remaining,
                BankNameSnapshot = item.BankName,
                BankAccountSnapshot = item.BankAccountNo,
                IbanSnapshot = item.Iban
            });

            await _glRepository.AddAsync(new GlEntry
            {
                VDate = request.PaymentDate,
                VTime = PakistanTime.CurrentTime,
                VoucherNo = glVoucherNo,
                VType = PaymentVType,
                VSeq = seq++,
                DrAccountId = payableAccountId,
                CrAccountId = request.SourceAccountId.Trim(),
                Amount = remaining,
                NarrationId = " ",
                Remarks = $"Salary Disbursement {disbursementNo} ({run.RunNo}) - {item.EmployeeName}",
                Clear = 1
            }, false);

            item.PaidAmount = Math.Round(item.PaidAmount + remaining, 2, MidpointRounding.AwayFromZero);
            totalDisbursed += remaining;
            await _runItemRepository.UpdateAsync(item, false);
        }

        disbursement.TotalAmount = Math.Round(totalDisbursed, 2, MidpointRounding.AwayFromZero);
        run.TotalPaidAmount = Math.Round(run.Items.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero);

        var allPaid = run.Items.Where(x => !x.IsOnHold && x.NetSalary > 0).All(x => x.PaidAmount >= x.NetSalary);
        run.Status = allPaid ? PayrollRunStatus.Paid : PayrollRunStatus.PartiallyPaid;

        await _disbursementRepository.AddAsync(disbursement, false);
        await _runRepository.UpdateAsync(run, false);
        await _disbursementRepository.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(disbursement.Id, cancellationToken))!;
    }

    public async Task VoidAsync(int id, CancellationToken cancellationToken)
    {
        var disbursement = await _disbursementRepository.GetAll()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payroll disbursement #{id} not found.");

        var run = await _runRepository.GetAll()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == disbursement.PayrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run #{disbursement.PayrollRunId} not found.");

        // Soft-delete associated GL Payment Voucher lines
        if (!string.IsNullOrWhiteSpace(disbursement.GlVoucherNo))
        {
            var glLines = await _glRepository.GetAll()
                .Where(x => x.VType == PaymentVType && x.VoucherNo == disbursement.GlVoucherNo)
                .ToListAsync(cancellationToken);
            if (glLines.Count > 0)
                await _glRepository.DeleteRangeAsync(glLines, false);
        }

        // Rollback PaidAmount on each PayrollRunItem
        foreach (var line in disbursement.Lines)
        {
            var item = run.Items.FirstOrDefault(x => x.Id == line.PayrollRunItemId);
            if (item is not null)
            {
                item.PaidAmount = Math.Max(0m, Math.Round(item.PaidAmount - line.Amount, 2, MidpointRounding.AwayFromZero));
                await _runItemRepository.UpdateAsync(item, false);
            }
        }

        run.TotalPaidAmount = Math.Round(run.Items.Sum(x => x.PaidAmount), 2, MidpointRounding.AwayFromZero);
        run.Status = run.TotalPaidAmount > 0m ? PayrollRunStatus.PartiallyPaid : PayrollRunStatus.Posted;

        await _disbursementRepository.DeleteAsync(disbursement, false);
        await _runRepository.UpdateAsync(run, false);
        await _disbursementRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<(byte[] Content, string FileName)> ExportBankAdviceCsvAsync(int disbursementId, CancellationToken cancellationToken)
    {
        var dto = await GetByIdAsync(disbursementId, cancellationToken)
            ?? throw new NotFoundException($"Payroll disbursement #{disbursementId} not found.");

        var sb = new StringBuilder();
        sb.AppendLine("Employee ID,Employee Name,Bank Name,Account Number,IBAN,Amount,Payment Date,Reference");
        foreach (var line in dto.Lines)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(line.HrInfoId),
                CsvEscape(line.EmployeeName),
                CsvEscape(line.BankNameSnapshot ?? string.Empty),
                CsvEscape(line.BankAccountSnapshot ?? string.Empty),
                CsvEscape(line.IbanSnapshot ?? string.Empty),
                line.Amount.ToString("0.00"),
                dto.PaymentDate.ToString("yyyy-MM-dd"),
                CsvEscape(dto.ReferenceNo ?? dto.DisbursementNo)));
        }

        return (Encoding.UTF8.GetBytes(sb.ToString()), $"BankAdvice_{dto.DisbursementNo}.csv");
    }

    private async Task<List<PayrollDisbursementDto>> MapListAsync(List<PayrollDisbursement> list, CancellationToken cancellationToken)
    {
        var accIds = list.Select(x => x.SourceAccountId).Distinct().ToList();
        var accMap = accIds.Count > 0
            ? await _coaRepository.GetAll().AsNoTracking()
                .Where(x => accIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken)
            : new Dictionary<string, string>();

        return list.Select(x => new PayrollDisbursementDto
        {
            Id = x.Id,
            DisbursementNo = x.DisbursementNo,
            PayrollRunId = x.PayrollRunId,
            RunNo = x.PayrollRun?.RunNo ?? string.Empty,
            PaymentDate = x.PaymentDate,
            PaymentMethod = x.PaymentMethod,
            SourceAccountId = x.SourceAccountId,
            SourceAccountTitle = accMap.GetValueOrDefault(x.SourceAccountId, x.SourceAccountId),
            TotalAmount = x.TotalAmount,
            GlVoucherNo = x.GlVoucherNo,
            ReferenceNo = x.ReferenceNo,
            Remarks = x.Remarks,
            CreatedBy = x.CreatedBy,
            CreatedOn = x.CreatedOn,
            Lines = x.Lines.Select(l => new PayrollDisbursementLineDto
            {
                Id = l.Id,
                PayrollRunItemId = l.PayrollRunItemId,
                HrInfoId = l.HrInfoId,
                EmployeeName = l.EmployeeName,
                Amount = l.Amount,
                BankNameSnapshot = l.BankNameSnapshot,
                BankAccountSnapshot = l.BankAccountSnapshot,
                IbanSnapshot = l.IbanSnapshot
            }).ToList()
        }).ToList();
    }

    private async Task<string> GenerateNextDisbursementNoAsync(CancellationToken cancellationToken)
    {
        var existing = await _disbursementRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Select(x => x.DisbursementNo)
            .ToListAsync(cancellationToken);

        long max = 0;
        foreach (var no in existing)
        {
            var part = no.StartsWith("PD-", StringComparison.OrdinalIgnoreCase) ? no[3..] : no;
            if (long.TryParse(part, out var val) && val > max)
                max = val;
        }

        return $"PD-{(max + 1):D5}";
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

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
