using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.CustomerDetails;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.CustomerDetails;

internal class CustomerService : ICustomerService
{
    private const string CustomerDefaultAccountTitle = "Customers";

    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<CustomerDetail> _customerDetailRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;

    public CustomerService(
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<CustomerDetail> customerDetailRepository,
        IRepository<DefaultAccount> defaultAccountRepository)
    {
        _chartOfAccountRepository = chartOfAccountRepository;
        _customerDetailRepository = customerDetailRepository;
        _defaultAccountRepository = defaultAccountRepository;
    }

    public async Task<List<CustomerResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var customerAccountPrefix = await GetCustomerAccountPrefixAsync(cancellationToken);

        var accounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.Id.StartsWith(customerAccountPrefix))
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Title })
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return [];

        var accountIds = accounts.Select(x => x.Id).ToList();

        var detailMap = await _customerDetailRepository.GetAll()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.Fax,
                x.Cnic,
                x.Address,
                x.Qualification,
                x.Phone1,
                x.Phone2,
                x.SmsNumber,
                x.Iban,
                x.SmsAlert,
                x.EmailAlert,
                x.Active,
                x.CreatedBy,
                x.CreatedOn,
                x.LastModifiedBy,
                x.LastModifiedOn
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return accounts.Select(x =>
        {
            detailMap.TryGetValue(x.Id, out var detail);
            return new CustomerResponse
            {
                Account = x.Id,
                Title = x.Title,
                Email = detail?.Email,
                Fax = detail?.Fax,
                Cnic = detail?.Cnic,
                Address = detail?.Address,
                Qualification = detail?.Qualification,
                Phone1 = detail?.Phone1,
                Phone2 = detail?.Phone2,
                SmsNumber = detail?.SmsNumber,
                Iban = detail?.Iban,
                SmsAlert = detail?.SmsAlert ?? false,
                EmailAlert = detail?.EmailAlert ?? false,
                Active = detail?.Active ?? true,
                CreatedBy = detail?.CreatedBy,
                CreatedOn = detail?.CreatedOn,
                LastModifiedBy = detail?.LastModifiedBy,
                LastModifiedOn = detail?.LastModifiedOn
            };
        }).ToList();
    }

    public async Task<string> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Customer title/name is required.");

        var customerAccountPrefix = await GetCustomerAccountPrefixAsync(cancellationToken);

        var parent = await _chartOfAccountRepository.GetByIdAsync(customerAccountPrefix, cancellationToken);
        if (parent is null)
            throw new NotFoundException($"Parent customer account '{customerAccountPrefix}' not found in Chart of Accounts.");

        // Generate the next level 5 account suffix under the customers parent
        var maxChildId = await _chartOfAccountRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.ParentId == customerAccountPrefix)
            .Select(x => x.Id)
            .OrderByDescending(x => x.Length)
            .ThenByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var maxSuffix = 0L;
        if (!string.IsNullOrWhiteSpace(maxChildId) && maxChildId.Length > customerAccountPrefix.Length)
        {
            long.TryParse(maxChildId[customerAccountPrefix.Length..], out maxSuffix);
        }

        var newAccountCode = customerAccountPrefix + (maxSuffix + 1).ToString("D3");
        var accLevel = parent.AccLevel + 1;

        // Create Chart of Account entry
        var newAccount = new ChartOfAccount
        {
            Id = newAccountCode,
            Title = request.Title,
            ParentId = customerAccountPrefix,
            AccLevel = accLevel,
            AccType = "Detail"
        };
        await _chartOfAccountRepository.AddAsync(newAccount);

        // Create CustomerDetail entry
        var customerDetail = new CustomerDetail
        {
            Id = newAccountCode,
            Email = request.Email,
            Fax = request.Fax,
            Cnic = request.Cnic,
            Address = request.Address,
            Qualification = request.Qualification,
            Phone1 = request.Phone1,
            Phone2 = request.Phone2,
            SmsNumber = request.SmsNumber,
            Iban = request.Iban,
            SmsAlert = request.SmsAlert,
            EmailAlert = request.EmailAlert,
            Active = request.Active
        };
        await _customerDetailRepository.AddAsync(customerDetail);

        return newAccountCode;
    }

    public async Task UpdateAsync(string account, CustomerUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new BadRequestException("Account is required.");

        var customerAccountPrefix = await GetCustomerAccountPrefixAsync(cancellationToken);

        // Fetch the chart of account (without AsNoTracking so we can update it if needed)
        var chartOfAccount = await _chartOfAccountRepository.GetByIdAsync(account, cancellationToken);

        if (chartOfAccount is null || chartOfAccount.AccLevel != 5 || !chartOfAccount.Id.StartsWith(customerAccountPrefix))
            throw new NotFoundException($"Customer account '{account}' not found.");

        // If Title is provided, update the ChartOfAccount's Title
        if (!string.IsNullOrWhiteSpace(request.Title) && chartOfAccount.Title != request.Title)
        {
            chartOfAccount.Title = request.Title;
            await _chartOfAccountRepository.UpdateAsync(chartOfAccount);
        }

        var customerDetail = await _customerDetailRepository.GetByIdAsync(account, cancellationToken);

        if (customerDetail is null)
        {
            customerDetail = new CustomerDetail
            {
                Id = account
            };

            ApplyRequest(customerDetail, request);
            await _customerDetailRepository.AddAsync(customerDetail);
            return;
        }

        ApplyRequest(customerDetail, request);
        await _customerDetailRepository.UpdateAsync(customerDetail);
    }

    private async Task<string> GetCustomerAccountPrefixAsync(CancellationToken cancellationToken)
    {
        var defaultAccount = await _defaultAccountRepository.GetAll()
             .AsNoTracking()
             .Where(x => x.Title == CustomerDefaultAccountTitle)
             .Select(x => new { x.AccountId, x.MapAccountId })
             .FirstOrDefaultAsync(cancellationToken);

        var accountPrefix = defaultAccount?.MapAccountId ?? defaultAccount?.AccountId;
        if (string.IsNullOrWhiteSpace(accountPrefix))
            throw new NotFoundException($"Default account mapping for '{CustomerDefaultAccountTitle}' is not configured.");

        return accountPrefix;
    }

    private static void ApplyRequest(CustomerDetail customerDetail, CustomerUpdateRequest request)
    {
        customerDetail.Email = request.Email;
        customerDetail.Fax = request.Fax;
        customerDetail.Cnic = request.Cnic;
        customerDetail.Address = request.Address;
        customerDetail.Qualification = request.Qualification;
        customerDetail.Phone1 = request.Phone1;
        customerDetail.Phone2 = request.Phone2;
        customerDetail.SmsNumber = request.SmsNumber;
        customerDetail.Iban = request.Iban;
        customerDetail.SmsAlert = request.SmsAlert;
        customerDetail.EmailAlert = request.EmailAlert;
        customerDetail.Active = request.Active;
    }
}
