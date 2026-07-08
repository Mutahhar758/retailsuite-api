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
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal class TenantService : ITenantService
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly TenantDatabaseInitializer _dbInitializer;
    private readonly ILogger<TenantService> _logger;
    private readonly DatabaseSettings _dbSettings;
    private readonly MultitenancySettings _multitenancySettings;

    public TenantService(
        TenantDbContext tenantDbContext,
        TenantDatabaseInitializer dbInitializer,
        ILogger<TenantService> logger,
        IOptions<DatabaseSettings> dbSettings,
        IOptions<MultitenancySettings> multitenancySettings)
    {
        _tenantDbContext = tenantDbContext;
        _dbInitializer = dbInitializer;
        _logger = logger;
        _dbSettings = dbSettings.Value;
        _multitenancySettings = multitenancySettings.Value;
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

        var tenant = new AppTenantInfo
        {
            Id = request.Id,
            Identifier = request.Identifier,
            Name = request.Name,
            DbProvider = request.DbProvider,
            ConnectionString = connectionString,
            AdminEmail = request.AdminEmail,
            IsActive = true,
            HasSupplyFeature = request.HasSupplyFeature,
            HasSecondaryQty = request.HasSecondaryQty,
            ValidFrom = request.ValidFrom ?? DateTime.UtcNow,
            ValidUntil = request.ValidUntil,
            LicenseKey = Guid.NewGuid().ToString("N").ToUpper()
        };

        _tenantDbContext.TenantInfo.Add(tenant);
        await _tenantDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant '{Identifier}' created. Provisioning database.", tenant.Identifier);
        await _dbInitializer.InitializeTenantDatabaseAsync(tenant, cancellationToken);

        return new CreateTenantResponse { Id = tenant.Id, LicenseKey = tenant.LicenseKey! };
    }

    public async Task UpdateAsync(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Tenant with id '{request.Id}' not found.");

        string connectionString = BuildConnectionString(request.DbProvider, tenant.Identifier);

        tenant.Name = request.Name;
        tenant.AdminEmail = request.AdminEmail;
        tenant.ValidFrom = request.ValidFrom ?? tenant.ValidFrom;
        tenant.ValidUntil = request.ValidUntil;
        tenant.DbProvider = request.DbProvider;
        tenant.ConnectionString = connectionString;
        tenant.HasSupplyFeature = request.HasSupplyFeature;
        tenant.HasSecondaryQty = request.HasSecondaryQty;

        await _tenantDbContext.SaveChangesAsync(cancellationToken);
    }

    private string BuildConnectionString(string dbProvider, string tenantIdentifier)
    {
        string providerLower = dbProvider.ToLowerInvariant();
        string baseConnectionString = providerLower switch
        {
            DbProviderKeys.Npgsql => _multitenancySettings.DefaultConnectionString,
            DbProviderKeys.SqlServer => _dbSettings.ConnectionString,
            _ => throw new BadRequestException($"Unsupported DB Provider: {dbProvider}")
        };

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new BadRequestException($"Base connection string for provider '{dbProvider}' is not configured.");
        }

        string dbName = tenantIdentifier.ToLowerInvariant();

        if (providerLower == DbProviderKeys.Npgsql)
        {
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Database = dbName
            };
            return builder.ConnectionString;
        }
        else if (providerLower == DbProviderKeys.SqlServer)
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
}
