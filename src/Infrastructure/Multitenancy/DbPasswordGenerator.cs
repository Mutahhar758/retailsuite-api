using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Retailer.Infrastructure.Multitenancy;

internal static class DbPasswordGenerator
{
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%*_-+";

    // Modern .NET built-in one-liner for cryptographically secure random string
    public static string Generate(int length = 24) =>
        RandomNumberGenerator.GetString(AllowedChars, length);

    public static string GenerateDbUsername(string tenantIdentifier)
    {
        string cleaned = Regex.Replace((tenantIdentifier ?? "tenant").Trim().ToLowerInvariant(), @"[^a-z0-9_]", "_");
        return $"usr_{(cleaned.Length > 40 ? cleaned[..40] : cleaned)}";
    }

    public static string SanitizeDbUsername(string input)
    {
        string cleaned = Regex.Replace((input ?? string.Empty).Trim(), @"[^a-zA-Z0-9_]", "_");
        if (string.IsNullOrWhiteSpace(cleaned)) return "usr_tenant";
        return char.IsDigit(cleaned[0]) ? $"u_{cleaned}" : cleaned;
    }
}
