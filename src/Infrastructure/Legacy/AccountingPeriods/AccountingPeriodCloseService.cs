using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.AccountingPeriods;
using Retailer.Application.Legacy.Inventory;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Legacy;
using Retailer.Domain.Payroll;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Shared.Common;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.AccountingPeriods;

internal class AccountingPeriodCloseService : IAccountingPeriodCloseService
{
    private const string ClosingVType = "CL";
    private const string StatusClosed = "Closed";
    private const string StatusReopened = "Reopened";

    private readonly IRepository<AccountingPeriodClose> _periodRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ChartOfAccount> _coaRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly IRepository<TransactionFifoMapping> _fifoMappingRepository;
    private readonly IRepository<PayrollRun> _payrollRunRepository;
    private readonly IRepository<KotOrder> _kotOrderRepository;
    private readonly IFifoCostingService _fifoCostingService;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    public AccountingPeriodCloseService(
        IRepository<AccountingPeriodClose> periodRepository,
        IRepository<GlEntry> glRepository,
        IRepository<ChartOfAccount> coaRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        IRepository<TransactionFifoMapping> fifoMappingRepository,
        IRepository<PayrollRun> payrollRunRepository,
        IRepository<KotOrder> kotOrderRepository,
        IFifoCostingService fifoCostingService,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant)
    {
        _periodRepository = periodRepository;
        _glRepository = glRepository;
        _coaRepository = coaRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _fifoMappingRepository = fifoMappingRepository;
        _payrollRunRepository = payrollRunRepository;
        _kotOrderRepository = kotOrderRepository;
        _fifoCostingService = fifoCostingService;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public async Task<PeriodLockStatusResponse> GetLockStatusAsync(CancellationToken cancellationToken)
    {
        var lastClosed = await _periodRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == StatusClosed)
            .OrderByDescending(x => x.ClosingDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var (defaultEquityId, defaultEquityTitle) = await ResolveTargetEquityAccountAsync(null, cancellationToken);

        return new PeriodLockStatusResponse
        {
            LockDate = lastClosed?.ClosingDate,
            LastClosedPeriodId = lastClosed?.Id,
            LastClosedPeriodName = lastClosed?.PeriodName,
            NextOpenDate = lastClosed?.ClosingDate.AddDays(1),
            DefaultTargetEquityAccountId = defaultEquityId,
            DefaultTargetEquityAccountTitle = defaultEquityTitle
        };
    }

    public async Task<List<AccountingPeriodCloseResponse>> GetHistoryAsync(CancellationToken cancellationToken)
    {
        var periods = await _periodRepository.GetAll()
            .AsNoTracking()
            .OrderByDescending(x => x.ClosingDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        if (periods.Count == 0)
            return [];

        var latestClosedId = periods.FirstOrDefault(x => x.Status == StatusClosed)?.Id;
        var equityIds = periods.Select(x => x.TargetEquityAccountId).Distinct().ToList();
        var titleMap = await _coaRepository.GetAll()
            .AsNoTracking()
            .Where(x => equityIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        return periods.Select(p => MapResponse(p, titleMap, latestClosedId)).ToList();
    }

    public async Task<AccountingPeriodCloseDetailResponse> GetDetailAsync(int id, CancellationToken cancellationToken)
    {
        var period = await _periodRepository.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Accounting period close #{id} not found.");

        var latestClosedId = await _periodRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == StatusClosed)
            .OrderByDescending(x => x.ClosingDate)
            .ThenByDescending(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var glEntries = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == ClosingVType && x.VoucherNo == period.ClosingVoucherNo)
            .OrderBy(x => x.VSeq)
            .ToListAsync(cancellationToken);

        var accountIds = glEntries
            .SelectMany(x => new[] { x.DrAccountId, x.CrAccountId })
            .Append(period.TargetEquityAccountId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Cast<string>()
            .ToList();

        var titleMap = await _coaRepository.GetAll()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        var baseResp = MapResponse(period, titleMap, latestClosedId);
        return new AccountingPeriodCloseDetailResponse
        {
            Id = baseResp.Id,
            PeriodName = baseResp.PeriodName,
            PeriodStartDate = baseResp.PeriodStartDate,
            ClosingDate = baseResp.ClosingDate,
            Status = baseResp.Status,
            ClosingVoucherNo = baseResp.ClosingVoucherNo,
            TargetEquityAccountId = baseResp.TargetEquityAccountId,
            TargetEquityAccountTitle = baseResp.TargetEquityAccountTitle,
            ClosedDrawings = baseResp.ClosedDrawings,
            OpeningStockValue = baseResp.OpeningStockValue,
            ClosingStockValue = baseResp.ClosingStockValue,
            TotalIncome = baseResp.TotalIncome,
            TotalCogs = baseResp.TotalCogs,
            TotalExpenses = baseResp.TotalExpenses,
            NetProfitLoss = baseResp.NetProfitLoss,
            TotalAssets = baseResp.TotalAssets,
            TotalLiabilities = baseResp.TotalLiabilities,
            TotalEquity = baseResp.TotalEquity,
            IsBalanced = baseResp.IsBalanced,
            CanReopen = baseResp.CanReopen,
            Notes = baseResp.Notes,
            ClosedBy = baseResp.ClosedBy,
            ClosedOn = baseResp.ClosedOn,
            ReopenedBy = baseResp.ReopenedBy,
            ReopenedOn = baseResp.ReopenedOn,
            ReopenReason = baseResp.ReopenReason,
            ClosingEntries = glEntries.Select(g => new ClosingVoucherLineDto
            {
                Seq = g.VSeq,
                DrAccountId = g.DrAccountId ?? string.Empty,
                DrAccountTitle = g.DrAccountId != null && titleMap.TryGetValue(g.DrAccountId, out var drT) ? drT : (g.DrAccountId ?? string.Empty),
                CrAccountId = g.CrAccountId ?? string.Empty,
                CrAccountTitle = g.CrAccountId != null && titleMap.TryGetValue(g.CrAccountId, out var crT) ? crT : (g.CrAccountId ?? string.Empty),
                Amount = g.Amount,
                Remarks = g.Remarks ?? string.Empty
            }).ToList()
        };
    }

    public async Task<PeriodClosePreviewResponse> PreviewCloseAsync(PeriodClosePreviewRequest request, CancellationToken cancellationToken)
    {
        return await BuildPreviewInternalAsync(request.ClosingDate, request.TargetEquityAccountId, request.CloseDrawings, periodNameForRemarks: null, cancellationToken);
    }

    public async Task<AccountingPeriodCloseResponse> ExecuteCloseAsync(PeriodCloseExecuteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PeriodName))
            throw new BadRequestException("Period name is required.");

        await EnsureClosingCoreAccountsExistAsync(cancellationToken);

        var periodName = request.PeriodName.Trim();
        var preview = await BuildPreviewInternalAsync(
            request.ClosingDate,
            request.TargetEquityAccountId,
            request.CloseDrawings,
            periodName,
            cancellationToken);

        if (!preview.CanClose)
        {
            throw new BadRequestException(string.Join(" ", preview.ValidationErrors));
        }

        var maxVoucherNo = await _glRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.VType == ClosingVType)
            .MaxAsync(x => (string?)x.VoucherNo, cancellationToken);

        long nextNum = 1;
        if (!string.IsNullOrWhiteSpace(maxVoucherNo) && long.TryParse(maxVoucherNo, out var parsed))
        {
            nextNum = parsed + 1;
        }
        var voucherNo = nextNum.ToString("D5");

        var closeRecord = new AccountingPeriodClose
        {
            PeriodName = periodName,
            PeriodStartDate = preview.PeriodStartDate,
            ClosingDate = preview.ClosingDate,
            Status = StatusClosed,
            ClosingVoucherNo = voucherNo,
            TargetEquityAccountId = preview.TargetEquityAccountId,
            ClosedDrawings = preview.CloseDrawings,
            OpeningStockValue = preview.OpeningStockValue,
            ClosingStockValue = preview.ClosingStockValue,
            TotalIncome = preview.TotalIncome,
            TotalCogs = preview.TotalCogs,
            TotalExpenses = preview.TotalExpenses,
            NetProfitLoss = preview.NetProfitLoss,
            TotalAssets = preview.PostCloseTotalAssets,
            TotalLiabilities = preview.PostCloseTotalLiabilities,
            TotalEquity = preview.PostCloseTotalEquity,
            IsBalanced = preview.IsBalanced,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        using (PeriodClosingScope.Enter())
        {
            var vTime = PakistanTime.CurrentTime;
            foreach (var line in preview.ProposedEntries)
            {
                await _glRepository.AddAsync(new GlEntry
                {
                    VDate = preview.ClosingDate,
                    VTime = vTime,
                    VoucherNo = voucherNo,
                    VType = ClosingVType,
                    VSeq = line.Seq,
                    DrAccountId = line.DrAccountId,
                    CrAccountId = line.CrAccountId,
                    Amount = line.Amount,
                    Remarks = line.Remarks,
                    Clear = 0m
                }, doSaveChanges: false);
            }

            await _periodRepository.AddAsync(closeRecord, doSaveChanges: false);
            await _periodRepository.SaveChangesAsync(cancellationToken);
        }

        PeriodLockCache.Invalidate(_currentTenant.Id ?? "__default__");

        var titleMap = new Dictionary<string, string>
        {
            [preview.TargetEquityAccountId] = preview.TargetEquityAccountTitle
        };
        return MapResponse(closeRecord, titleMap, closeRecord.Id);
    }

    public async Task<AccountingPeriodCloseResponse> ReopenLatestAsync(int id, PeriodReopenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException("A reason is required to reopen a closed accounting period.");

        var period = await _periodRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Accounting period close #{id} not found.");

        if (!string.Equals(period.Status, StatusClosed, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException("This accounting period is already reopened.");

        var latestClosed = await _periodRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == StatusClosed)
            .OrderByDescending(x => x.ClosingDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestClosed == null || latestClosed.Id != period.Id)
        {
            throw new BadRequestException("Only the most recent closed accounting period can be reopened (LIFO order). Reopen newer periods first.");
        }

        var closingEntries = await _glRepository.GetAll()
            .Where(x => x.VType == ClosingVType && x.VoucherNo == period.ClosingVoucherNo)
            .ToListAsync(cancellationToken);

        using (PeriodClosingScope.Enter())
        {
            if (closingEntries.Count > 0)
            {
                await _glRepository.DeleteRangeAsync(closingEntries, doSaveChanges: false);
            }

            period.Status = StatusReopened;
            period.ReopenedBy = string.IsNullOrWhiteSpace(_currentUser.Username)
                ? _currentUser.GetUserId()
                : _currentUser.Username;
            period.ReopenedOn = PakistanTime.Now;
            period.ReopenReason = request.Reason.Trim();

            await _periodRepository.UpdateAsync(period, doSaveChanges: false);
            await _periodRepository.SaveChangesAsync(cancellationToken);
        }

        PeriodLockCache.Invalidate(_currentTenant.Id ?? "__default__");

        var titleMap = await _coaRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Id == period.TargetEquityAccountId)
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);

        return MapResponse(period, titleMap, latestClosedId: null);
    }

    private async Task<PeriodClosePreviewResponse> BuildPreviewInternalAsync(
        DateOnly closingDate,
        string? requestedTargetEquityId,
        bool closeDrawings,
        string? periodNameForRemarks,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var today = DateOnly.FromDateTime(PakistanTime.Now);
        if (closingDate == default)
            errors.Add("Closing date is required.");
        else if (closingDate > today)
            errors.Add($"Closing date ({closingDate:dd-MMM-yyyy}) cannot be in the future.");

        var lastClosed = await _periodRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == StatusClosed)
            .OrderByDescending(x => x.ClosingDate)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastClosed != null && closingDate <= lastClosed.ClosingDate)
        {
            errors.Add($"Closing date must be after the last closed period date ({lastClosed.ClosingDate:dd-MMM-yyyy}).");
        }

        // Determine period start date
        DateOnly periodStartDate;
        if (lastClosed != null)
        {
            periodStartDate = lastClosed.ClosingDate.AddDays(1);
        }
        else
        {
            var earliestGlDate = await _glRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.VType != "Op" && x.VDate <= closingDate)
                .MinAsync(x => (DateOnly?)x.VDate, cancellationToken);

            periodStartDate = earliestGlDate ?? new DateOnly(closingDate.Year, 1, 1);
        }

        var (targetEquityId, targetEquityTitle) = await ResolveTargetEquityAccountAsync(requestedTargetEquityId, cancellationToken);
        if (string.IsNullOrWhiteSpace(targetEquityId))
        {
            errors.Add("Target equity account could not be resolved.");
        }

        // Load full Chart of Accounts
        var allAccounts = await _coaRepository.GetAll()
            .AsNoTracking()
            .Select(x => new { x.Id, x.Title, x.ParentId, x.AccLevel })
            .ToListAsync(cancellationToken);

        var accountMap = allAccounts.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

        string? GetRootId(string? id)
        {
            var current = id;
            int guard = 0;
            while (!string.IsNullOrWhiteSpace(current) && accountMap.TryGetValue(current, out var acc) && guard++ < 10)
            {
                if (string.IsNullOrWhiteSpace(acc.ParentId))
                    return acc.Id;
                current = acc.ParentId;
            }
            return current;
        }

        string GetTitle(string id) => accountMap.TryGetValue(id, out var acc) ? acc.Title : id;

        if (!string.IsNullOrWhiteSpace(requestedTargetEquityId))
        {
            if (!accountMap.TryGetValue(requestedTargetEquityId, out var reqAcc) || reqAcc.AccLevel != ChartOfAccountConstants.Levels.Detail)
            {
                errors.Add("Selected target equity account must be a valid level-5 detail account.");
            }
            else if (GetRootId(reqAcc.Id) != ChartOfAccountConstants.RootIds.Capital)
            {
                errors.Add("Selected target equity account must belong to CAPITAL (005).");
            }
        }

        // Resolve Inventory & Income Summary & Owner's Equity accounts
        var inventoryAccountId = await ResolveDefaultOrWellKnownAccountIdAsync(
            ChartOfAccountConstants.DefaultAccountTitles.Inventory,
            ChartOfAccountConstants.WellKnownAccountIds.Inventory,
            cancellationToken);

        var incomeSummaryAccountId = await ResolveDefaultOrWellKnownAccountIdAsync(
            ChartOfAccountConstants.DefaultAccountTitles.IncomeSummary,
            ChartOfAccountConstants.WellKnownAccountIds.IncomeSummary,
            cancellationToken);

        var ownerEquityAccountId = await ResolveDefaultOrWellKnownAccountIdAsync(
            ChartOfAccountConstants.DefaultAccountTitles.Capital,
            ChartOfAccountConstants.WellKnownAccountIds.OwnerEquity,
            cancellationToken);

        // Fetch all GL entries up to closingDate (including Op)
        var glRows = await _glRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.VType == "Op" || x.VDate <= closingDate)
            .Select(x => new { x.DrAccountId, x.CrAccountId, x.Amount, x.VType })
            .ToListAsync(cancellationToken);

        // Build cumulative balance map (Dr positive, Cr negative) and check GL balance
        var balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        decimal totalDrAll = 0m;
        decimal totalCrAll = 0m;

        foreach (var row in glRows)
        {
            if (!string.IsNullOrWhiteSpace(row.DrAccountId))
            {
                balances[row.DrAccountId] = balances.GetValueOrDefault(row.DrAccountId) + row.Amount;
                totalDrAll += row.Amount;
            }
            if (!string.IsNullOrWhiteSpace(row.CrAccountId))
            {
                balances[row.CrAccountId] = balances.GetValueOrDefault(row.CrAccountId) - row.Amount;
                totalCrAll += row.Amount;
            }
        }

        var glImbalance = Math.Round(totalDrAll - totalCrAll, 2);
        if (Math.Abs(glImbalance) >= 0.01m)
        {
            warnings.Add($"Trial Balance has an existing Opening Balance / GL imbalance of {Math.Abs(glImbalance):N2} ({(glImbalance > 0 ? "Dr > Cr" : "Cr > Dr")}). Review Opening Balances if you expect an exact zero difference.");
        }

        // Pre-close operational warnings (non-blocking)
        var draftPayrollCount = await _payrollRunRepository.GetAll()
            .AsNoTracking()
            .CountAsync(x => x.PayDate <= closingDate && x.Status != PayrollRunStatus.Posted && x.Status != PayrollRunStatus.Reversed, cancellationToken);
        if (draftPayrollCount > 0)
        {
            warnings.Add($"{draftPayrollCount} payroll run(s) on or before {closingDate:dd-MMM-yyyy} are not yet posted.");
        }

        var openKotCount = await _kotOrderRepository.GetAll()
            .AsNoTracking()
            .CountAsync(x => x.OrderDate <= closingDate && x.Status != "Completed" && x.Status != "Cancelled" && x.Status != "Served", cancellationToken);
        if (openKotCount > 0)
        {
            warnings.Add($"{openKotCount} KOT order(s) on or before {closingDate:dd-MMM-yyyy} are still open.");
        }

        // Compute FIFO stock valuations (flushes dirty items first)
        var openingStockValue = await GetFifoStockValuationAsync(periodStartDate.AddDays(-1), cancellationToken);
        var closingStockValue = await GetFifoStockValuationAsync(closingDate, cancellationToken);
        var currentGlInventory = balances.GetValueOrDefault(inventoryAccountId);
        var inventoryAdjustment = Math.Round(closingStockValue - currentGlInventory, 2);

        // Identify Purchase account for COGS reporting breakdown
        var detailAccounts = allAccounts.Where(x => x.AccLevel == ChartOfAccountConstants.Levels.Detail).OrderBy(x => x.Id).ToList();
        var purchaseAccountId = await ResolveDefaultOrWellKnownAccountIdAsync(
            ChartOfAccountConstants.DefaultAccountTitles.Purchases,
            "004001001001001",
            cancellationToken);

        var periodLabel = string.IsNullOrWhiteSpace(periodNameForRemarks)
            ? $"Period Closing ({periodStartDate:dd-MMM-yyyy} to {closingDate:dd-MMM-yyyy})"
            : $"Period Closing - {periodNameForRemarks}";

        var proposedEntries = new List<ClosingVoucherLineDto>();
        int seq = 1;

        decimal totalIncome = 0m;
        decimal totalPurchases = 0m;
        decimal totalOperatingExpenses = 0m;
        decimal incomeSummaryNetCr = 0m; // positive = credit (profit), negative = debit (loss)

        var incomeSummaryTitle = accountMap.TryGetValue(incomeSummaryAccountId, out var isAcc) ? isAcc.Title : "Income Summary";
        var inventoryTitle = accountMap.TryGetValue(inventoryAccountId, out var invAcc) ? invAcc.Title : "INVENTORY";

        // 1. Close all Income (003*) detail accounts to Income Summary
        foreach (var acc in detailAccounts.Where(x => GetRootId(x.Id) == ChartOfAccountConstants.RootIds.Income))
        {
            var bal = Math.Round(balances.GetValueOrDefault(acc.Id), 2);
            if (bal == 0m) continue;

            // Normal income has Cr balance (negative in Dr-Cr convention)
            totalIncome += -bal;
            incomeSummaryNetCr += -bal;

            if (bal < 0m)
            {
                proposedEntries.Add(new ClosingVoucherLineDto
                {
                    Seq = seq++,
                    DrAccountId = acc.Id,
                    DrAccountTitle = acc.Title,
                    CrAccountId = incomeSummaryAccountId,
                    CrAccountTitle = incomeSummaryTitle,
                    Amount = Math.Abs(bal),
                    Remarks = $"{periodLabel} - Close income account"
                });
            }
            else
            {
                proposedEntries.Add(new ClosingVoucherLineDto
                {
                    Seq = seq++,
                    DrAccountId = incomeSummaryAccountId,
                    DrAccountTitle = incomeSummaryTitle,
                    CrAccountId = acc.Id,
                    CrAccountTitle = acc.Title,
                    Amount = bal,
                    Remarks = $"{periodLabel} - Close contra-income account"
                });
            }
        }

        // 2. Close all Expense (004*) detail accounts (including Purchase) to Income Summary
        foreach (var acc in detailAccounts.Where(x => GetRootId(x.Id) == ChartOfAccountConstants.RootIds.Expenses))
        {
            var bal = Math.Round(balances.GetValueOrDefault(acc.Id), 2);
            if (bal == 0m) continue;

            if (string.Equals(acc.Id, purchaseAccountId, StringComparison.OrdinalIgnoreCase))
                totalPurchases += bal;
            else
                totalOperatingExpenses += bal;

            incomeSummaryNetCr -= bal;

            if (bal > 0m)
            {
                proposedEntries.Add(new ClosingVoucherLineDto
                {
                    Seq = seq++,
                    DrAccountId = incomeSummaryAccountId,
                    DrAccountTitle = incomeSummaryTitle,
                    CrAccountId = acc.Id,
                    CrAccountTitle = acc.Title,
                    Amount = bal,
                    Remarks = $"{periodLabel} - Close expense account"
                });
            }
            else
            {
                proposedEntries.Add(new ClosingVoucherLineDto
                {
                    Seq = seq++,
                    DrAccountId = acc.Id,
                    DrAccountTitle = acc.Title,
                    CrAccountId = incomeSummaryAccountId,
                    CrAccountTitle = incomeSummaryTitle,
                    Amount = Math.Abs(bal),
                    Remarks = $"{periodLabel} - Close contra-expense account"
                });
            }
        }

        // 3. Periodic Inventory Adjustment: bring GL INVENTORY account to FIFO Closing Stock Value
        if (inventoryAdjustment != 0m)
        {
            incomeSummaryNetCr += inventoryAdjustment;
            if (inventoryAdjustment > 0m)
            {
                proposedEntries.Add(new ClosingVoucherLineDto
                {
                    Seq = seq++,
                    DrAccountId = inventoryAccountId,
                    DrAccountTitle = inventoryTitle,
                    CrAccountId = incomeSummaryAccountId,
                    CrAccountTitle = incomeSummaryTitle,
                    Amount = inventoryAdjustment,
                    Remarks = $"{periodLabel} - Inventory valuation adjustment (Closing Stock: {closingStockValue:N2})"
                });
            }
            else
            {
                proposedEntries.Add(new ClosingVoucherLineDto
                {
                    Seq = seq++,
                    DrAccountId = incomeSummaryAccountId,
                    DrAccountTitle = incomeSummaryTitle,
                    CrAccountId = inventoryAccountId,
                    CrAccountTitle = inventoryTitle,
                    Amount = Math.Abs(inventoryAdjustment),
                    Remarks = $"{periodLabel} - Inventory valuation adjustment (Closing Stock: {closingStockValue:N2})"
                });
            }
        }

        // 4. Transfer Net Profit / (Loss) from Income Summary -> Target Equity Account
        var netProfitLoss = Math.Round(incomeSummaryNetCr, 2);
        if (netProfitLoss > 0m)
        {
            proposedEntries.Add(new ClosingVoucherLineDto
            {
                Seq = seq++,
                DrAccountId = incomeSummaryAccountId,
                DrAccountTitle = incomeSummaryTitle,
                CrAccountId = targetEquityId,
                CrAccountTitle = targetEquityTitle,
                Amount = netProfitLoss,
                Remarks = $"{periodLabel} - Transfer Net Profit to {targetEquityTitle}"
            });
        }
        else if (netProfitLoss < 0m)
        {
            proposedEntries.Add(new ClosingVoucherLineDto
            {
                Seq = seq++,
                DrAccountId = targetEquityId,
                DrAccountTitle = targetEquityTitle,
                CrAccountId = incomeSummaryAccountId,
                CrAccountTitle = incomeSummaryTitle,
                Amount = Math.Abs(netProfitLoss),
                Remarks = $"{periodLabel} - Transfer Net Loss to {targetEquityTitle}"
            });
        }

        // 5. Optional: Close Drawing (005002*) accounts into Owner's Equity
        decimal totalDrawingsClosed = 0m;
        if (closeDrawings)
        {
            var ownerEquityTitle = GetTitle(ownerEquityAccountId);
            foreach (var acc in detailAccounts.Where(x => x.Id.StartsWith(ChartOfAccountConstants.Prefixes.Drawings, StringComparison.OrdinalIgnoreCase)))
            {
                var bal = Math.Round(balances.GetValueOrDefault(acc.Id), 2);
                if (bal == 0m) continue;

                totalDrawingsClosed += bal;
                if (bal > 0m)
                {
                    proposedEntries.Add(new ClosingVoucherLineDto
                    {
                        Seq = seq++,
                        DrAccountId = ownerEquityAccountId,
                        DrAccountTitle = ownerEquityTitle,
                        CrAccountId = acc.Id,
                        CrAccountTitle = acc.Title,
                        Amount = bal,
                        Remarks = $"{periodLabel} - Close drawings to {ownerEquityTitle}"
                    });
                }
                else
                {
                    proposedEntries.Add(new ClosingVoucherLineDto
                    {
                        Seq = seq++,
                        DrAccountId = acc.Id,
                        DrAccountTitle = acc.Title,
                        CrAccountId = ownerEquityAccountId,
                        CrAccountTitle = ownerEquityTitle,
                        Amount = Math.Abs(bal),
                        Remarks = $"{periodLabel} - Close drawings to {ownerEquityTitle}"
                    });
                }
            }
        }

        // Simulate post-closing balances to verify Assets == Liabilities + Equity
        var postBalances = new Dictionary<string, decimal>(balances, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in proposedEntries)
        {
            postBalances[entry.DrAccountId] = postBalances.GetValueOrDefault(entry.DrAccountId) + entry.Amount;
            postBalances[entry.CrAccountId] = postBalances.GetValueOrDefault(entry.CrAccountId) - entry.Amount;
        }

        decimal postAssets = 0m;
        decimal postLiabilities = 0m;
        decimal postEquity = 0m;

        foreach (var kvp in postBalances)
        {
            if (kvp.Value == 0m) continue;
            var root = GetRootId(kvp.Key);
            if (root == ChartOfAccountConstants.RootIds.Assets)
                postAssets += kvp.Value;
            else if (root == ChartOfAccountConstants.RootIds.Liabilities)
                postLiabilities += -kvp.Value;
            else if (root == ChartOfAccountConstants.RootIds.Capital)
                postEquity += -kvp.Value;
        }

        postAssets = Math.Round(postAssets, 2);
        postLiabilities = Math.Round(postLiabilities, 2);
        postEquity = Math.Round(postEquity, 2);
        var bsDiff = Math.Round(postAssets - (postLiabilities + postEquity), 2);

        var totalCogs = Math.Round(openingStockValue + totalPurchases - closingStockValue, 2);
        var grossProfit = Math.Round(totalIncome - totalCogs, 2);

        return new PeriodClosePreviewResponse
        {
            PeriodStartDate = periodStartDate,
            ClosingDate = closingDate,
            TargetEquityAccountId = targetEquityId,
            TargetEquityAccountTitle = targetEquityTitle,
            CloseDrawings = closeDrawings,
            OpeningStockValue = Math.Round(openingStockValue, 2),
            ClosingStockValue = Math.Round(closingStockValue, 2),
            CurrentGlInventoryBalance = Math.Round(currentGlInventory, 2),
            InventoryAdjustment = inventoryAdjustment,
            TotalIncome = Math.Round(totalIncome, 2),
            TotalPurchases = Math.Round(totalPurchases, 2),
            TotalCogs = totalCogs,
            GrossProfit = grossProfit,
            TotalExpenses = Math.Round(totalOperatingExpenses, 2),
            NetProfitLoss = netProfitLoss,
            TotalDrawingsClosed = Math.Round(totalDrawingsClosed, 2),
            PostCloseTotalAssets = postAssets,
            PostCloseTotalLiabilities = postLiabilities,
            PostCloseTotalEquity = postEquity,
            BalanceSheetDifference = bsDiff,
            IsBalanced = Math.Abs(bsDiff) < 0.01m,
            CanClose = errors.Count == 0,
            ValidationErrors = errors,
            ValidationWarnings = warnings,
            ProposedEntries = proposedEntries
        };
    }

    private async Task<(string AccountId, string Title)> ResolveTargetEquityAccountAsync(string? requestedAccountId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedAccountId))
        {
            var req = await _coaRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == requestedAccountId, cancellationToken);
            if (req != null)
                return (req.Id, req.Title);
        }

        // Prefer Retained Earnings, fallback to Owner's Equity
        var retained = await _coaRepository.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ChartOfAccountConstants.WellKnownAccountIds.RetainedEarnings, cancellationToken);
        if (retained != null)
            return (retained.Id, retained.Title);

        var defaultCapitalId = await ResolveDefaultOrWellKnownAccountIdAsync(
            ChartOfAccountConstants.DefaultAccountTitles.Capital,
            ChartOfAccountConstants.WellKnownAccountIds.OwnerEquity,
            cancellationToken);

        var capitalAcc = await _coaRepository.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == defaultCapitalId, cancellationToken);

        return (capitalAcc?.Id ?? ChartOfAccountConstants.WellKnownAccountIds.RetainedEarnings, capitalAcc?.Title ?? "Retained Earnings");
    }

    private async Task<string> ResolveDefaultOrWellKnownAccountIdAsync(string defaultTitle, string fallbackId, CancellationToken cancellationToken)
    {
        var id = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == defaultTitle)
            .Select(x => x.AccountId)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(id) ? fallbackId : id;
    }

    private async Task EnsureClosingCoreAccountsExistAsync(CancellationToken cancellationToken)
    {
        var requiredAccounts = new[]
        {
            new ChartOfAccount
            {
                Id = ChartOfAccountConstants.WellKnownAccountIds.RetainedEarnings,
                Title = "Retained Earnings",
                ParentId = "005001001001",
                AccType = ChartOfAccountConstants.AccountTypes.Detail,
                AccLevel = ChartOfAccountConstants.Levels.Detail
            },
            new ChartOfAccount
            {
                Id = ChartOfAccountConstants.WellKnownAccountIds.IncomeSummary,
                Title = "Income Summary",
                ParentId = "005001001001",
                AccType = ChartOfAccountConstants.AccountTypes.Detail,
                AccLevel = ChartOfAccountConstants.Levels.Detail
            }
        };

        var ids = requiredAccounts.Select(x => x.Id).ToList();
        var existing = await _coaRepository.GetAll()
            .IgnoreQueryFilters()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var existingMap = existing.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        bool changed = false;

        foreach (var req in requiredAccounts)
        {
            if (existingMap.TryGetValue(req.Id, out var found))
            {
                if (found.DeletedOn != null)
                {
                    found.DeletedOn = null;
                    found.DeletedBy = null;
                    await _coaRepository.UpdateAsync(found, doSaveChanges: false);
                    changed = true;
                }
            }
            else
            {
                await _coaRepository.AddAsync(req, doSaveChanges: false);
                changed = true;
            }
        }

        if (changed)
        {
            await _coaRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<decimal> GetFifoStockValuationAsync(DateOnly asOfDate, CancellationToken cancellationToken)
    {
        var dirtyValuationItemIds = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId)
                     && (x.VDate <= asOfDate || x.VType == "Op")
                     && x.Item != null
                     && x.Item.FifoRebuildFromDate != null)
            .Select(x => x.ItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (dirtyValuationItemIds.Count > 0)
        {
            await _fifoCostingService.FlushDirtyItemsAsync(dirtyValuationItemIds, cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (asOfDate >= today)
        {
            return Math.Round(await _itemTransactionRepository.GetAll()
                .AsNoTracking()
                .Where(x => string.Equals(x.TranType, "in", StringComparison.OrdinalIgnoreCase)
                         && !string.IsNullOrWhiteSpace(x.ItemId)
                         && (x.VDate <= asOfDate || x.VType == "Op")
                         && x.RemainingQty > 0)
                .SumAsync(x => x.RemainingQty * x.Rate, cancellationToken), 2);
        }

        var inBatches = await _itemTransactionRepository.GetAll()
            .AsNoTracking()
            .Where(x => string.Equals(x.TranType, "in", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(x.ItemId)
                     && (x.VDate <= asOfDate || x.VType == "Op"))
            .Select(x => new { x.Id, x.QtyIn, x.Rate })
            .ToListAsync(cancellationToken);

        if (inBatches.Count == 0)
            return 0m;

        var consumptions = await _fifoMappingRepository.GetAll()
            .AsNoTracking()
            .Where(m => m.OutTransaction.VDate <= asOfDate)
            .GroupBy(m => m.InTransactionId)
            .Select(g => new { InTxId = g.Key, ConsumedQty = g.Sum(x => x.QtyConsumed) })
            .ToDictionaryAsync(x => x.InTxId, x => x.ConsumedQty, cancellationToken);

        decimal totalValue = 0m;
        foreach (var batch in inBatches)
        {
            var consumed = consumptions.GetValueOrDefault(batch.Id, 0m);
            var remaining = Math.Max(0m, batch.QtyIn - consumed);
            if (remaining > 0)
            {
                totalValue += remaining * batch.Rate;
            }
        }

        return Math.Round(totalValue, 2);
    }

    private static AccountingPeriodCloseResponse MapResponse(
        AccountingPeriodClose p,
        Dictionary<string, string> titleMap,
        int? latestClosedId) => new()
        {
            Id = p.Id,
            PeriodName = p.PeriodName,
            PeriodStartDate = p.PeriodStartDate,
            ClosingDate = p.ClosingDate,
            Status = p.Status,
            ClosingVoucherNo = p.ClosingVoucherNo,
            TargetEquityAccountId = p.TargetEquityAccountId,
            TargetEquityAccountTitle = titleMap.TryGetValue(p.TargetEquityAccountId, out var t) ? t : p.TargetEquityAccountId,
            ClosedDrawings = p.ClosedDrawings,
            OpeningStockValue = p.OpeningStockValue,
            ClosingStockValue = p.ClosingStockValue,
            TotalIncome = p.TotalIncome,
            TotalCogs = p.TotalCogs,
            TotalExpenses = p.TotalExpenses,
            NetProfitLoss = p.NetProfitLoss,
            TotalAssets = p.TotalAssets,
            TotalLiabilities = p.TotalLiabilities,
            TotalEquity = p.TotalEquity,
            IsBalanced = p.IsBalanced,
            CanReopen = string.Equals(p.Status, StatusClosed, StringComparison.OrdinalIgnoreCase) && latestClosedId == p.Id,
            Notes = p.Notes,
            ClosedBy = p.CreatedBy,
            ClosedOn = p.CreatedOn,
            ReopenedBy = p.ReopenedBy,
            ReopenedOn = p.ReopenedOn,
            ReopenReason = p.ReopenReason
        };
}
