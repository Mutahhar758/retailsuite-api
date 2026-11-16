using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Retailer.Host.OnPremises;

/// <summary>
/// One-time install-time provisioning and Windows Service setup invoked by the Visual Studio Setup Project
/// (<c>RetailSuite.Setup.vdproj</c>) Custom Actions or from the command line:
/// <list type="bullet">
/// <item><c>Retailer.Host.exe --onprem-provision --tenant-id store-102 ...</c> (generates DPAPI secrets &amp; unique 64-byte JWT key)</item>
/// <item><c>Retailer.Host.exe --onprem-install --activation-key "..." --store-name "..." --admin-email "..." --cloud-relay-url "..."</c>
/// (provisions portable PostgreSQL on port 5439, writes DPAPI store.dat, and registers/starts Windows Services)</item>
/// <item><c>Retailer.Host.exe --onprem-uninstall</c> (stops and deletes RetailSuiteLocalServer &amp; RetailSuitePostgres services)</item>
/// </list>
/// </summary>
internal static class OnPremisesProvisioner
{
    internal const string ProvisionSwitch = "--onprem-provision";
    internal const string InstallSwitch = "--onprem-install";
    internal const string UninstallSwitch = "--onprem-uninstall";

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const string PgServiceName = "RetailSuitePostgres";
    private const string ApiServiceName = "RetailSuiteLocalServer";

    internal static bool IsCliCommand(string[] args) =>
        args.Contains(ProvisionSwitch, StringComparer.OrdinalIgnoreCase)
        || args.Contains(InstallSwitch, StringComparer.OrdinalIgnoreCase)
        || args.Contains(UninstallSwitch, StringComparer.OrdinalIgnoreCase);

    internal static int Run(string[] args)
    {
        if (args.Contains(UninstallSwitch, StringComparer.OrdinalIgnoreCase))
        {
            return RunUninstall();
        }

        if (args.Contains(InstallSwitch, StringComparer.OrdinalIgnoreCase))
        {
            return RunFullInstall(args);
        }

        return RunProvision(args);
    }

    private static int RunProvision(string[] args)
    {
        var options = ParseOptions(args);

        var tenantId = Require(options, "tenant-id");
        var path = OnPremisesSecretStore.DefaultPath;

        if (File.Exists(path) && !options.ContainsKey("force"))
        {
            Console.Error.WriteLine($"Already provisioned ({path}). Use --force to re-provision (this rotates all secrets).");
            return 2;
        }

        var dbHost = Get(options, "db-host", "127.0.0.1");
        var dbPort = Get(options, "db-port", "5439");
        var dbName = Get(options, "db-name", "RetailSuite_Local");
        var dbUser = Get(options, "db-user", "retailsuite");
        var dbPassword = Get(options, "db-password", null) ?? RandomPassword(64);

        var adminPasswordGenerated = !options.ContainsKey("admin-password");
        var adminPassword = Get(options, "admin-password", null) ?? RandomPassword(16);

        // Unique 512-bit (64-byte) JWT signing key per installation. Never shared between stores or with the cloud.
        var jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var payload = new Dictionary<string, object?>
        {
            ["HostingMode"] = "OnPremises",
            ["urls"] = Get(options, "urls", "http://0.0.0.0:5050"),
            ["StoreSettings"] = new Dictionary<string, object?>
            {
                ["TenantId"] = tenantId,
                ["StoreName"] = Get(options, "store-name", tenantId),
                ["AdminEmail"] = Get(options, "admin-email", null),
                ["InitialAdminPassword"] = adminPassword,
                ["LicenseKey"] = Get(options, "license-key", null),
                ["CloudRelayUrl"] = Get(options, "cloud-relay-url", null),
                ["LocalLoopbackUrl"] = Get(options, "loopback-url", "http://127.0.0.1:5050"),
            },
            ["DatabaseSettings"] = new Dictionary<string, object?>
            {
                ["DBProvider"] = "postgresql",
                ["ConnectionString"] =
                    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};Include Error Detail=false",
            },
            ["SecuritySettings"] = new Dictionary<string, object?>
            {
                ["JwtSettings"] = new Dictionary<string, object?> { ["Key"] = jwtKey },
            },
        };

        OnPremisesSecretStore.Write(path, JsonSerializer.Serialize(payload));

        // Hand the generated DB password to initdb via a one-shot file deleted immediately afterwards.
        if (options.TryGetValue("pg-pwfile", out var pwFile) && !string.IsNullOrWhiteSpace(pwFile))
        {
            File.WriteAllText(pwFile, dbPassword, new UTF8Encoding(false));
        }

        Console.WriteLine($"Provisioned store '{tenantId}'. Encrypted configuration written to {path}");
        if (adminPasswordGenerated)
        {
            Console.WriteLine($"INITIAL_ADMIN_PASSWORD={adminPassword}");
        }

        return 0;
    }

    private static int RunFullInstall(string[] args)
    {
        var options = ParseOptions(args);
        var installDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var dataDir = Path.GetDirectoryName(OnPremisesSecretStore.DefaultPath)!;
        var pgDataDir = Path.Combine(dataDir, "pgdata");
        var pgBinDir = Path.Combine(installDir, "pgsql", "bin");
        var tempPwFile = Path.Combine(dataDir, "pg_init.pw");

        Directory.CreateDirectory(dataDir);

        var activationKey = Get(options, "activation-key", null)
            ?? Get(options, "tenant-id", "local-store")!;
        var cloudRelayUrl = Get(options, "cloud-relay-url", null);
        var tenantId = ResolveTenantIdFromCloudOrFallback(activationKey, cloudRelayUrl);

        var initDbExe = Path.Combine(pgBinDir, "initdb.exe");
        var pgCtlExe = Path.Combine(pgBinDir, "pg_ctl.exe");
        var hasEmbeddedPostgres = File.Exists(initDbExe);

        try
        {
            var provisionArgs = new List<string>
            {
                ProvisionSwitch,
                "--tenant-id", tenantId,
                "--license-key", activationKey,
                "--store-name", Get(options, "store-name", tenantId)!,
                "--db-port", Get(options, "db-port", hasEmbeddedPostgres ? "5439" : "5432")!,
                "--db-user", Get(options, "db-user", hasEmbeddedPostgres ? "retailsuite" : "postgres")!,
                "--pg-pwfile", tempPwFile,
            };

            if (!hasEmbeddedPostgres)
            {
                provisionArgs.AddRange(new[] { "--db-password", Get(options, "db-password", "postgres")! });
            }

            if (Get(options, "admin-email", null) is { } adminEmail)
            {
                provisionArgs.AddRange(new[] { "--admin-email", adminEmail });
            }

            if (!string.IsNullOrWhiteSpace(cloudRelayUrl))
            {
                provisionArgs.AddRange(new[] { "--cloud-relay-url", cloudRelayUrl });
            }

            var rc = RunProvision(provisionArgs.ToArray());
            if (rc is not (0 or 2))
            {
                return rc;
            }

            // Initialize portable PostgreSQL cluster on 127.0.0.1:5439 if bundled
            if (hasEmbeddedPostgres && !File.Exists(Path.Combine(pgDataDir, "PG_VERSION")))
            {
                Exec(initDbExe, $"-D \"{pgDataDir}\" -U retailsuite --pwfile=\"{tempPwFile}\" -E UTF8 -A scram-sha-256");
                File.AppendAllText(
                    Path.Combine(pgDataDir, "postgresql.conf"),
                    "\nlisten_addresses = '127.0.0.1'\nport = 5439\n");
            }

            if (File.Exists(pgCtlExe))
            {
                Exec(pgCtlExe, $"register -N \"{PgServiceName}\" -D \"{pgDataDir}\" -S auto", ignoreExitCode: true);
                Exec("sc.exe", $"start \"{PgServiceName}\"", ignoreExitCode: true);
            }
        }
        finally
        {
            if (File.Exists(tempPwFile))
            {
                File.Delete(tempPwFile);
            }
        }

        // Register & start RetailSuiteLocalServer Windows Service
        var hostExe = Path.Combine(installDir, "Retailer.Host.exe");
        Exec("sc.exe", $"create \"{ApiServiceName}\" binPath= \"\\\"{hostExe}\\\"\" start= auto DisplayName= \"RetailSuite Local Server\"", ignoreExitCode: true);
        Exec("sc.exe", $"failure \"{ApiServiceName}\" reset= 86400 actions= restart/5000/restart/10000/restart/30000", ignoreExitCode: true);
        Exec("sc.exe", $"start \"{ApiServiceName}\"", ignoreExitCode: true);
        Exec("netsh.exe", "advfirewall firewall add rule name=\"RetailSuite Local POS API\" dir=in action=allow protocol=TCP localport=5050 profile=private,domain", ignoreExitCode: true);

        return 0;
    }

    private static int RunUninstall()
    {
        Exec("sc.exe", $"stop \"{ApiServiceName}\"", ignoreExitCode: true);
        Exec("sc.exe", $"delete \"{ApiServiceName}\"", ignoreExitCode: true);
        Exec("sc.exe", $"stop \"{PgServiceName}\"", ignoreExitCode: true);
        Exec("sc.exe", $"delete \"{PgServiceName}\"", ignoreExitCode: true);
        Exec("netsh.exe", "advfirewall firewall delete rule name=\"RetailSuite Local POS API\"", ignoreExitCode: true);
        return 0;
    }

    private static string ResolveTenantIdFromCloudOrFallback(string activationKey, string? cloudRelayUrl)
    {
        if (string.IsNullOrWhiteSpace(cloudRelayUrl))
        {
            return activationKey.Trim();
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var url = $"{cloudRelayUrl.TrimEnd('/')}/api/license/verify/{Uri.EscapeDataString(activationKey.Trim())}";
            var json = http.GetFromJsonAsync<JsonElement>(url).GetAwaiter().GetResult();
            if (json.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.String)
            {
                var resolved = dataProp.GetString();
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    return resolved;
                }
            }
        }
        catch
        {
            // Offline during install: fall back to activationKey as tenant identifier
        }

        return activationKey.Trim();
    }

    private static void Exec(string fileName, string arguments, bool ignoreExitCode = false)
    {
        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        proc?.WaitForExit();
        if (!ignoreExitCode && proc is { ExitCode: not 0 })
        {
            throw new InvalidOperationException($"Command '{fileName}' failed with exit code {proc.ExitCode}.");
        }
    }

    private static string RandomPassword(int length) =>
        string.Create(length, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }
        });

    private static Dictionary<string, string?> ParseOptions(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var flag = args[i];
            if (string.Equals(flag, ProvisionSwitch, StringComparison.OrdinalIgnoreCase)
                || string.Equals(flag, InstallSwitch, StringComparison.OrdinalIgnoreCase)
                || string.Equals(flag, UninstallSwitch, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var key = flag[2..];
            var eqIdx = key.IndexOf('=');
            if (eqIdx > 0)
            {
                result[key[..eqIdx]] = key[(eqIdx + 1)..].Trim('"');
                continue;
            }

            var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
            result[key] = hasValue ? args[++i].Trim('"') : null;
        }

        return result;
    }

    private static string Require(Dictionary<string, string?> options, string key) =>
        options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required argument --{key}");

    private static string? Get(Dictionary<string, string?> options, string key, string? fallback) =>
        options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}
