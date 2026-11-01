using System.IO;
using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.CompanyDetails;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.CompanyDetails;

internal class CompanyDetailService : ICompanyDetailService
{
    private const string CompanyCategory = "Company";
    private const string LogoMediaIdKey = "Company.LogoMediaId";
    private const string EmailKey = "Company.Email";
    private const string WebsiteKey = "Company.Website";
    private const string NtnKey = "Company.Ntn";
    private const string StrnKey = "Company.Strn";

    private readonly IRepository<CompanyDetail> _repository;
    private readonly IRepository<Setting> _settingRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IMediaServiceClient _mediaServiceClient;

    public CompanyDetailService(
        IRepository<CompanyDetail> repository,
        IRepository<Setting> settingRepository,
        ICurrentTenant currentTenant,
        IMediaServiceClient mediaServiceClient)
    {
        _repository = repository;
        _settingRepository = settingRepository;
        _currentTenant = currentTenant;
        _mediaServiceClient = mediaServiceClient;
    }

    public async Task<CompanyDetailResponse?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAll()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        var companySettings = await _settingRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Category == CompanyCategory || x.Key.StartsWith("Company."))
            .ToListAsync(cancellationToken);

        string? GetSettingVal(string key) =>
            companySettings.FirstOrDefault(x => x.Key == key)?.Value;

        string bopCompanyName = !string.IsNullOrWhiteSpace(_currentTenant.Name)
            ? _currentTenant.Name!
            : (entity?.CompanyName ?? string.Empty);

        var logoMediaId = GetSettingVal(LogoMediaIdKey);
        string? logoUrl = null;

        if (!string.IsNullOrWhiteSpace(logoMediaId))
        {
            try
            {
                var sasResponse = await _mediaServiceClient.GetViewTokenAsync(logoMediaId, 24, cancellationToken);
                if (sasResponse != null)
                {
                    logoUrl = sasResponse.ViewUrl;
                }
            }
            catch
            {
                // Fail-safe: return company info even if storage service is temporarily unreachable
            }
        }

        return new CompanyDetailResponse
        {
            CompanyName = bopCompanyName,
            UrCompanyName = entity?.UrCompanyName,
            Descr = entity?.Descr,
            Address = entity?.Address,
            Phone = entity?.Phone,
            Cell = entity?.Cell,
            Cell2 = entity?.Cell2,
            ContactHeader = entity?.ContactHeader,
            Email = GetSettingVal(EmailKey),
            Website = GetSettingVal(WebsiteKey),
            Ntn = GetSettingVal(NtnKey),
            Strn = GetSettingVal(StrnKey),
            LogoMediaId = logoMediaId,
            LogoUrl = logoUrl,
            LastModifiedBy = entity?.LastModifiedBy,
            LastModifiedOn = entity?.LastModifiedOn ?? entity?.CreatedOn
        };
    }

    public async Task<CompanyDetailResponse> UpdateCurrentAsync(CompanyDetailUpdateRequest request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetAll()
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        // CompanyName is strictly managed by BOP (TenantInfo.Name)
        string bopCompanyName = !string.IsNullOrWhiteSpace(_currentTenant.Name)
            ? _currentTenant.Name!
            : (existing?.CompanyName ?? "Company");

        if (existing != null)
        {
            existing.CompanyName = bopCompanyName;
            existing.UrCompanyName = request.UrCompanyName?.Trim();
            existing.Descr = request.Descr?.Trim();
            existing.Address = request.Address?.Trim();
            existing.Phone = request.Phone?.Trim();
            existing.Cell = request.Cell?.Trim();
            existing.Cell2 = request.Cell2?.Trim();
            existing.ContactHeader = request.ContactHeader?.Trim();

            await _repository.UpdateAsync(existing);
        }
        else
        {
            var newCompany = new CompanyDetail
            {
                CompanyName = bopCompanyName,
                UrCompanyName = request.UrCompanyName?.Trim(),
                Descr = request.Descr?.Trim(),
                Address = request.Address?.Trim(),
                Phone = request.Phone?.Trim(),
                Cell = request.Cell?.Trim(),
                Cell2 = request.Cell2?.Trim(),
                ContactHeader = request.ContactHeader?.Trim()
            };

            await _repository.AddAsync(newCompany);
        }

        await UpsertCompanySettingAsync(LogoMediaIdKey, request.LogoMediaId?.Trim(), "Company Logo Media ID", cancellationToken);
        await UpsertCompanySettingAsync(EmailKey, request.Email?.Trim(), "Company Email Address", cancellationToken);
        await UpsertCompanySettingAsync(WebsiteKey, request.Website?.Trim(), "Company Website URL", cancellationToken);
        await UpsertCompanySettingAsync(NtnKey, request.Ntn?.Trim(), "Company NTN (National Tax Number)", cancellationToken);
        await UpsertCompanySettingAsync(StrnKey, request.Strn?.Trim(), "Company STRN (Sales Tax Registration Number)", cancellationToken);

        return await GetCurrentAsync(cancellationToken) ?? new CompanyDetailResponse { CompanyName = bopCompanyName };
    }

    public async Task<PresignedUploadUrlResponse?> GetLogoUploadUrlAsync(string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new BadRequestException("File name is required.");

        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName != fileName || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            throw new BadRequestException("Invalid file name. Only plain file names without paths are allowed.");
        }

        var response = await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "company-logos", cancellationToken);
        return response ?? throw new InternalServerException("Failed to generate upload URL for company logo.");
    }

    private async Task UpsertCompanySettingAsync(string key, string? value, string description, CancellationToken cancellationToken)
    {
        var existing = await _settingRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (existing != null)
        {
            existing.Value = value ?? string.Empty;
            existing.Description = description;
            existing.Category = CompanyCategory;
            await _settingRepository.UpdateAsync(existing);
        }
        else if (!string.IsNullOrEmpty(value))
        {
            await _settingRepository.AddAsync(new Setting
            {
                Key = key,
                Value = value,
                Description = description,
                Category = CompanyCategory
            });
        }
    }
}
