using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Common.Interfaces;
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
    private readonly IRepository<CustomerSupplyItem> _customerSupplyItemRepository;
    private readonly IRepository<ItemDetail> _itemDetailRepository;
    private readonly IMediaServiceClient _mediaServiceClient;

    public CustomerService(
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<CustomerDetail> customerDetailRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IRepository<CustomerSupplyItem> customerSupplyItemRepository,
        IRepository<ItemDetail> itemDetailRepository,
        IMediaServiceClient mediaServiceClient)
    {
        _chartOfAccountRepository = chartOfAccountRepository;
        _customerDetailRepository = customerDetailRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _customerSupplyItemRepository = customerSupplyItemRepository;
        _itemDetailRepository = itemDetailRepository;
        _mediaServiceClient = mediaServiceClient;
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
                x.MediaId,
                x.CreatedBy,
                x.CreatedOn,
                x.LastModifiedBy,
                x.LastModifiedOn
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var supplyItems = await _customerSupplyItemRepository.GetAll()
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.CustomerAccountId))
            .Join(_itemDetailRepository.GetAll().AsNoTracking(),
                cs => cs.ItemId,
                item => item.Id,
                (cs, item) => new
                {
                    cs.CustomerAccountId,
                    cs.ItemId,
                    ItemTitle = item.Title,
                    cs.Qty,
                    cs.SecQty,
                    cs.Rate,
                    cs.AddLess,
                    cs.Discount
                })
            .ToListAsync(cancellationToken);

        var supplyItemMap = supplyItems
            .GroupBy(x => x.CustomerAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(i => new CustomerSupplyItemDto
                {
                    ItemId = i.ItemId,
                    ItemTitle = i.ItemTitle,
                    Qty = i.Qty,
                    SecQty = i.SecQty,
                    Rate = i.Rate,
                    AddLess = i.AddLess,
                    Discount = i.Discount
                }).ToList());

        var mappedCustomers = accounts.Select(x =>
        {
            detailMap.TryGetValue(x.Id, out var detail);
            supplyItemMap.TryGetValue(x.Id, out var items);
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
                MediaId = detail?.MediaId,
                CreatedBy = detail?.CreatedBy,
                CreatedOn = detail?.CreatedOn,
                LastModifiedBy = detail?.LastModifiedBy,
                LastModifiedOn = detail?.LastModifiedOn,
                SupplyItems = items ?? new()
            };
        }).ToList();

        await PopulateMediaUrlsAsync(mappedCustomers, cancellationToken);

        return mappedCustomers;
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
            Active = request.Active,
            MediaId = request.MediaId
        };
        await _customerDetailRepository.AddAsync(customerDetail);

        if (request.SupplyItems is not null && request.SupplyItems.Count > 0)
        {
            var itemsToAdd = request.SupplyItems
                .Where(i => !string.IsNullOrWhiteSpace(i.ItemId))
                .Select(item => new CustomerSupplyItem
                {
                    CustomerAccountId = newAccountCode,
                    ItemId = item.ItemId,
                    Qty = item.Qty,
                    SecQty = item.SecQty,
                    Rate = item.Rate,
                    AddLess = item.AddLess,
                    Discount = item.Discount
                }).ToList();

            if (itemsToAdd.Count > 0)
            {
                await _customerSupplyItemRepository.AddRangeAsync(itemsToAdd);
            }
        }

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
        }
        else
        {
            ApplyRequest(customerDetail, request);
            await _customerDetailRepository.UpdateAsync(customerDetail);
        }

        if (request.SupplyItems is not null)
        {
            var existingItems = await _customerSupplyItemRepository.GetAll()
                .Where(x => x.CustomerAccountId == account)
                .ToListAsync(cancellationToken);

            if (existingItems.Count > 0)
            {
                await _customerSupplyItemRepository.DeleteRangeAsync(existingItems, true);
            }

            var itemsToAdd = request.SupplyItems
                .Where(i => !string.IsNullOrWhiteSpace(i.ItemId))
                .Select(item => new CustomerSupplyItem
                {
                    CustomerAccountId = account,
                    ItemId = item.ItemId,
                    Qty = item.Qty,
                    SecQty = item.SecQty,
                    Rate = item.Rate,
                    AddLess = item.AddLess,
                    Discount = item.Discount
                }).ToList();

            if (itemsToAdd.Count > 0)
            {
                await _customerSupplyItemRepository.AddRangeAsync(itemsToAdd);
            }
        }
    }


    public async Task<List<CustomerSupplyItemDto>> GetSupplyItemsAsync(string? customerId, string? itemId, CancellationToken cancellationToken)
    {
        var query = _customerSupplyItemRepository.GetAll().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerId))
            query = query.Where(x => x.CustomerAccountId == customerId);

        if (!string.IsNullOrWhiteSpace(itemId))
            query = query.Where(x => x.ItemId == itemId);

        return await query
            .Join(_itemDetailRepository.GetAll().AsNoTracking(),
                cs => cs.ItemId,
                item => item.Id,
                (cs, item) => new CustomerSupplyItemDto
                {
                    CustomerAccountId = cs.CustomerAccountId,
                    ItemId = cs.ItemId,
                    ItemTitle = item.Title,
                    Qty = cs.Qty,
                    SecQty = cs.SecQty,
                    Rate = cs.Rate,
                    AddLess = cs.AddLess,
                    Discount = cs.Discount
                })

            .ToListAsync(cancellationToken);
    }

    public async Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new BadRequestException("File name is required.");

        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName != fileName || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            throw new BadRequestException("Invalid file name. Only plain file names without paths are allowed.");
        }

        return await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "customer", cancellationToken);
    }

    private async Task PopulateMediaUrlsAsync(List<CustomerResponse> items, CancellationToken cancellationToken)
    {
        var tasks = items
            .Where(x => !string.IsNullOrEmpty(x.MediaId))
            .Select(async item =>
            {
                try
                {
                    var sasResponse = await _mediaServiceClient.GetViewTokenAsync(item.MediaId!, 24, cancellationToken);
                    if (sasResponse != null)
                    {
                        item.MediaUrl = sasResponse.ViewUrl;
                    }
                }
                catch
                {
                    // Fail-safe: ignore media service exceptions to keep main application running
                }
            });
        await Task.WhenAll(tasks);
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
        customerDetail.MediaId = request.MediaId;
    }
}

