using System.Collections.ObjectModel;

namespace Retailer.Shared.Authorization;

public static class AppRoles
{
    public const string Admin = nameof(Admin);
    public const string Basic = nameof(Basic);
    public const string Cashier = nameof(Cashier);
    public const string InventoryManager = "Inventory Manager";
    public const string Accountant = nameof(Accountant);
    public const string PayrollManager = "Payroll Manager";

    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Basic,
        Cashier,
        InventoryManager,
        Accountant,
        PayrollManager
    });

    public static bool IsDefault(string roleName) => DefaultRoles.Any(r => r == roleName);
}