using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Retailer.Infrastructure.Persistence;
using Retailer.Infrastructure.Persistence.Context;
using Retailer.Infrastructure.Persistence.Initialization;
using Retailer.Infrastructure.Persistence.Transactions;
using AppTenantInfo = Retailer.Domain.Multitenancy.TenantInfo;

namespace Retailer.Infrastructure.OnPremises;

/// <summary>
/// Startup initialization for a single-store on-premises server: creates the local PostgreSQL database if
/// missing, applies EF Core migrations and seeds roles/admin. No master tenant database is involved.
/// </summary>
internal class OnPremisesDatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly StoreSettings _store;
    private readonly DatabaseSettings _database;
    private readonly ILogger<OnPremisesDatabaseInitializer> _logger;

    public OnPremisesDatabaseInitializer(
        IServiceProvider serviceProvider,
        IOptions<StoreSettings> store,
        IOptions<DatabaseSettings> database,
        ILogger<OnPremisesDatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _store = store.Value;
        _database = database.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await EnsureDatabaseCreatedAsync(cancellationToken);
        await MigrateAndSeedAsync(cancellationToken);
    }

    public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken)
    {
        var tenant = StoreTenantFactory.Create(_store, _database);
        using var scope = _serviceProvider.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        if (accessor is IMultiTenantContextSetter setter)
        {
            setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsurePostgreSqlDatabaseExistsAsync(dbContext, cancellationToken);
    }

    public async Task MigrateAndSeedAsync(CancellationToken cancellationToken)
    {
        var tenant = StoreTenantFactory.Create(_store, _database);
        _logger.LogInformation("Initializing on-premises store database for '{Tenant}'.", tenant.Identifier);

        using var scope = _serviceProvider.CreateScope();

        var accessor = scope.ServiceProvider.GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        if (accessor is not IMultiTenantContextSetter setter)
        {
            throw new InvalidOperationException("Unable to set tenant context during on-premises initialization.");
        }

        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsurePostgreSqlDatabaseExistsAsync(dbContext, cancellationToken);

        if (dbContext.Database.GetMigrations().Any())
        {
            _logger.LogInformation("Applying migrations to the local store database.");
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("Cannot connect to the local store database.");
        }

        var seeder = scope.ServiceProvider.GetRequiredService<ApplicationDbSeeder>();
        await seeder.SeedDatabaseAsync(dbContext, cancellationToken, tenant.AdminEmail, _store.InitialAdminPassword);

        var txManager = scope.ServiceProvider.GetRequiredService<EfTransactionManager>();
        if (txManager.Transaction != null)
        {
            await txManager.Transaction.CommitAsync(cancellationToken);
            await txManager.Transaction.DisposeAsync();
            txManager.Transaction = null;
        }
    }

    private async Task EnsurePostgreSqlDatabaseExistsAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            throw new InvalidOperationException("On-premises mode supports PostgreSQL only.");
        }

        // NpgsqlConnection.ConnectionString strips the password unless Persist Security Info=true,
        // so read the raw configured connection string directly from DatabaseSettings.
        var rawConnectionString = !string.IsNullOrWhiteSpace(_database.ConnectionString)
            ? _database.ConnectionString
            : dbContext.Database.GetConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(rawConnectionString);
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
        if (await existsCommand.ExecuteScalarAsync(cancellationToken) is not null)
        {
            return;
        }

        var escaped = targetDatabase.Replace("\"", "\"\"");
        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{escaped}\"", connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Created database '{Database}'.", targetDatabase);
    }
}
