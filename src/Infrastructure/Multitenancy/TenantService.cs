using Retailer.Application.Common.Exceptions;
using Retailer.Application.Multitenancy;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Retailer.Infrastructure.Persistence;
using Retailer.Infrastructure.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using Retailer.Application.Common.Mailing;
using Retailer.Infrastructure.Persistence.Initialization;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal class TenantService : ITenantService
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly TenantDatabaseInitializer _dbInitializer;
    private readonly ILogger<TenantService> _logger;
    private readonly DatabaseSettings _dbSettings;
    private readonly MultitenancySettings _multitenancySettings;
    private readonly IMailService _mailService;

    public TenantService(
        TenantDbContext tenantDbContext,
        TenantDatabaseInitializer dbInitializer,
        ILogger<TenantService> logger,
        IOptions<DatabaseSettings> dbSettings,
        IOptions<MultitenancySettings> multitenancySettings,
        IMailService mailService)
    {
        _tenantDbContext = tenantDbContext;
        _dbInitializer = dbInitializer;
        _logger = logger;
        _dbSettings = dbSettings.Value;
        _multitenancySettings = multitenancySettings.Value;
        _mailService = mailService;
    }

    public async Task<List<TenantDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await _tenantDbContext.TenantInfo.ToListAsync(cancellationToken)).Adapt<List<TenantDto>>();

    public async Task<TenantDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Tenant with id '{id}' not found.");
        return tenant.Adapt<TenantDto>();
    }

    public async Task<CreateTenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (await _tenantDbContext.TenantInfo.AnyAsync(t => t.Identifier == request.Identifier, cancellationToken))
        {
            throw new ConflictException($"Tenant with identifier '{request.Identifier}' already exists.");
        }

        if (await _tenantDbContext.TenantInfo.AnyAsync(t => t.Id == request.Id, cancellationToken))
        {
            throw new ConflictException($"Tenant with id '{request.Id}' already exists.");
        }

        string connectionString = BuildConnectionString(request.DbProvider, request.Identifier);

        string rawIndustry = request.IndustryType?.Trim().ToLowerInvariant() ?? string.Empty;
        string industry = !string.IsNullOrWhiteSpace(rawIndustry)
            ? rawIndustry
            : (request.HasVariablePackFeature ? "wanda" : request.HasMobileShopFeature ? "mobile" : "general");

        string? rawSubCategory = request.BusinessSubType?.Trim().ToLowerInvariant();
        string? subCategory = !string.IsNullOrWhiteSpace(rawSubCategory)
            ? rawSubCategory
            : (industry == "general" ? "general_store" : null);

        bool isWanda = industry == "wanda" || request.HasVariablePackFeature;
        bool isMobile = industry == "mobile" || request.HasMobileShopFeature;

        var tenant = new AppTenantInfo
        {
            Id = request.Id,
            Identifier = request.Identifier,
            Name = request.Name,
            DbProvider = request.DbProvider,
            ConnectionString = connectionString,
            AdminEmail = request.AdminEmail,
            IsActive = true,
            IndustryType = industry,
            BusinessSubType = subCategory,
            HasVariablePackFeature = isWanda,
            HasMobileShopFeature = isMobile,
            ValidFrom = request.ValidFrom ?? DateTime.UtcNow,
            ValidUntil = request.ValidUntil,
            LicenseKey = Guid.NewGuid().ToString("N").ToUpper()
        };

        _tenantDbContext.TenantInfo.Add(tenant);
        await _tenantDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Tenant '{Identifier}' created with industry '{Industry}' and subcategory '{SubCategory}'. Provisioning database.", tenant.Identifier, tenant.IndustryType, tenant.BusinessSubType);
            await _dbInitializer.InitializeTenantDatabaseAsync(tenant, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database for tenant '{Identifier}'. Cleaning up tenant record.", tenant.Identifier);
            _tenantDbContext.TenantInfo.Remove(tenant);
            await _tenantDbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return new CreateTenantResponse { Id = tenant.Id, LicenseKey = tenant.LicenseKey! };
    }

    public async Task UpdateAsync(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Tenant with id '{request.Id}' not found.");

        if (!string.IsNullOrWhiteSpace(request.DbProvider) && !string.Equals(request.DbProvider, tenant.DbProvider, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Updating database provider for an existing tenant is not allowed.");
        }

        if (!string.IsNullOrWhiteSpace(request.IndustryType)
            && !string.IsNullOrWhiteSpace(tenant.IndustryType)
            && !string.Equals(request.IndustryType.Trim(), tenant.IndustryType.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Updating industry type for an existing tenant is not allowed. Industry and sub type must remain the same.");
        }

        if (!string.IsNullOrWhiteSpace(request.BusinessSubType)
            && !string.IsNullOrWhiteSpace(tenant.BusinessSubType)
            && !string.Equals(request.BusinessSubType.Trim(), tenant.BusinessSubType.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Updating business sub-category for an existing tenant is not allowed. Industry and sub type must remain the same.");
        }

        tenant.Name = request.Name;
        tenant.AdminEmail = request.AdminEmail;
        tenant.ValidFrom = request.ValidFrom ?? tenant.ValidFrom;
        tenant.ValidUntil = request.ValidUntil;

        // If previously unassigned (e.g. legacy tenant), assign once
        if (string.IsNullOrWhiteSpace(tenant.IndustryType) && !string.IsNullOrWhiteSpace(request.IndustryType))
        {
            tenant.IndustryType = request.IndustryType.Trim().ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(tenant.BusinessSubType) && !string.IsNullOrWhiteSpace(request.BusinessSubType))
        {
            tenant.BusinessSubType = request.BusinessSubType.Trim().ToLowerInvariant();
        }

        tenant.HasVariablePackFeature = tenant.IndustryType == "wanda" || request.HasVariablePackFeature;
        tenant.HasMobileShopFeature = tenant.IndustryType == "mobile" || request.HasMobileShopFeature;

        await _tenantDbContext.SaveChangesAsync(cancellationToken);
    }

    private string BuildConnectionString(string dbProvider, string tenantIdentifier)
    {
        string providerLower = dbProvider.ToLowerInvariant();
        string baseConnectionString = providerLower switch
        {
            DbProviderKeys.Npgsql or "npgsql" or "postgres" => _multitenancySettings.DefaultConnectionString,
            DbProviderKeys.SqlServer or "sqlserver" => _dbSettings.ConnectionString,
            _ => throw new BadRequestException($"Unsupported DB Provider: {dbProvider}")
        };

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new BadRequestException($"Base connection string for provider '{dbProvider}' is not configured.");
        }

        string dbName = tenantIdentifier.ToLowerInvariant();

        if (providerLower is DbProviderKeys.Npgsql or "npgsql" or "postgres")
        {
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Database = dbName
            };
            return builder.ConnectionString;
        }
        else if (providerLower is DbProviderKeys.SqlServer or "sqlserver")
        {
            var builder = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = dbName
            };
            return builder.ConnectionString;
        }

        throw new BadRequestException($"Unsupported DB Provider: {dbProvider}");
    }

    public async Task ActivateAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Tenant with id '{id}' not found.");

        tenant.IsActive = true;
        await _tenantDbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tenant '{Identifier}' activated.", tenant.Identifier);
    }

    public async Task DeactivateAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Tenant with id '{id}' not found.");

        tenant.IsActive = false;
        await _tenantDbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tenant '{Identifier}' deactivated.", tenant.Identifier);
    }

    public async Task<TenantDto> GetTenantIdByLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var tenant = await _tenantDbContext.TenantInfo
            .FirstOrDefaultAsync(t => t.LicenseKey == licenseKey, cancellationToken);

        if (tenant == null)
        {
            throw new NotFoundException("Invalid license key.");
        }

        if (!tenant.IsActive)
        {
            throw new ConflictException("Tenant is inactive.");
        }

        return tenant.Adapt<TenantDto>();
    }

    public async Task InviteTenantAsync(string id, InviteTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Tenant with id '{id}' not found.");

        string recipientEmail = !string.IsNullOrWhiteSpace(request.RecipientEmail)
            ? request.RecipientEmail.Trim()
            : tenant.AdminEmail?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new BadRequestException("No recipient email provided and tenant has no admin email configured.");
        }

        string desktopUrl = !string.IsNullOrWhiteSpace(request.DesktopSetupUrl)
            ? request.DesktopSetupUrl.Trim()
            : "http://retailsuite-desktop.bizgripsolutions.com/setup.exe";

        string androidUrl = !string.IsNullOrWhiteSpace(request.AndroidAppUrl)
            ? request.AndroidAppUrl.Trim()
            : "https://drive.google.com/file/d/1R9YrB8ijLX3Cz_aOuQS7XDTDOK-7kW5-/view?usp=sharing";

        string webUrl = !string.IsNullOrWhiteSpace(request.WebAppUrl)
            ? request.WebAppUrl.Trim()
            : "https://retailsuite.bizgripsolutions.com/";

        string licenseKey = tenant.LicenseKey ?? "N/A";
        string defaultPassword = ApplicationDbSeeder.InitialDataSeedConstants.DefaultPassword;

        string customMsgSection = !string.IsNullOrWhiteSpace(request.CustomMessage)
            ? $@"<div class='card' style='background:#fef3c7;border-color:#fde68a;'><div class='card-title' style='color:#92400e;'>Message from Administrator</div><p style='margin:0;font-size:14px;color:#78350f;'>{System.Net.WebUtility.HtmlEncode(request.CustomMessage.Trim())}</p></div>"
            : string.Empty;

        string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
    .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }}
    .header {{ background: linear-gradient(135deg, #1e3a8a, #2563eb); color: #ffffff; padding: 32px 24px; text-align: center; }}
    .header h1 {{ margin: 0 0 8px 0; font-size: 24px; font-weight: 700; }}
    .header p {{ margin: 0; opacity: 0.9; font-size: 14px; }}
    .content {{ padding: 28px 24px; }}
    .badge {{ display: inline-block; background: #eff6ff; color: #1d4ed8; padding: 4px 10px; border-radius: 9999px; font-size: 12px; font-weight: 600; margin-bottom: 16px; }}
    .card {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; margin: 20px 0; }}
    .card-title {{ font-size: 13px; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 12px; }}
    .info-row {{ padding: 8px 0; border-bottom: 1px solid #f1f5f9; font-size: 14px; }}
    .info-row:last-child {{ border-bottom: none; }}
    .info-label {{ color: #64748b; font-weight: 500; display: inline-block; width: 160px; }}
    .info-value {{ font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace; font-weight: 600; color: #0f172a; }}
    .btn {{ display: inline-block; background-color: #2563eb; color: #ffffff !important; font-size: 14px; font-weight: 600; text-decoration: none; padding: 12px 22px; border-radius: 6px; margin: 6px 4px 6px 0; text-align: center; }}
    .btn-secondary {{ background-color: #0f172a; }}
    .steps {{ margin: 12px 0 0 0; padding-left: 20px; font-size: 14px; line-height: 1.6; color: #334155; }}
    .footer {{ background: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 24px; text-align: center; font-size: 12px; color: #94a3b8; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>RetailSuite Onboarding</h1>
      <p>Your organization account is ready for use</p>
    </div>
    <div class='content'>
      <div class='badge'>Organization Access</div>
      <p style='font-size: 15px; line-height: 1.6; margin-top: 0;'>
        Hello,<br><br>
        Welcome to <strong>RetailSuite</strong>! Your organization account for <strong>{System.Net.WebUtility.HtmlEncode(tenant.Name)}</strong> has been provisioned.
      </p>

      {customMsgSection}

      <div class='card'>
        <div class='card-title'>Credentials & License Information</div>
        <div class='info-row'><span class='info-label'>Organization:</span><span class='info-value'>{System.Net.WebUtility.HtmlEncode(tenant.Name)}</span></div>
        <div class='info-row'><span class='info-label'>License Key:</span><span class='info-value'>{licenseKey}</span></div>
        <div class='info-row'><span class='info-label'>Initial Username:</span><span class='info-value'>Admin</span></div>
        <div class='info-row'><span class='info-label'>Temporary Password:</span><span class='info-value'>{defaultPassword}</span></div>
      </div>

      <div style='margin: 24px 0 16px 0;'>
        <a href='{desktopUrl}' class='btn' target='_blank'>📥 Download Windows Desktop Setup</a>
        <a href='{androidUrl}' class='btn' target='_blank' style='background-color: #16a34a;'>📱 Download Android App</a>
        <a href='{webUrl}' class='btn btn-secondary' target='_blank'>🌐 Open Web Portal</a>
      </div>

      <div class='card'>
        <div class='card-title'>Getting Started Guide</div>
        <ol class='steps'>
          <li><strong>Download and install</strong> the Windows Desktop ERP application, Android Mobile App, or open the Web Portal.</li>
          <li>When prompted, register using your <strong>License Key</strong> (<code>{licenseKey}</code>).</li>
          <li>Sign in using the initial administrator credentials shown above.</li>
          <li>For security, please change your password immediately after your first sign-in.</li>
        </ol>
      </div>

      <p style='font-size: 13px; color: #64748b;'>
        Need help getting set up? Reach out to our technical support team at <a href='mailto:support@bizgripsolutions.com' style='color: #2563eb;'>support@bizgripsolutions.com</a>.
      </p>
    </div>
    <div class='footer'>
      &copy; BizGrip Solutions. All rights reserved.<br>
      Sent from onboarding@bizgripsolutions.com
    </div>
  </div>
</body>
</html>";

        var mailRequest = new MailRequest(
            to: new List<string> { recipientEmail },
            subject: $"Welcome to RetailSuite - Access Details for {tenant.Name}",
            body: htmlBody,
            from: "onboarding@bizgripsolutions.com",
            displayName: "RetailSuite Onboarding",
            isHtml: true
        );

        _logger.LogInformation("Sending onboarding invitation email to '{Email}' for tenant '{TenantIdentifier}' from onboarding@bizgripsolutions.com.", recipientEmail, tenant.Identifier);
        await _mailService.SendAsync(mailRequest, cancellationToken);
    }
}
