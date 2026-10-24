using Retailer.Infrastructure.Persistence;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Infrastructure.Persistence.Initialization;
using Retailer.Infrastructure.Persistence.Transactions;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.Multitenancy;

internal class TenantDatabaseInitializer
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseSettings _databaseSettings;
    private readonly MultitenancySettings _multitenancySettings;
    private readonly ILogger<TenantDatabaseInitializer> _logger;

    public TenantDatabaseInitializer(
        TenantDbContext tenantDbContext,
        IServiceProvider serviceProvider,
        IOptions<DatabaseSettings> databaseSettings,
        IOptions<MultitenancySettings> multitenancySettings,
        ILogger<TenantDatabaseInitializer> logger)
    {
        _tenantDbContext = tenantDbContext;
        _serviceProvider = serviceProvider;
        _databaseSettings = databaseSettings.Value;
        _multitenancySettings = multitenancySettings.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await EnsureDatabaseExistsAsync(_tenantDbContext, cancellationToken);

        // Migrate the root (tenant registry) database first
        if (_tenantDbContext.Database.GetMigrations().Any())
        {
            _logger.LogInformation("Applying Root Tenant Registry Migrations.");
            await _tenantDbContext.Database.MigrateAsync(cancellationToken);
        }

        var aiToolSeeder = new AiToolSeeder(_tenantDbContext, _serviceProvider.GetRequiredService<ILogger<AiToolSeeder>>());
        await aiToolSeeder.SeedAiToolsAsync(cancellationToken);

        await EnsureDefaultTenantAsync(cancellationToken);

        // Migrate and seed each registered tenant's database
        var tenants = await _tenantDbContext.TenantInfo.ToListAsync(cancellationToken);
        foreach (var tenant in tenants)
        {
            await InitializeTenantDatabaseAsync(tenant, cancellationToken);
        }
    }

    public async Task InitializeTenantDatabaseAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing database for tenant '{Tenant}'.", tenant.Identifier);

        using var scope = _serviceProvider.CreateScope();

        var tenantContextAccessor = scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        if (tenantContextAccessor is not IMultiTenantContextSetter tenantContextSetter)
        {
            throw new InvalidOperationException("Unable to set tenant context during tenant database initialization.");
        }

        tenantContextSetter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var tenantDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureDatabaseExistsAsync(tenantDbContext, cancellationToken);

        if (tenantDbContext.Database.GetMigrations().Any())
        {
            _logger.LogInformation("Applying Migrations for tenant '{Tenant}'.", tenant.Identifier);
            await tenantDbContext.Database.MigrateAsync(cancellationToken);
        }

        if (await tenantDbContext.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding database for tenant '{Tenant}'.", tenant.Identifier);
            var seeder = scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>();
            await seeder.SeedDatabaseAsync(tenantDbContext, cancellationToken, tenant.AdminEmail);

            // Commit any open transaction started by EfTransactionManager during seeding.
            // Normally EfTransactionMiddleware handles this, but seeding runs outside the HTTP pipeline.
            var txManager = scope.ServiceProvider.GetRequiredService<EfTransactionManager>();
            if (txManager.Transaction != null)
            {
                await txManager.Transaction.CommitAsync(cancellationToken);
                await txManager.Transaction.DisposeAsync();
                txManager.Transaction = null;
            }
        }
    }

    private async Task EnsureDefaultTenantAsync(CancellationToken cancellationToken)
    {
        var defaultTenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(t => t.Id == "root", cancellationToken);
        if (defaultTenant == null)
        {
            defaultTenant = new AppTenantInfo
            {
                Id = "root",
                Identifier = "root",
                Name = "Root Tenant",
                ConnectionString = _databaseSettings.ConnectionString,
                DbProvider = _databaseSettings.DBProvider,
                IsActive = true,
                IndustryType = "general",
                BusinessSubType = "general_store",
                ValidFrom = DateTime.UtcNow
            };

            _tenantDbContext.TenantInfo.Add(defaultTenant);
            await _tenantDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created default tenant '{TenantIdentifier}'.", defaultTenant.Identifier);
        }
        else
        {
            if (defaultTenant.DbProvider != _databaseSettings.DBProvider || defaultTenant.ConnectionString != _databaseSettings.ConnectionString)
            {
                defaultTenant.DbProvider = _databaseSettings.DBProvider;
                defaultTenant.ConnectionString = _databaseSettings.ConnectionString;
                _tenantDbContext.TenantInfo.Update(defaultTenant);
                await _tenantDbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated default tenant '{TenantIdentifier}' to match environment provider '{DbProvider}'.", defaultTenant.Identifier, _databaseSettings.DBProvider);
            }
        }
    }

    private async Task EnsureDatabaseExistsAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        switch (dbContext.Database.ProviderName)
        {
            case "Npgsql.EntityFrameworkCore.PostgreSQL":
                await EnsurePostgreSqlDatabaseExistsAsync(dbContext, cancellationToken);
                break;
            case "Microsoft.EntityFrameworkCore.SqlServer":
                await EnsureSqlServerDatabaseExistsAsync(dbContext, cancellationToken);
                break;
        }
    }

    private async Task EnsurePostgreSqlDatabaseExistsAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var rawConnStr = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(rawConnStr))
        {
            rawConnStr = dbContext.Database.GetDbConnection().ConnectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder(rawConnStr);
        if (string.IsNullOrWhiteSpace(builder.Password))
        {
            if (!string.IsNullOrWhiteSpace(_multitenancySettings.DefaultConnectionString))
            {
                try
                {
                    var fallback = new NpgsqlConnectionStringBuilder(_multitenancySettings.DefaultConnectionString);
                    if (!string.IsNullOrWhiteSpace(fallback.Password))
                    {
                        builder.Password = fallback.Password;
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(builder.Password) && !string.IsNullOrWhiteSpace(_databaseSettings.ConnectionString))
            {
                try
                {
                    var fallbackDb = new NpgsqlConnectionStringBuilder(_databaseSettings.ConnectionString);
                    if (!string.IsNullOrWhiteSpace(fallbackDb.Password))
                    {
                        builder.Password = fallbackDb.Password;
                    }
                }
                catch { }
            }
        }
        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            return;
        }

        var targetDatabase = builder.Database;
        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        existsCommand.Parameters.AddWithValue("name", targetDatabase);
        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;

        if (exists)
        {
            return;
        }

        var escapedDatabase = targetDatabase.Replace("\"", "\"\"");
        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{escapedDatabase}\"", connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Created database '{Database}'.", targetDatabase);
    }

    private async Task EnsureSqlServerDatabaseExistsAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var rawConnStr = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(rawConnStr))
        {
            rawConnStr = dbContext.Database.GetDbConnection().ConnectionString;
        }

        var builder = new SqlConnectionStringBuilder(rawConnStr);
        if (string.IsNullOrWhiteSpace(builder.Password) && !builder.IntegratedSecurity)
        {
            if (!string.IsNullOrWhiteSpace(_multitenancySettings.DefaultConnectionString))
            {
                try
                {
                    var fallback = new SqlConnectionStringBuilder(_multitenancySettings.DefaultConnectionString);
                    if (!string.IsNullOrWhiteSpace(fallback.Password))
                    {
                        builder.Password = fallback.Password;
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(builder.Password) && !string.IsNullOrWhiteSpace(_databaseSettings.ConnectionString))
            {
                try
                {
                    var fallbackDb = new SqlConnectionStringBuilder(_databaseSettings.ConnectionString);
                    if (!string.IsNullOrWhiteSpace(fallbackDb.Password))
                    {
                        builder.Password = fallbackDb.Password;
                    }
                }
                catch { }
            }
        }
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            return;
        }

        var targetDatabase = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = new SqlCommand("SELECT 1 FROM sys.databases WHERE name = @name", connection);
        existsCommand.Parameters.AddWithValue("@name", targetDatabase);
        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;

        if (exists)
        {
            return;
        }

        var escapedDatabase = targetDatabase.Replace("]", "]]");
        await using var createCommand = new SqlCommand($"CREATE DATABASE [{escapedDatabase}]", connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Created database '{Database}'.", targetDatabase);
    }
}
