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
    private readonly ILogger<TenantDatabaseInitializer> _logger;

    public TenantDatabaseInitializer(
        TenantDbContext tenantDbContext,
        IServiceProvider serviceProvider,
        IOptions<DatabaseSettings> databaseSettings,
        ILogger<TenantDatabaseInitializer> logger)
    {
        _tenantDbContext = tenantDbContext;
        _serviceProvider = serviceProvider;
        _databaseSettings = databaseSettings.Value;
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
        if (await _tenantDbContext.TenantInfo.AnyAsync(cancellationToken))
        {
            return;
        }

        var defaultTenant = new AppTenantInfo
        {
            Id = "root",
            Identifier = "root",
            Name = "Root Tenant",
            ConnectionString = _databaseSettings.ConnectionString,
            DbProvider = _databaseSettings.DBProvider,
            IsActive = true,
            ValidFrom = DateTime.UtcNow
        };

        _tenantDbContext.TenantInfo.Add(defaultTenant);
        await _tenantDbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created default tenant '{TenantIdentifier}'.", defaultTenant.Identifier);
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
        var builder = new NpgsqlConnectionStringBuilder(dbContext.Database.GetDbConnection().ConnectionString);
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
        var builder = new SqlConnectionStringBuilder(dbContext.Database.GetDbConnection().ConnectionString);
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
