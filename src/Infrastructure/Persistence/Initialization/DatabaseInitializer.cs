using Retailer.Infrastructure.Persistence.Context;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Persistence.Initialization;

internal class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ApplicationDbSeeder _dbSeeder;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantContextAccessor;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        ApplicationDbContext dbContext,
        ILogger<DatabaseInitializer> logger,
        ApplicationDbSeeder dbSeeder,
        IMultiTenantContextAccessor<AppTenantInfo> tenantContextAccessor)
    {
        _dbContext = dbContext;
        _logger = logger;
        _dbSeeder = dbSeeder;
        _tenantContextAccessor = tenantContextAccessor;
    }

    public async Task InitializeDatabasesAsync(CancellationToken cancellationToken)
    {
        if (_tenantContextAccessor.MultiTenantContext?.TenantInfo is null)
        {
            _logger.LogInformation("Skipping default ApplicationDb seeding because no tenant context is set. Tenant seeding is handled by TenantDatabaseInitializer.");
            return;
        }

        if (_dbContext.Database.GetMigrations().Any())
        {
            if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                _logger.LogInformation("Applying Migrations");
                await _dbContext.Database.MigrateAsync(cancellationToken);
            }

            if (await _dbContext.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogInformation("Connection to Database Succeeded.");

                await _dbSeeder.SeedDatabaseAsync(_dbContext, cancellationToken);
            }
        }

        _logger.LogInformation("For documentations and guides, visit https://www.fullstackhero.net");
        _logger.LogInformation("To Sponsor this project, visit https://opencollective.com/fullstackhero");
    }
}