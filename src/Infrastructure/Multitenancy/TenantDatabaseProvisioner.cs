using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Retailer.Application.Common.Exceptions;
using Retailer.Infrastructure.Common;
using Retailer.Infrastructure.Persistence;

namespace Retailer.Infrastructure.Multitenancy;

internal class TenantDatabaseProvisioner
{
    private readonly DatabaseSettings _dbSettings;
    private readonly MultitenancySettings _multitenancySettings;
    private readonly ILogger<TenantDatabaseProvisioner> _logger;

    public TenantDatabaseProvisioner(
        IOptions<DatabaseSettings> dbSettings,
        IOptions<MultitenancySettings> multitenancySettings,
        ILogger<TenantDatabaseProvisioner> logger)
    {
        _dbSettings = dbSettings.Value;
        _multitenancySettings = multitenancySettings.Value;
        _logger = logger;
    }

    public string GetAdminConnectionString(string dbProvider)
    {
        string providerLower = dbProvider.ToLowerInvariant();
        string adminConn = providerLower switch
        {
            DbProviderKeys.Npgsql or "npgsql" or "postgres" or "postgresql" =>
                !string.IsNullOrWhiteSpace(_multitenancySettings.DefaultConnectionString)
                    ? _multitenancySettings.DefaultConnectionString
                    : _dbSettings.ConnectionString,
            DbProviderKeys.SqlServer or "sqlserver" or "mssql" =>
                !string.IsNullOrWhiteSpace(_dbSettings.ConnectionString)
                    ? _dbSettings.ConnectionString
                    : _multitenancySettings.DefaultConnectionString,
            _ => throw new BadRequestException($"Unsupported DB Provider: {dbProvider}")
        };

        if (string.IsNullOrWhiteSpace(adminConn))
        {
            throw new BadRequestException($"No administrative connection string found for provider '{dbProvider}'.");
        }

        return adminConn;
    }

    public bool IsDedicatedTenantUser(string dbProvider, string dbUser)
    {
        if (string.IsNullOrWhiteSpace(dbUser))
        {
            return false;
        }

        string providerLower = dbProvider.ToLowerInvariant();
        string adminConn = GetAdminConnectionString(dbProvider);

        if (providerLower is DbProviderKeys.Npgsql or "npgsql" or "postgres" or "postgresql")
        {
            try
            {
                var adminBuilder = new NpgsqlConnectionStringBuilder(adminConn);
                string adminUser = adminBuilder.Username ?? string.Empty;
                if (string.Equals(dbUser, adminUser, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dbUser, "postgres", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch { }
        }
        else if (providerLower is DbProviderKeys.SqlServer or "sqlserver" or "mssql")
        {
            try
            {
                var adminBuilder = new SqlConnectionStringBuilder(adminConn);
                string adminUser = adminBuilder.UserID ?? string.Empty;
                if (string.Equals(dbUser, adminUser, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dbUser, "sa", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch { }
        }

        return dbUser.StartsWith("usr_", StringComparison.OrdinalIgnoreCase);
    }

    public string BuildConnectionString(string dbProvider, string targetDatabase, string dbUser, string dbPassword)
    {
        string adminConn = GetAdminConnectionString(dbProvider);
        string providerLower = dbProvider.ToLowerInvariant();

        if (providerLower is DbProviderKeys.Npgsql or "npgsql" or "postgres" or "postgresql")
        {
            var builder = new NpgsqlConnectionStringBuilder(adminConn)
            {
                Database = targetDatabase.ToLowerInvariant(),
                Username = dbUser,
                Password = dbPassword
            };
            return builder.ConnectionString;
        }
        else if (providerLower is DbProviderKeys.SqlServer or "sqlserver" or "mssql")
        {
            var builder = new SqlConnectionStringBuilder(adminConn)
            {
                InitialCatalog = targetDatabase,
                UserID = dbUser,
                Password = dbPassword,
                IntegratedSecurity = false
            };
            return builder.ConnectionString;
        }

        throw new BadRequestException($"Unsupported DB Provider: {dbProvider}");
    }

    public async Task EnsureDatabaseAndUserProvisionedAsync(
        string dbProvider,
        string targetDatabase,
        string dbUser,
        string dbPassword,
        CancellationToken cancellationToken)
    {
        string providerLower = dbProvider.ToLowerInvariant();
        if (providerLower is DbProviderKeys.Npgsql or "npgsql" or "postgres" or "postgresql")
        {
            await ProvisionPostgreSqlAsync(targetDatabase, dbUser, dbPassword, cancellationToken);
        }
        else if (providerLower is DbProviderKeys.SqlServer or "sqlserver" or "mssql")
        {
            await ProvisionSqlServerAsync(targetDatabase, dbUser, dbPassword, cancellationToken);
        }
        else
        {
            throw new BadRequestException($"Unsupported DB Provider: {dbProvider}");
        }
    }

    private async Task ProvisionPostgreSqlAsync(
        string targetDatabase,
        string dbUser,
        string dbPassword,
        CancellationToken cancellationToken)
    {
        string adminConn = GetAdminConnectionString("postgresql");
        var adminBuilder = new NpgsqlConnectionStringBuilder(adminConn)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. Ensure target database exists
        await using var existsCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        existsCmd.Parameters.AddWithValue("name", targetDatabase);
        var exists = await existsCmd.ExecuteScalarAsync(cancellationToken) != null;
        if (!exists)
        {
            var escapedDb = targetDatabase.Replace("\"", "\"\"");
            await using var createDbCmd = new NpgsqlCommand($"CREATE DATABASE \"{escapedDb}\"", connection);
            await createDbCmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created PostgreSQL database '{Database}'.", targetDatabase);
        }

        // If the user being provisioned is the administrative account, skip user creation
        string adminUser = adminBuilder.Username ?? string.Empty;
        if (string.Equals(dbUser, adminUser, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dbUser, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Database user '{User}' is the administrative user; skipping role creation for '{Database}'.", dbUser, targetDatabase);
            return;
        }

        // 2. Ensure DB user exists with password
        var escapedUser = dbUser.Replace("\"", "\"\"");
        var escapedPassword = dbPassword.Replace("'", "''");

        string userScript = $@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{escapedUser}') THEN
    EXECUTE 'CREATE USER ""{escapedUser}"" WITH PASSWORD ''{escapedPassword}''';
  ELSE
    EXECUTE 'ALTER USER ""{escapedUser}"" WITH PASSWORD ''{escapedPassword}''';
  END IF;
END
$$;";

        await using var userCmd = new NpgsqlCommand(userScript, connection);
        await userCmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Provisioned PostgreSQL user '{User}'.", dbUser);

        // 3. Make user owner of database and grant all privileges
        var escapedDbName = targetDatabase.Replace("\"", "\"\"");
        string ownershipScript = $@"
ALTER DATABASE ""{escapedDbName}"" OWNER TO ""{escapedUser}"";
GRANT ALL PRIVILEGES ON DATABASE ""{escapedDbName}"" TO ""{escapedUser}"";";

        await using var ownerCmd = new NpgsqlCommand(ownershipScript, connection);
        await ownerCmd.ExecuteNonQueryAsync(cancellationToken);

        // 4. Connect directly to target database as admin to grant schema privileges
        var targetAdminBuilder = new NpgsqlConnectionStringBuilder(adminConn)
        {
            Database = targetDatabase
        };

        await using var targetConnection = new NpgsqlConnection(targetAdminBuilder.ConnectionString);
        await targetConnection.OpenAsync(cancellationToken);

        string schemaScript = $@"
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS auditing;

DO $$
DECLARE
    s text;
BEGIN
    FOR s IN SELECT nspname FROM pg_namespace WHERE nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast') AND nspname NOT LIKE 'pg_temp%' AND nspname NOT LIKE 'pg_toast_temp%'
    LOOP
        EXECUTE format('GRANT ALL ON SCHEMA %I TO ""{escapedUser}""', s);
        EXECUTE format('GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I TO ""{escapedUser}""', s);
        EXECUTE format('GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I TO ""{escapedUser}""', s);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON TABLES TO ""{escapedUser}""', s);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON SEQUENCES TO ""{escapedUser}""', s);
    END LOOP;
END
$$;";

        await using var schemaCmd = new NpgsqlCommand(schemaScript, targetConnection);
        try
        {
            await schemaCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Schema permission assignment notice for tenant database '{Database}'.", targetDatabase);
        }
    }

    private async Task ProvisionSqlServerAsync(
        string targetDatabase,
        string dbUser,
        string dbPassword,
        CancellationToken cancellationToken)
    {
        string adminConn = GetAdminConnectionString("mssql");
        var adminBuilder = new SqlConnectionStringBuilder(adminConn)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. Ensure target database exists
        await using var existsCmd = new SqlCommand("SELECT 1 FROM sys.databases WHERE name = @name", connection);
        existsCmd.Parameters.AddWithValue("@name", targetDatabase);
        var exists = await existsCmd.ExecuteScalarAsync(cancellationToken) != null;
        if (!exists)
        {
            var escapedDb = targetDatabase.Replace("]", "]]");
            await using var createDbCmd = new SqlCommand($"CREATE DATABASE [{escapedDb}]", connection);
            await createDbCmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created SQL Server database '{Database}'.", targetDatabase);
        }

        // If the user being provisioned is the administrative account, skip user creation
        string adminUser = adminBuilder.UserID ?? string.Empty;
        if (string.Equals(dbUser, adminUser, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dbUser, "sa", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Database user '{User}' is the administrative login; skipping SQL Server user mapping for '{Database}'.", dbUser, targetDatabase);
            return;
        }

        // 2. Ensure server login exists with password
        var escapedLogin = dbUser.Replace("]", "]]");
        var escapedPassword = dbPassword.Replace("'", "''");

        string loginScript = $@"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{escapedLogin}')
BEGIN
    EXEC('CREATE LOGIN [{escapedLogin}] WITH PASSWORD = ''{escapedPassword}'', CHECK_POLICY = OFF');
END
ELSE
BEGIN
    EXEC('ALTER LOGIN [{escapedLogin}] WITH PASSWORD = ''{escapedPassword}''');
END";

        await using var loginCmd = new SqlCommand(loginScript, connection);
        await loginCmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Provisioned SQL Server login '{Login}'.", dbUser);

        // 3. Connect to target tenant database as admin to map user and assign db_owner
        var targetAdminBuilder = new SqlConnectionStringBuilder(adminConn)
        {
            InitialCatalog = targetDatabase
        };

        await using var targetConnection = new SqlConnection(targetAdminBuilder.ConnectionString);
        await targetConnection.OpenAsync(cancellationToken);

        var escapedUser = dbUser.Replace("]", "]]");
        string userRoleScript = $@"
DECLARE @existingUser sysname;
SELECT @existingUser = dp.name
FROM sys.database_principals dp
JOIN sys.server_principals sp ON dp.sid = sp.sid
WHERE sp.name = '{escapedLogin}';

IF @existingUser IS NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{escapedUser}')
    BEGIN
        EXEC('CREATE USER [{escapedUser}] FOR LOGIN [{escapedLogin}]');
    END;
    IF IS_ROLEMEMBER('db_owner', '{escapedUser}') = 0 OR IS_ROLEMEMBER('db_owner', '{escapedUser}') IS NULL
    BEGIN
        ALTER ROLE db_owner ADD MEMBER [{escapedUser}];
    END;
END
ELSE IF @existingUser <> 'dbo'
BEGIN
    IF IS_ROLEMEMBER('db_owner', @existingUser) = 0 OR IS_ROLEMEMBER('db_owner', @existingUser) IS NULL
    BEGIN
        EXEC('ALTER ROLE db_owner ADD MEMBER [' + @existingUser + ']');
    END;
END;";

        await using var roleCmd = new SqlCommand(userRoleScript, targetConnection);
        await roleCmd.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Mapped SQL Server user '{User}' to db_owner in '{Database}'.", dbUser, targetDatabase);
    }

    public async Task TestConnectivityAsync(string dbProvider, string connectionString, CancellationToken cancellationToken)
    {
        string providerLower = dbProvider.ToLowerInvariant();
        if (providerLower is DbProviderKeys.Npgsql or "npgsql" or "postgres" or "postgresql")
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);
        }
        else if (providerLower is DbProviderKeys.SqlServer or "sqlserver" or "mssql")
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);
        }
        else
        {
            throw new BadRequestException($"Unsupported DB Provider: {dbProvider}");
        }
    }
}
