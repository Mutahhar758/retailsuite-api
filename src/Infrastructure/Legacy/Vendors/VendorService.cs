using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Models;
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
    private readonly IMediaServiceClient _mediaServiceClient;

    public VendorService(
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<SupplierDetail> vendorDetailRepository,
        IRepository<DefaultAccount> defaultAccountRepository,
        IMediaServiceClient mediaServiceClient)
    {
        _chartOfAccountRepository = chartOfAccountRepository;
        _vendorDetailRepository = vendorDetailRepository;
        _defaultAccountRepository = defaultAccountRepository;
        _mediaServiceClient = mediaServiceClient;
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
                x.MediaId,
                x.CreatedBy,
                x.CreatedOn,
                x.LastModifiedBy,
                x.LastModifiedOn
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var mappedVendors = accounts.Select(x =>
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
                MediaId = detail?.MediaId,
                CreatedBy = detail?.CreatedBy,
                CreatedOn = detail?.CreatedOn,
                LastModifiedBy = detail?.LastModifiedBy,
                LastModifiedOn = detail?.LastModifiedOn
            };
        }).ToList();

        await PopulateMediaUrlsAsync(mappedVendors, cancellationToken);

        return mappedVendors;
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
            ShowInSales = request.ShowInSales,
            MediaId = request.MediaId
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

    public async Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new BadRequestException("File name is required.");

        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName != fileName || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            throw new BadRequestException("Invalid file name. Only plain file names without paths are allowed.");
        }

        var response = await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "vendor", cancellationToken);
        return response ?? throw new InternalServerException("Failed to generate upload URL for vendor image.");
    }

    private async Task PopulateMediaUrlsAsync(List<VendorResponse> items, CancellationToken cancellationToken)
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
        vendorDetail.MediaId = request.MediaId;
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
        vendorDetail.MediaId = request.MediaId;
    }

    public async Task<BulkOperationResult> BulkUpsertAsync(VendorBulkUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = new BulkOperationResult();
        if (request.Vendors == null || request.Vendors.Count == 0)
        {
            return result;
        }

        result.TotalProcessed = request.Vendors.Count;
        var vendorAccountPrefix = await GetVendorAccountPrefixAsync(cancellationToken);
        var parent = await _chartOfAccountRepository.GetByIdAsync(vendorAccountPrefix, cancellationToken);
        if (parent is null)
            throw new NotFoundException($"Parent vendor account '{vendorAccountPrefix}' not found in Chart of Accounts.");

        var maxChildId = await _chartOfAccountRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .Where(x => x.ParentId == vendorAccountPrefix)
            .Select(x => x.Id)
            .OrderByDescending(x => x.Length)
            .ThenByDescending(x => x)
            .FirstOrDefaultAsync(cancellationToken);

        var currentSuffix = 0L;
        if (!string.IsNullOrWhiteSpace(maxChildId) && maxChildId.Length > vendorAccountPrefix.Length)
        {
            long.TryParse(maxChildId[vendorAccountPrefix.Length..], out currentSuffix);
        }

        var accLevel = parent.AccLevel + 1;

        var updateCandidates = request.Vendors
            .Select((v, idx) => new { Item = v, RowIndex = idx + 1 })
            .Where(x => !string.IsNullOrWhiteSpace(x.Item.Account))
            .ToList();

        var updateAccountIds = updateCandidates.Select(x => x.Item.Account!.Trim()).Distinct().ToList();

        var existingCoas = await _chartOfAccountRepository.GetAll()
            .Where(x => updateAccountIds.Contains(x.Id) && x.AccLevel == 5 && x.Id.StartsWith(vendorAccountPrefix))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var existingDetails = await _vendorDetailRepository.GetAll()
            .Where(x => updateAccountIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var newCoasToAdd = new List<ChartOfAccount>();
        var newDetailsToAdd = new List<SupplierDetail>();
        var coasToUpdate = new List<ChartOfAccount>();
        var detailsToUpdate = new List<SupplierDetail>();

        for (int i = 0; i < request.Vendors.Count; i++)
        {
            var item = request.Vendors[i];
            var rowIndex = i + 1;

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                result.FailedCount++;
                result.Errors.Add(new BulkItemError
                {
                    RowIndex = rowIndex,
                    Identifier = item.Account ?? $"Row #{rowIndex}",
                    ErrorMessage = "Vendor name/title is required."
                });
                continue;
            }

            var account = item.Account?.Trim();
            if (!string.IsNullOrWhiteSpace(account) && existingCoas.TryGetValue(account, out var coa))
            {
                if (coa.Title != item.Title)
                {
                    coa.Title = item.Title;
                    coasToUpdate.Add(coa);
                }

                if (existingDetails.TryGetValue(account, out var detail))
                {
                    detail.Email = item.Email;
                    detail.Fax = item.Fax;
                    detail.Cnic = item.Cnic;
                    detail.Address = item.Address;
                    detail.Qualification = item.Qualification;
                    detail.Phone1 = item.Phone1;
                    detail.Phone2 = item.Phone2;
                    detail.SmsNumber = item.SmsNumber;
                    detail.Iban = item.Iban;
                    detail.SmsAlert = item.SmsAlert;
                    detail.EmailAlert = item.EmailAlert;
                    detail.Active = item.Active;
                    detail.ShowInSales = item.ShowInSales;
                    detailsToUpdate.Add(detail);
                }
                else
                {
                    var newDetail = new SupplierDetail
                    {
                        Id = account,
                        Email = item.Email,
                        Fax = item.Fax,
                        Cnic = item.Cnic,
                        Address = item.Address,
                        Qualification = item.Qualification,
                        Phone1 = item.Phone1,
                        Phone2 = item.Phone2,
                        SmsNumber = item.SmsNumber,
                        Iban = item.Iban,
                        SmsAlert = item.SmsAlert,
                        EmailAlert = item.EmailAlert,
                        Active = item.Active,
                        ShowInSales = item.ShowInSales
                    };
                    newDetailsToAdd.Add(newDetail);
                }

                result.UpdatedCount++;
            }
            else
            {
                currentSuffix++;
                var newAccountCode = vendorAccountPrefix + currentSuffix.ToString("D3");

                var newAccount = new ChartOfAccount
                {
                    Id = newAccountCode,
                    Title = item.Title,
                    ParentId = vendorAccountPrefix,
                    AccLevel = accLevel,
                    AccType = "Detail"
                };
                newCoasToAdd.Add(newAccount);

                var newDetail = new SupplierDetail
                {
                    Id = newAccountCode,
                    Email = item.Email,
                    Fax = item.Fax,
                    Cnic = item.Cnic,
                    Address = item.Address,
                    Qualification = item.Qualification,
                    Phone1 = item.Phone1,
                    Phone2 = item.Phone2,
                    SmsNumber = item.SmsNumber,
                    Iban = item.Iban,
                    SmsAlert = item.SmsAlert,
                    EmailAlert = item.EmailAlert,
                    Active = item.Active,
                    ShowInSales = item.ShowInSales
                };
                newDetailsToAdd.Add(newDetail);

                result.CreatedCount++;
            }
        }

        if (newCoasToAdd.Count > 0)
            await _chartOfAccountRepository.AddRangeAsync(newCoasToAdd, doSaveChanges: false);

        if (newDetailsToAdd.Count > 0)
            await _vendorDetailRepository.AddRangeAsync(newDetailsToAdd, doSaveChanges: false);

        if (coasToUpdate.Count > 0)
            await _chartOfAccountRepository.UpdateRangeAsync(coasToUpdate, doSaveChanges: false);

        if (detailsToUpdate.Count > 0)
            await _vendorDetailRepository.UpdateRangeAsync(detailsToUpdate, doSaveChanges: false);

        await _chartOfAccountRepository.SaveChangesAsync(cancellationToken);
        await _vendorDetailRepository.SaveChangesAsync(cancellationToken);

        return result;
    }
}
