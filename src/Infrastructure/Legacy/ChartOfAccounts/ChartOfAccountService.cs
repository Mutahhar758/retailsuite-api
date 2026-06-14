using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.ChartOfAccounts;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Legacy.ChartOfAccounts;

internal class ChartOfAccountService : IChartOfAccountService
{
    private const string CashBankDefaultAccountTitle = "Cash";
    private const string SupplierDefaultAccountTitle = "Suppliers";
    private static readonly string[] CustomerAccountTitles = ["Customers", "Suppliers"];

    private readonly IRepository<ChartOfAccount> _repository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;

    public ChartOfAccountService(
        IRepository<ChartOfAccount> repository,
        IRepository<DefaultAccount> defaultAccountRepository)
    {
        _repository = repository;
        _defaultAccountRepository = defaultAccountRepository;
    }

    public async Task<List<ChartOfAccountResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ChartOfAccountResponse
            {
                Account = x.Id,
                Title = x.Title,
                ParentId = x.ParentId ?? string.Empty,
                AccType = x.AccType,
                AccLevel = x.AccLevel,
                CreatedBy = x.CreatedBy,
                CreatedOn = x.CreatedOn,
                LastModifiedBy = x.LastModifiedBy,
                LastModifiedOn = x.LastModifiedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChartOfAccountHeadResponse>> GetHeadsAsync(int level, CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == level)
            .OrderBy(x => x.Id)
            .Select(x => new ChartOfAccountHeadResponse
            {
                Account = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<string> CreateAsync(ChartOfAccountCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ParentId))
            throw new BadRequestException("Parent account is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Title is required.");

        var parent = await _repository.GetByIdAsync(request.ParentId, cancellationToken);
        if (parent is null)
            throw new NotFoundException($"Parent account '{request.ParentId}' not found.");

        if (parent.AccLevel >= 5)
            throw new BadRequestException("Cannot create child for detail account.");

        var maxChildId = await _repository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.ParentId == request.ParentId)
            .Select(x => x.Id)
            .OrderByDescending(x => x.Length)
            .ThenByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var maxSuffix = 0L;
        if (!string.IsNullOrWhiteSpace(maxChildId) && maxChildId.Length > request.ParentId.Length)
        {
            long.TryParse(maxChildId[request.ParentId.Length..], out maxSuffix);
        }

        var account = request.ParentId + (maxSuffix + 1).ToString("D3");
        var accLevel = parent.AccLevel + 1;

        var chartOfAccount = new ChartOfAccount
        {
            Id = account,
            Title = request.Title,
            ParentId = request.ParentId,
            AccLevel = accLevel,
            AccType = accLevel == 5 ? "Detail" : "Parent"
        };

        await _repository.AddAsync(chartOfAccount);

        return account;
    }

    public async Task UpdateAsync(string account, ChartOfAccountUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new BadRequestException("Account is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Title is required.");

        var chartOfAccount = await _repository.GetByIdAsync(account, cancellationToken);

        if (chartOfAccount is null)
            throw new NotFoundException($"Account '{account}' not found.");

        chartOfAccount.Title = request.Title;

        await _repository.UpdateAsync(chartOfAccount);
    }

    public async Task DeleteAsync(string account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new BadRequestException("Account is required.");

        var chartOfAccount = await _repository.GetByIdAsync(account, cancellationToken);
        if (chartOfAccount is null)
            return;

        var hasChildren = await _repository.GetAll()
            .AsNoTracking()
            .AnyAsync(x => x.ParentId == account, cancellationToken);

        if (hasChildren)
            throw new BadRequestException("Cannot delete account that has child accounts.");

        await _repository.DeleteAsync(chartOfAccount);
    }

    public async Task<List<ChartOfAccountHeadResponse>> GetByPrefixAsync(string prefix, int? level, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new BadRequestException("Prefix is required.");

        var query = _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.Id.StartsWith(prefix));

        if (level.HasValue)
            query = query.Where(x => x.AccLevel == level.Value);

        return await query
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .Select(x => new ChartOfAccountHeadResponse
            {
                Account = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChartOfAccountHeadResponse>> GetDetailAccountsAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccType == "Detail")
            .OrderBy(x => x.Id)
            .Select(x => new ChartOfAccountHeadResponse
            {
                Account = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChartOfAccountHeadResponse>> GetCashBankAccountsAsync(CancellationToken cancellationToken)
    {
        var defaultAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == CashBankDefaultAccountTitle)
            .Select(x => new { x.AccountId, x.MapAccountId })
            .FirstOrDefaultAsync(cancellationToken);

        var prefix = defaultAccount?.MapAccountId ?? defaultAccount?.AccountId;
        if (string.IsNullOrWhiteSpace(prefix))
            throw new NotFoundException($"Default account mapping for '{CashBankDefaultAccountTitle}' is not configured.");

        return await _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.Id.StartsWith(prefix))
            .OrderBy(x => x.Id)
            .Select(x => new ChartOfAccountHeadResponse
            {
                Account = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChartOfAccountHeadResponse>> GetSupplierAccountsAsync(CancellationToken cancellationToken)
    {
        var defaultAccount = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Title == SupplierDefaultAccountTitle)
            .Select(x => new { x.AccountId, x.MapAccountId })
            .FirstOrDefaultAsync(cancellationToken);

        var prefix = defaultAccount?.MapAccountId ?? defaultAccount?.AccountId;
        if (string.IsNullOrWhiteSpace(prefix))
            throw new NotFoundException($"Default account mapping for '{SupplierDefaultAccountTitle}' is not configured.");

        return await _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.Id.StartsWith(prefix))
            .OrderBy(x => x.Id)
            .Select(x => new ChartOfAccountHeadResponse
            {
                Account = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChartOfAccountHeadResponse>> GetCustomerAccountsAsync(CancellationToken cancellationToken)
    {
        var prefixes = await _defaultAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => CustomerAccountTitles.Contains(x.Title!))
            .Select(x => x.MapAccountId ?? x.AccountId)
            .Where(x => x != null && x != string.Empty)
            .ToListAsync(cancellationToken);

        if (prefixes.Count == 0)
            throw new NotFoundException("Default customer or supplier account mapping is not configured.");

        var distinctPrefixes = prefixes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();

        return (await _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5)
            .OrderBy(x => x.Id)
            .Select(x => new ChartOfAccountHeadResponse { Account = x.Id, Title = x.Title })
            .ToListAsync(cancellationToken))
            .Where(x => distinctPrefixes.Any(p => x.Account.StartsWith(p, StringComparison.Ordinal)))
            .ToList();
    }
}
