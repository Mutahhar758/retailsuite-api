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

        // Leave InitialAdminPassword null unless explicitly supplied so ApplicationDbSeeder uses the
        // standard default password ("Hallo123$"), matching Cloud tenant provisioning.
        var adminPassword = Get(options, "admin-password", null);

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
                ["IndustryType"] = Get(options, "industry-type", "general"),
                ["BusinessSubType"] = Get(options, "business-sub-type", "general_store"),
                ["HasSupplyFeature"] = ParseBool(Get(options, "has-supply", "true"), true),
                ["HasSecondaryQty"] = ParseBool(Get(options, "has-secondary-qty", "false"), false),
                ["HasKotFeature"] = ParseBool(Get(options, "has-kot", "false"), false),
                ["HasVariablePackFeature"] = ParseBool(Get(options, "has-variable-pack", "false"), false),
                ["HasMobileShopFeature"] = ParseBool(Get(options, "has-mobile-shop", "false"), false),
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
        if (!string.IsNullOrWhiteSpace(adminPassword) && Get(options, "admin-password-file", null) is { } outFile)
        {
            File.WriteAllText(outFile, adminPassword, new UTF8Encoding(false));
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

        var activationKey = Get(options, "activation-key", null) ?? Get(options, "tenant-id", null);
        if (string.IsNullOrWhiteSpace(activationKey))
        {
            // Installed without activation details (activation happens later in the RetailSuite tray app).
            RegisterShell(installDir);
            return 0;
        }
        var cloudRelayUrl = Get(options, "cloud-relay-url", "https://localhost:5001")!;
        var verifiedTenant = VerifyLicenseWithCloud(activationKey, cloudRelayUrl);

        var initDbExe = Path.Combine(pgBinDir, "initdb.exe");
        var pgCtlExe = Path.Combine(pgBinDir, "pg_ctl.exe");
        var hasEmbeddedPostgres = File.Exists(initDbExe);

        try
        {
            var provisionArgs = new List<string>
            {
                ProvisionSwitch,
                "--force",
                "--tenant-id", verifiedTenant.TenantId,
                "--license-key", verifiedTenant.LicenseKey,
                "--store-name", verifiedTenant.StoreName,
                "--industry-type", verifiedTenant.IndustryType,
                "--business-sub-type", verifiedTenant.BusinessSubType,
                "--has-supply", verifiedTenant.HasSupplyFeature.ToString(),
                "--has-secondary-qty", verifiedTenant.HasSecondaryQty.ToString(),
                "--has-kot", verifiedTenant.HasKotFeature.ToString(),
                "--has-variable-pack", verifiedTenant.HasVariablePackFeature.ToString(),
                "--has-mobile-shop", verifiedTenant.HasMobileShopFeature.ToString(),
                "--cloud-relay-url", cloudRelayUrl,
                "--db-port", Get(options, "db-port", hasEmbeddedPostgres ? "5439" : "5432")!,
                "--db-user", Get(options, "db-user", hasEmbeddedPostgres ? "retailsuite" : "postgres")!,
                "--pg-pwfile", tempPwFile,
            };

            if (!hasEmbeddedPostgres)
            {
                provisionArgs.AddRange(new[] { "--db-password", Get(options, "db-password", "postgres")! });
            }

            if (!string.IsNullOrWhiteSpace(verifiedTenant.AdminEmail))
            {
                provisionArgs.AddRange(new[] { "--admin-email", verifiedTenant.AdminEmail });
            }

            if (Get(options, "admin-password-file", null) is { } adminPwFile)
            {
                provisionArgs.AddRange(new[] { "--admin-password-file", adminPwFile });
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

        // Allow normal (interactive) users to start/stop both services from the tray app without admin rights.
        const string serviceSddl = "D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWRPWPDTLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)";
        Exec("sc.exe", $"sdset \"{ApiServiceName}\" \"{serviceSddl}\"", ignoreExitCode: true);
        Exec("sc.exe", $"sdset \"{PgServiceName}\" \"{serviceSddl}\"", ignoreExitCode: true);

        Exec("sc.exe", $"start \"{ApiServiceName}\"", ignoreExitCode: true);
        Exec("netsh.exe", "advfirewall firewall add rule name=\"RetailSuite Local POS API\" dir=in action=allow protocol=TCP localport=5050 profile=private,domain", ignoreExitCode: true);
        RegisterShell(installDir);

        return 0;
    }

    private static void RegisterShell(string installDir)
    {
        var trayExe = Path.Combine(installDir, "RetailSuite.Tray.exe");
        if (!File.Exists(trayExe))
        {
            return;
        }

        // Start the tray manager at every user login + Start Menu / Desktop shortcuts.
        Microsoft.Win32.Registry.SetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            "RetailSuiteTray",
            $"\"{trayExe}\"");
        CreateShortcut(ShortcutPath(Environment.SpecialFolder.CommonPrograms), trayExe);
        CreateShortcut(ShortcutPath(Environment.SpecialFolder.CommonDesktopDirectory), trayExe);
    }

    private static string ShortcutPath(Environment.SpecialFolder folder) =>
        Path.Combine(Environment.GetFolderPath(folder), "RetailSuite Local Server.lnk");

    private static void CreateShortcut(string path, string target)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic link = shell.CreateShortcut(path);
            link.TargetPath = target;
            link.WorkingDirectory = Path.GetDirectoryName(target);
            link.Description = "RetailSuite Local Server manager";
            link.Save();
        }
        catch
        {
            // Shortcuts are a convenience only.
        }
    }

    private static int RunUninstall()
    {
        Exec("taskkill.exe", "/F /IM RetailSuite.Tray.exe", ignoreExitCode: true);
        Microsoft.Win32.Registry.LocalMachine
            .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true)?
            .DeleteValue("RetailSuiteTray", throwOnMissingValue: false);
        foreach (var folder in new[] { Environment.SpecialFolder.CommonPrograms, Environment.SpecialFolder.CommonDesktopDirectory })
        {
            var lnk = ShortcutPath(folder);
            if (File.Exists(lnk))
            {
                File.Delete(lnk);
            }
        }

        Exec("sc.exe", $"stop \"{ApiServiceName}\"", ignoreExitCode: true);
        Exec("sc.exe", $"delete \"{ApiServiceName}\"", ignoreExitCode: true);
        Exec("sc.exe", $"stop \"{PgServiceName}\"", ignoreExitCode: true);
        Exec("sc.exe", $"delete \"{PgServiceName}\"", ignoreExitCode: true);
        Exec("netsh.exe", "advfirewall firewall delete rule name=\"RetailSuite Local POS API\"", ignoreExitCode: true);
        return 0;
    }

    private static VerifiedTenantPayload VerifyLicenseWithCloud(string licenseKey, string cloudRelayUrl)
    {
        var trimmedKey = licenseKey.Trim();
        var baseUri = cloudRelayUrl.TrimEnd('/');
        var url = $"{baseUri}/api/license/verify/{Uri.EscapeDataString(trimmedKey)}";

        using var handler = new HttpClientHandler();
        if (Uri.TryCreate(baseUri, UriKind.Absolute, out var parsedUri)
            && (parsedUri.IsLoopback || string.Equals(parsedUri.Host, "localhost", StringComparison.OrdinalIgnoreCase)))
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var response = http.GetAsync(url).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException(
                $"License verification failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
        }

        var json = response.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult();
        if (!(json.TryGetProperty("body", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
            && !(json.TryGetProperty("data", out dataProp) && dataProp.ValueKind == JsonValueKind.Object))
        {
            throw new InvalidOperationException("License verification returned an unexpected response from the Cloud server.");
        }

        string ReadString(string name, string fallback = "") =>
            dataProp.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString() ?? fallback
                : fallback;

        bool ReadBool(string name, bool fallback = false) =>
            dataProp.TryGetProperty(name, out var p) && (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False)
                ? p.GetBoolean()
                : fallback;

        var tenantId = ReadString("identifier");
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            tenantId = ReadString("id");
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("Cloud license verification did not return a valid Tenant ID.");
        }

        return new VerifiedTenantPayload(
            TenantId: tenantId,
            StoreName: ReadString("name", tenantId),
            AdminEmail: ReadString("adminEmail"),
            LicenseKey: ReadString("licenseKey", trimmedKey),
            IndustryType: ReadString("industryType", "general"),
            BusinessSubType: ReadString("businessSubType", "general_store"),
            HasSupplyFeature: ReadBool("hasSupplyFeature", true),
            HasSecondaryQty: ReadBool("hasSecondaryQty", false),
            HasKotFeature: ReadBool("hasKotFeature", false),
            HasVariablePackFeature: ReadBool("hasVariablePackFeature", false),
            HasMobileShopFeature: ReadBool("hasMobileShopFeature", false));
    }

    private static bool ParseBool(string? value, bool fallback) =>
        bool.TryParse(value, out var parsed) ? parsed : fallback;

    private sealed record VerifiedTenantPayload(
        string TenantId,
        string StoreName,
        string AdminEmail,
        string LicenseKey,
        string IndustryType,
        string BusinessSubType,
        bool HasSupplyFeature,
        bool HasSecondaryQty,
        bool HasKotFeature,
        bool HasVariablePackFeature,
        bool HasMobileShopFeature);

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
