using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Retailer.Host.OnPremises;

/// <summary>
/// Stores the on-premises configuration secrets (local DB connection string, per-install JWT signing key,
/// store identity) in a single file encrypted with Windows DPAPI (machine scope) and ACL-restricted to
/// SYSTEM/Administrators. Nothing secret is ever written to a plain-text appsettings file.
/// </summary>
internal static class OnPremisesSecretStore
{
    private const string FileName = "store.dat";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("RetailSuite.OnPremises.Store.v1");

    internal static string DefaultPath
    {
        get
        {
            var directory = Environment.GetEnvironmentVariable("RETAILSUITE_DATA_DIR");
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "RetailSuite");
            }

            return Path.Combine(directory, FileName);
        }
    }

    internal static void Write(string path, string json)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The on-premises secret store requires Windows (DPAPI).");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), Entropy, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(path, encrypted);
        RestrictAccess(path);
    }

    internal static string? Read(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The on-premises secret store requires Windows (DPAPI).");
        }

        var decrypted = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.LocalMachine);
        return Encoding.UTF8.GetString(decrypted);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RestrictAccess(string path)
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
    }
}
