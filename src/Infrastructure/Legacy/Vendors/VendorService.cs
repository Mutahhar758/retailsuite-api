using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Vendors;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Vendors;

internal class VendorService : IVendorService
{
    private const string VendorDefaultAccountTitle = "Suppliers";

    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<SupplierDetail> _vendorDetailRepository;
    private readonly IRepository<DefaultAccount> _defaultAccountRepository;

    public VendorService(
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<SupplierDetail> vendorDetailRepository,
        IRepository<DefaultAccount> defaultAccountRepository)
    {
        _chartOfAccountRepository = chartOfAccountRepository;
        _vendorDetailRepository = vendorDetailRepository;
        _defaultAccountRepository = defaultAccountRepository;
    }

    public async Task<List<VendorResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var vendorAccountPrefix = await GetVendorAccountPrefixAsync(cancellationToken);

        var accounts = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && x.Id.StartsWith(vendorAccountPrefix))
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Title })
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return [];

        var accountIds = accounts.Select(x => x.Id).ToList();

        var detailMap = await _vendorDetailRepository.GetAll()
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
                x.ShowInSales,
                x.CreatedBy,
                x.CreatedOn,
                x.LastModifiedBy,
                x.LastModifiedOn
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return accounts.Select(x =>
        {
            detailMap.TryGetValue(x.Id, out var detail);
            return new VendorResponse
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
                ShowInSales = detail?.ShowInSales ?? false,
                CreatedBy = detail?.CreatedBy,
                CreatedOn = detail?.CreatedOn,
                LastModifiedBy = detail?.LastModifiedBy,
                LastModifiedOn = detail?.LastModifiedOn
            };
        }).ToList();
    }

    public async Task UpsertAsync(string account, VendorUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new BadRequestException("Account is required.");

        var vendorAccountPrefix = await GetVendorAccountPrefixAsync(cancellationToken);

        var accountExists = await _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .AnyAsync(x => x.Id == account && x.AccLevel == 5 && x.Id.StartsWith(vendorAccountPrefix), cancellationToken);

        if (!accountExists)
            throw new NotFoundException($"Vendor account '{account}' not found.");

        var vendorDetail = await _vendorDetailRepository.GetByIdAsync(account, cancellationToken);

        if (vendorDetail is null)
        {
            vendorDetail = new SupplierDetail
            {
                Id = account
            };

            ApplyRequest(vendorDetail, request);
            await _vendorDetailRepository.AddAsync(vendorDetail);
            return;
        }

        ApplyRequest(vendorDetail, request);
        await _vendorDetailRepository.UpdateAsync(vendorDetail);
    }

    public async Task<string> CreateAsync(VendorCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Vendor title/name is required.");

        var vendorAccountPrefix = await GetVendorAccountPrefixAsync(cancellationToken);

        var parent = await _chartOfAccountRepository.GetByIdAsync(vendorAccountPrefix, cancellationToken);
        if (parent is null)
            throw new NotFoundException($"Parent vendor account '{vendorAccountPrefix}' not found in Chart of Accounts.");

        // Generate the next level 5 account suffix under the vendors parent
        var maxChildId = await _chartOfAccountRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.ParentId == vendorAccountPrefix)
            .Select(x => x.Id)
            .OrderByDescending(x => x.Length)
            .ThenByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var maxSuffix = 0L;
        if (!string.IsNullOrWhiteSpace(maxChildId) && maxChildId.Length > vendorAccountPrefix.Length)
        {
            long.TryParse(maxChildId[vendorAccountPrefix.Length..], out maxSuffix);
        }

        var newAccountCode = vendorAccountPrefix + (maxSuffix + 1).ToString("D3");
        var accLevel = parent.AccLevel + 1;

        // Create Chart of Account entry
        var newAccount = new ChartOfAccount
        {
            Id = newAccountCode,
            Title = request.Title,
            ParentId = vendorAccountPrefix,
            AccLevel = accLevel,
            AccType = "Detail"
        };
        await _chartOfAccountRepository.AddAsync(newAccount);

        // Create SupplierDetail entry
        var vendorDetail = new SupplierDetail
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
            ShowInSales = request.ShowInSales
        };
        await _vendorDetailRepository.AddAsync(vendorDetail);

        return newAccountCode;
    }

    public async Task UpdateAsync(string account, VendorUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account))
            throw new BadRequestException("Account is required.");

        var vendorAccountPrefix = await GetVendorAccountPrefixAsync(cancellationToken);

        // Fetch the chart of account
        var chartOfAccount = await _chartOfAccountRepository.GetByIdAsync(account, cancellationToken);

        if (chartOfAccount is null || chartOfAccount.AccLevel != 5 || !chartOfAccount.Id.StartsWith(vendorAccountPrefix))
            throw new NotFoundException($"Vendor account '{account}' not found.");

        // If Title is provided, update the ChartOfAccount's Title
        if (!string.IsNullOrWhiteSpace(request.Title) && chartOfAccount.Title != request.Title)
        {
            chartOfAccount.Title = request.Title;
            await _chartOfAccountRepository.UpdateAsync(chartOfAccount);
        }

        var vendorDetail = await _vendorDetailRepository.GetByIdAsync(account, cancellationToken);

        if (vendorDetail is null)
        {
            vendorDetail = new SupplierDetail
            {
                Id = account
            };

            ApplyRequest(vendorDetail, request);
            await _vendorDetailRepository.AddAsync(vendorDetail);
            return;
        }

        ApplyRequest(vendorDetail, request);
        await _vendorDetailRepository.UpdateAsync(vendorDetail);
    }

    private async Task<string> GetVendorAccountPrefixAsync(CancellationToken cancellationToken)
    {
        var defaultAccount = await _defaultAccountRepository.GetAll()
             .AsNoTracking()
             .Where(x => x.Title == VendorDefaultAccountTitle)
             .Select(x => new { x.AccountId, x.MapAccountId })
             .FirstOrDefaultAsync(cancellationToken);

        var accountPrefix = defaultAccount?.MapAccountId ?? defaultAccount?.AccountId;
        if (string.IsNullOrWhiteSpace(accountPrefix))
            throw new NotFoundException($"Default account mapping for '{VendorDefaultAccountTitle}' is not configured.");

        return accountPrefix;
    }

    private static void ApplyRequest(SupplierDetail vendorDetail, VendorUpsertRequest request)
    {
        vendorDetail.Email = request.Email;
        vendorDetail.Fax = request.Fax;
        vendorDetail.Cnic = request.Cnic;
        vendorDetail.Address = request.Address;
        vendorDetail.Qualification = request.Qualification;
        vendorDetail.Phone1 = request.Phone1;
        vendorDetail.Phone2 = request.Phone2;
        vendorDetail.SmsNumber = request.SmsNumber;
        vendorDetail.Iban = request.Iban;
        vendorDetail.SmsAlert = request.SmsAlert;
        vendorDetail.EmailAlert = request.EmailAlert;
        vendorDetail.Active = request.Active;
        vendorDetail.ShowInSales = request.ShowInSales;
    }

    private static void ApplyRequest(SupplierDetail vendorDetail, VendorUpdateRequest request)
    {
        vendorDetail.Email = request.Email;
        vendorDetail.Fax = request.Fax;
        vendorDetail.Cnic = request.Cnic;
        vendorDetail.Address = request.Address;
        vendorDetail.Qualification = request.Qualification;
        vendorDetail.Phone1 = request.Phone1;
        vendorDetail.Phone2 = request.Phone2;
        vendorDetail.SmsNumber = request.SmsNumber;
        vendorDetail.Iban = request.Iban;
        vendorDetail.SmsAlert = request.SmsAlert;
        vendorDetail.EmailAlert = request.EmailAlert;
        vendorDetail.Active = request.Active;
        vendorDetail.ShowInSales = request.ShowInSales;
    }
}
