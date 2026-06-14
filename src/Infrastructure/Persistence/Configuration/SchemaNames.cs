namespace Retailer.Infrastructure.Persistence.Configuration;

internal static class SchemaNames
{
    // TODO: figure out how to capitalize these only for Oracle
    public static string Auditing = nameof(Auditing).ToLower(); // "AUDITING";
    public static string Identity = nameof(Identity).ToLower(); // "IDENTITY";
    public static string Public = nameof(Public).ToLower(); // "IDENTITY";
}