using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.BankAccounts;
using Retailer.Application.Legacy.OpeningBalances;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.BankAccounts;

internal class BankAccountService : IBankAccountService
{
    private const string DefaultBankPrefix = "001002002002";
    private const string DefaultCashPrefix = "001002002001";
    private const string CashBanksParentId = "001002002";
    private const string OpeningVType = "Op";

    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;
    private readonly IRepository<BankDetail> _bankDetailRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IOpeningBalanceService _openingBalanceService;
    private readonly ILogger<BankAccountService> _logger;

    public BankAccountService(
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<BankDetail> bankDetailRepository,
        IRepository<GlEntry> glRepository,
        IOpeningBalanceService openingBalanceService,
        ILogger<BankAccountService> logger)
    {
        _chartOfAccountRepository = chartOfAccountRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _bankDetailRepository = bankDetailRepository;
        _glRepository = glRepository;
        _openingBalanceService = openingBalanceService;
        _logger = logger;
    }

    public async Task<List<BankAccountResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var bankPrefix = await GetBankParentPrefixAsync(cancellationToken);
        var cashPrefix = await GetCashParentPrefixAsync(cancellationToken);

        var accounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && (x.ParentId == bankPrefix || x.Id.StartsWith(bankPrefix) || x.ParentId == cashPrefix || x.Id.StartsWith(cashPrefix)))
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.ParentId,
                x.CreatedBy,
                x.CreatedOn,
                x.LastModifiedBy,
                x.LastModifiedOn
            })
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return [];

        var accountIds = accounts.Select(x => x.Id).ToList();

        // Fetch bank details (safely, in case table is not yet migrated)
        Dictionary<string, BankDetail> detailMap = [];
        try
        {
            detailMap = await _bankDetailRepository.GetAll()
                .AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not query BankDetail table; falling back to ChartOfAccount only.");
        }

        // Fetch opening balances (fast indexed lookup on VType == 'Op')
        Dictionary<string, decimal> opMap = [];
        try
        {
            var opEntries = await _glRepository.GetAll()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.VType == OpeningVType &&
                            (accountIds.Contains(x.DrAccountId!) || accountIds.Contains(x.CrAccountId!)))
                .Select(x => new { x.DrAccountId, x.CrAccountId, x.Amount })
                .ToListAsync(cancellationToken);

            opMap = opEntries.ToDictionary(
                x => (x.DrAccountId ?? x.CrAccountId)!,
                x => x.DrAccountId != null ? x.Amount : -x.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not query opening balance GL entries.");
        }

        // Fetch current balances (net Dr - Cr from all GL entries)
        Dictionary<string, decimal> curMap = [];
        try
        {
            var drEntries = await _glRepository.GetAll()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.DrAccountId != null && accountIds.Contains(x.DrAccountId))
                .GroupBy(x => x.DrAccountId!)
                .Select(g => new { AccountId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.AccountId, x => x.Total, cancellationToken);

            var crEntries = await _glRepository.GetAll()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.CrAccountId != null && accountIds.Contains(x.CrAccountId))
                .GroupBy(x => x.CrAccountId!)
                .Select(g => new { AccountId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.AccountId, x => x.Total, cancellationToken);

            foreach (var accId in accountIds)
            {
                drEntries.TryGetValue(accId, out var dr);
                crEntries.TryGetValue(accId, out var cr);
                curMap[accId] = dr - cr;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not query current balance GL entries.");
        }

        return accounts.Select(x =>
        {
            detailMap.TryGetValue(x.Id, out var detail);
            opMap.TryGetValue(x.Id, out var opBal);
            curMap.TryGetValue(x.Id, out var curBal);
            var isCash = x.ParentId == cashPrefix || x.Id.StartsWith(cashPrefix);

            return new BankAccountResponse
            {
                Account = x.Id,
                AccountType = isCash ? "Cash" : "Bank",
                BankName = isCash ? "Cash" : (detail?.BankName ?? "Bank"),
                AccountTitle = detail?.AccountTitle ?? x.Title,
                AccountNumber = detail?.AccountNumber,
                Iban = detail?.Iban,
                BranchName = detail?.BranchName,
                BranchCode = detail?.BranchCode,
                SwiftCode = detail?.SwiftCode,
                Currency = detail?.Currency ?? "PKR",
                Notes = detail?.Notes,
                Active = detail?.Active ?? true,
                OpeningBalance = opBal,
                CurrentBalance = curBal,
                CreatedBy = detail?.CreatedBy ?? x.CreatedBy,
                CreatedOn = detail?.CreatedOn ?? x.CreatedOn,
                LastModifiedBy = detail?.LastModifiedBy ?? x.LastModifiedBy,
                LastModifiedOn = detail?.LastModifiedOn ?? x.LastModifiedOn
            };
        }).ToList();
    }

    public async Task<BankAccountResponse?> GetByIdAsync(string account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            return null;

        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(x => x.Account == account);
    }

    public async Task<string> CreateAsync(BankAccountCreateRequest request, CancellationToken cancellationToken)
    {
        var isCash = string.Equals(request.AccountType, "Cash", StringComparison.OrdinalIgnoreCase);

        if (!isCash && string.IsNullOrWhiteSpace(request.BankName))
            throw new BadRequestException("Bank name is required for bank accounts.");

        if (string.IsNullOrWhiteSpace(request.AccountTitle))
            throw new BadRequestException("Account title is required.");

        var parentPrefix = isCash
            ? await GetCashParentPrefixAsync(cancellationToken)
            : await GetBankParentPrefixAsync(cancellationToken);

        var parent = await _chartOfAccountRepository.GetByIdAsync(parentPrefix, cancellationToken);
        if (parent is null)
        {
            // Auto-create parent if 001002002 exists
            var cashBanks = await _chartOfAccountRepository.GetByIdAsync(CashBanksParentId, cancellationToken);
            if (cashBanks != null)
            {
                parent = new ChartOfAccount
                {
                    Id = isCash ? DefaultCashPrefix : DefaultBankPrefix,
                    Title = isCash ? "Cash in Hand" : "Banks",
                    ParentId = CashBanksParentId,
                    AccLevel = 4,
                    AccType = "Parent"
                };
                await _chartOfAccountRepository.AddAsync(parent);
                parentPrefix = parent.Id;
            }
            else
            {
                throw new NotFoundException($"Parent account '{parentPrefix}' not found in Chart of Accounts.");
            }
        }

        // Generate next level 5 account suffix
        var maxChildId = await _chartOfAccountRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.ParentId == parentPrefix)
            .Select(x => x.Id)
            .OrderByDescending(x => x.Length)
            .ThenByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var maxSuffix = 0L;
        if (!string.IsNullOrWhiteSpace(maxChildId) && maxChildId.Length > parentPrefix.Length)
        {
            long.TryParse(maxChildId[parentPrefix.Length..], out maxSuffix);
        }

        var newAccountCode = parentPrefix + (maxSuffix + 1).ToString("D3");
        var accLevel = parent.AccLevel + 1;

        // Build composite title for COA display
        var coaTitle = isCash
            ? request.AccountTitle.Trim()
            : BuildCoaTitle(request.BankName ?? "Bank", request.AccountTitle);

        var newAccount = new ChartOfAccount
        {
            Id = newAccountCode,
            Title = coaTitle,
            ParentId = parentPrefix,
            AccLevel = accLevel,
            AccType = "Detail"
        };
        await _chartOfAccountRepository.AddAsync(newAccount);

        // Save bank detail entity
        try
        {
            var detail = new BankDetail
            {
                Id = newAccountCode,
                BankName = isCash ? "Cash" : request.BankName?.Trim(),
                AccountTitle = request.AccountTitle.Trim(),
                AccountNumber = isCash ? null : request.AccountNumber?.Trim(),
                Iban = isCash ? null : request.Iban?.Trim(),
                BranchName = isCash ? null : request.BranchName?.Trim(),
                BranchCode = isCash ? null : request.BranchCode?.Trim(),
                SwiftCode = isCash ? null : request.SwiftCode?.Trim(),
                Currency = request.Currency?.Trim() ?? "PKR",
                Notes = request.Notes?.Trim(),
                Active = request.Active
            };
            await _bankDetailRepository.AddAsync(detail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist BankDetail; ChartOfAccount was created.");
        }

        // Record opening balance if provided
        if (request.OpeningBalance.HasValue && request.OpeningBalance.Value != 0)
        {
            try
            {
                var dr = request.OpeningBalance.Value > 0 ? request.OpeningBalance.Value : (decimal?)null;
                var cr = request.OpeningBalance.Value < 0 ? -request.OpeningBalance.Value : (decimal?)null;
                await _openingBalanceService.UpsertAsync(new OpeningBalanceUpsertRequest
                {
                    Account = newAccountCode,
                    DrAmount = dr,
                    CrAmount = cr
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set opening balance for account {Account}.", newAccountCode);
            }
        }

        return newAccountCode;
    }

    public async Task UpdateAsync(string account, BankAccountUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new BadRequestException("Account code is required.");

        var chartOfAccount = await _chartOfAccountRepository.GetByIdAsync(account, cancellationToken);
        if (chartOfAccount is null || chartOfAccount.AccLevel != 5)
            throw new NotFoundException($"Account '{account}' not found.");

        var cashPrefix = await GetCashParentPrefixAsync(cancellationToken);
        var isCash = chartOfAccount.ParentId == cashPrefix || chartOfAccount.Id.StartsWith(cashPrefix);

        var bankName = isCash ? "Cash" : request.BankName?.Trim();
        var accountTitle = request.AccountTitle?.Trim();

        if (!string.IsNullOrWhiteSpace(bankName) || !string.IsNullOrWhiteSpace(accountTitle))
        {
            var newTitle = isCash
                ? (accountTitle ?? chartOfAccount.Title)
                : BuildCoaTitle(bankName ?? chartOfAccount.Title, accountTitle ?? chartOfAccount.Title);

            if (chartOfAccount.Title != newTitle)
            {
                chartOfAccount.Title = newTitle;
                await _chartOfAccountRepository.UpdateAsync(chartOfAccount);
            }
        }

        try
        {
            var detail = await _bankDetailRepository.GetByIdAsync(account, cancellationToken);
            if (detail is null)
            {
                detail = new BankDetail
                {
                    Id = account,
                    BankName = bankName,
                    AccountTitle = accountTitle ?? chartOfAccount.Title,
                    AccountNumber = isCash ? null : request.AccountNumber?.Trim(),
                    Iban = isCash ? null : request.Iban?.Trim(),
                    BranchName = isCash ? null : request.BranchName?.Trim(),
                    BranchCode = isCash ? null : request.BranchCode?.Trim(),
                    SwiftCode = isCash ? null : request.SwiftCode?.Trim(),
                    Currency = request.Currency?.Trim() ?? "PKR",
                    Notes = request.Notes?.Trim(),
                    Active = request.Active ?? true
                };
                await _bankDetailRepository.AddAsync(detail);
            }
            else
            {
                if (bankName != null) detail.BankName = bankName;
                if (accountTitle != null) detail.AccountTitle = accountTitle;
                if (request.AccountNumber != null) detail.AccountNumber = isCash ? null : request.AccountNumber.Trim();
                if (request.Iban != null) detail.Iban = isCash ? null : request.Iban.Trim();
                if (request.BranchName != null) detail.BranchName = isCash ? null : request.BranchName.Trim();
                if (request.BranchCode != null) detail.BranchCode = isCash ? null : request.BranchCode.Trim();
                if (request.SwiftCode != null) detail.SwiftCode = isCash ? null : request.SwiftCode.Trim();
                if (request.Currency != null) detail.Currency = request.Currency.Trim();
                if (request.Notes != null) detail.Notes = request.Notes.Trim();
                if (request.Active.HasValue) detail.Active = request.Active.Value;

                await _bankDetailRepository.UpdateAsync(detail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update BankDetail for account {Account}.", account);
        }

        // Update opening balance if specified
        if (request.OpeningBalance.HasValue)
        {
            try
            {
                var dr = request.OpeningBalance.Value > 0 ? request.OpeningBalance.Value : (decimal?)null;
                var cr = request.OpeningBalance.Value < 0 ? -request.OpeningBalance.Value : (decimal?)null;
                await _openingBalanceService.UpsertAsync(new OpeningBalanceUpsertRequest
                {
                    Account = account,
                    DrAmount = dr,
                    CrAmount = cr
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update opening balance for account {Account}.", account);
            }
        }
    }

    public async Task DeleteAsync(string account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new BadRequestException("Account code is required.");

        var chartOfAccount = await _chartOfAccountRepository.GetByIdAsync(account, cancellationToken);
        if (chartOfAccount is null)
            throw new NotFoundException($"Account '{account}' not found.");

        // Check if there are any non-opening GL entries
        var hasTransactions = await _glRepository.GetAll()
            .AsNoTracking()
            .AnyAsync(x => (x.DrAccountId == account || x.CrAccountId == account) && x.VType != OpeningVType, cancellationToken);

        if (hasTransactions)
        {
            throw new BadRequestException($"Cannot delete account '{chartOfAccount.Title}' because it has existing transaction entries in the general ledger. You can deactivate it instead.");
        }

        // Delete opening balance entry if exists
        var opEntries = await _glRepository.GetAll()
            .Where(x => (x.DrAccountId == account || x.CrAccountId == account) && x.VType == OpeningVType)
            .ToListAsync(cancellationToken);

        if (opEntries.Count > 0)
        {
            await _glRepository.DeleteRangeAsync(opEntries);
        }

        // Delete bank detail if exists
        try
        {
            var detail = await _bankDetailRepository.GetByIdAsync(account, cancellationToken);
            if (detail != null)
            {
                await _bankDetailRepository.DeleteAsync(detail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete BankDetail for account {Account}.", account);
        }

        await _chartOfAccountRepository.DeleteAsync(chartOfAccount);
    }

    private async Task<string> GetCashParentPrefixAsync(CancellationToken cancellationToken)
    {
        // 1. Try DefaultAccount with title "Cash"
        var defaultAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == "Cash")
            .Select(x => new { x.AccountId, x.MapAccountId })
            .FirstOrDefaultAsync(cancellationToken);

        // MapAccountId explicitly maps the Level 4 Cash parent prefix (e.g. 001002002001)
        if (!string.IsNullOrWhiteSpace(defaultAccount?.MapAccountId))
            return defaultAccount.MapAccountId;

        // If MapAccountId is null, resolve parent from AccountId
        if (!string.IsNullOrWhiteSpace(defaultAccount?.AccountId))
        {
            var coa = await _chartOfAccountRepository.GetByIdAsync(defaultAccount.AccountId, cancellationToken);
            if (coa != null)
            {
                if (coa.AccLevel == 4)
                    return coa.Id;
                if (coa.AccLevel == 5 && !string.IsNullOrWhiteSpace(coa.ParentId))
                    return coa.ParentId;
            }
        }

        // 2. Fallback to standard Cash code: "001002002001"
        return DefaultCashPrefix;
    }

    private async Task<string> GetBankParentPrefixAsync(CancellationToken cancellationToken)
    {
        // 1. Try DefaultAccount with title "Bank" or "Banks"
        var defaultAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == "Bank" || x.Title == "Banks")
            .Select(x => new { x.AccountId, x.MapAccountId })
            .FirstOrDefaultAsync(cancellationToken);

        // MapAccountId explicitly maps the Level 4 Bank parent prefix (e.g. 001002002002)
        if (!string.IsNullOrWhiteSpace(defaultAccount?.MapAccountId))
            return defaultAccount.MapAccountId;

        // If MapAccountId is null, resolve parent from AccountId
        if (!string.IsNullOrWhiteSpace(defaultAccount?.AccountId))
        {
            var coa = await _chartOfAccountRepository.GetByIdAsync(defaultAccount.AccountId, cancellationToken);
            if (coa != null)
            {
                if (coa.AccLevel == 4)
                    return coa.Id;
                if (coa.AccLevel == 5 && !string.IsNullOrWhiteSpace(coa.ParentId))
                    return coa.ParentId;
            }
        }

        // 2. Fallback to standard Banks code: "001002002002"
        return DefaultBankPrefix;
    }

    private static string BuildCoaTitle(string bankName, string accountTitle)
    {
        var cleanBank = bankName.Trim();
        var cleanTitle = accountTitle.Trim();

        if (cleanTitle.StartsWith(cleanBank, StringComparison.OrdinalIgnoreCase))
            return cleanTitle;

        if (string.Equals(cleanBank, cleanTitle, StringComparison.OrdinalIgnoreCase))
            return cleanBank;

        return $"{cleanBank} - {cleanTitle}";
    }
}
