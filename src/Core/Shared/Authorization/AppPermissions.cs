using System.Collections.ObjectModel;

namespace Retailer.Shared.Authorization;

public static class AppAction
{
    public const string View = nameof(View);
    public const string Search = nameof(Search);
    public const string Create = nameof(Create);
    public const string Update = nameof(Update);
    public const string Delete = nameof(Delete);
    public const string Export = nameof(Export);
    public const string Print = nameof(Print);
}

public static class AppResource
{
    public const string Users = nameof(Users);
    public const string Roles = nameof(Roles);
    public const string Dashboard = nameof(Dashboard);
    public const string Reports = nameof(Reports);
    public const string PrinterSettings = nameof(PrinterSettings);
    public const string ChartOfAccounts = nameof(ChartOfAccounts);
    public const string DetailAccounts = nameof(DetailAccounts);
    public const string Customers = nameof(Customers);
    public const string Vendors = nameof(Vendors);
    public const string InventoryItems = nameof(InventoryItems);
    public const string ItemCategories = nameof(ItemCategories);
    public const string Units = nameof(Units);
    public const string Narrations = nameof(Narrations);
    public const string HRInfo = nameof(HRInfo);
    public const string SupplyOrders = nameof(SupplyOrders);
    public const string OpeningBalances = nameof(OpeningBalances);
    public const string PaymentVouchers = nameof(PaymentVouchers);
    public const string ReceiptVouchers = nameof(ReceiptVouchers);
    public const string JournalVouchers = nameof(JournalVouchers);
    public const string Purchases = nameof(Purchases);
    public const string Sales = nameof(Sales);
    public const string POSSales = nameof(POSSales);
    public const string SaleSupplies = nameof(SaleSupplies);
    public const string PurchaseReturns = nameof(PurchaseReturns);
    public const string SaleReturns = nameof(SaleReturns);
    public const string StockAdjustments = nameof(StockAdjustments);
    public const string BankReconciliations = nameof(BankReconciliations);
    public const string Payrolls = nameof(Payrolls);
}

public static class AppPermissions
{
    private static readonly AppPermission[] _all = new AppPermission[]
    {
        // Users
        new("View Users", AppAction.View, AppResource.Users),
        new("Search Users", AppAction.Search, AppResource.Users),
        new("Create Users", AppAction.Create, AppResource.Users),
        new("Update Users", AppAction.Update, AppResource.Users),
        new("Delete Users", AppAction.Delete, AppResource.Users),

        // Roles
        new("View Roles", AppAction.View, AppResource.Roles),
        new("Search Roles", AppAction.Search, AppResource.Roles),
        new("Create Roles", AppAction.Create, AppResource.Roles),
        new("Update Roles", AppAction.Update, AppResource.Roles),
        new("Delete Roles", AppAction.Delete, AppResource.Roles),

        // Dashboard
        new("View Dashboard", AppAction.View, AppResource.Dashboard, IsBasic: true),

        // Reports
        new("View Reports", AppAction.View, AppResource.Reports),
        new("Export Reports", AppAction.Export, AppResource.Reports),

        // PrinterSettings
        new("View PrinterSettings", AppAction.View, AppResource.PrinterSettings),
        new("Update PrinterSettings", AppAction.Update, AppResource.PrinterSettings),

        // ChartOfAccounts
        new("View ChartOfAccounts", AppAction.View, AppResource.ChartOfAccounts),
        new("Search ChartOfAccounts", AppAction.Search, AppResource.ChartOfAccounts),
        new("Create ChartOfAccounts", AppAction.Create, AppResource.ChartOfAccounts),
        new("Update ChartOfAccounts", AppAction.Update, AppResource.ChartOfAccounts),
        new("Delete ChartOfAccounts", AppAction.Delete, AppResource.ChartOfAccounts),
        new("Export ChartOfAccounts", AppAction.Export, AppResource.ChartOfAccounts),

        // DetailAccounts
        new("View DetailAccounts", AppAction.View, AppResource.DetailAccounts),
        new("Search DetailAccounts", AppAction.Search, AppResource.DetailAccounts),
        new("Create DetailAccounts", AppAction.Create, AppResource.DetailAccounts),
        new("Update DetailAccounts", AppAction.Update, AppResource.DetailAccounts),
        new("Delete DetailAccounts", AppAction.Delete, AppResource.DetailAccounts),
        new("Export DetailAccounts", AppAction.Export, AppResource.DetailAccounts),

        // Customers
        new("View Customers", AppAction.View, AppResource.Customers),
        new("Search Customers", AppAction.Search, AppResource.Customers),
        new("Create Customers", AppAction.Create, AppResource.Customers),
        new("Update Customers", AppAction.Update, AppResource.Customers),
        new("Delete Customers", AppAction.Delete, AppResource.Customers),
        new("Export Customers", AppAction.Export, AppResource.Customers),

        // Vendors
        new("View Vendors", AppAction.View, AppResource.Vendors),
        new("Search Vendors", AppAction.Search, AppResource.Vendors),
        new("Create Vendors", AppAction.Create, AppResource.Vendors),
        new("Update Vendors", AppAction.Update, AppResource.Vendors),
        new("Delete Vendors", AppAction.Delete, AppResource.Vendors),
        new("Export Vendors", AppAction.Export, AppResource.Vendors),

        // InventoryItems
        new("View InventoryItems", AppAction.View, AppResource.InventoryItems),
        new("Search InventoryItems", AppAction.Search, AppResource.InventoryItems),
        new("Create InventoryItems", AppAction.Create, AppResource.InventoryItems),
        new("Update InventoryItems", AppAction.Update, AppResource.InventoryItems),
        new("Delete InventoryItems", AppAction.Delete, AppResource.InventoryItems),
        new("Export InventoryItems", AppAction.Export, AppResource.InventoryItems),

        // ItemCategories
        new("View ItemCategories", AppAction.View, AppResource.ItemCategories),
        new("Search ItemCategories", AppAction.Search, AppResource.ItemCategories),
        new("Create ItemCategories", AppAction.Create, AppResource.ItemCategories),
        new("Update ItemCategories", AppAction.Update, AppResource.ItemCategories),
        new("Delete ItemCategories", AppAction.Delete, AppResource.ItemCategories),

        // Units
        new("View Units", AppAction.View, AppResource.Units),
        new("Search Units", AppAction.Search, AppResource.Units),
        new("Create Units", AppAction.Create, AppResource.Units),
        new("Update Units", AppAction.Update, AppResource.Units),
        new("Delete Units", AppAction.Delete, AppResource.Units),

        // Narrations
        new("View Narrations", AppAction.View, AppResource.Narrations),
        new("Search Narrations", AppAction.Search, AppResource.Narrations),
        new("Create Narrations", AppAction.Create, AppResource.Narrations),
        new("Update Narrations", AppAction.Update, AppResource.Narrations),
        new("Delete Narrations", AppAction.Delete, AppResource.Narrations),

        // HRInfo
        new("View HRInfo", AppAction.View, AppResource.HRInfo),
        new("Search HRInfo", AppAction.Search, AppResource.HRInfo),
        new("Create HRInfo", AppAction.Create, AppResource.HRInfo),
        new("Update HRInfo", AppAction.Update, AppResource.HRInfo),
        new("Delete HRInfo", AppAction.Delete, AppResource.HRInfo),
        new("Export HRInfo", AppAction.Export, AppResource.HRInfo),

        // SupplyOrders
        new("View SupplyOrders", AppAction.View, AppResource.SupplyOrders),
        new("Search SupplyOrders", AppAction.Search, AppResource.SupplyOrders),
        new("Create SupplyOrders", AppAction.Create, AppResource.SupplyOrders),
        new("Update SupplyOrders", AppAction.Update, AppResource.SupplyOrders),
        new("Delete SupplyOrders", AppAction.Delete, AppResource.SupplyOrders),
        new("Export SupplyOrders", AppAction.Export, AppResource.SupplyOrders),

        // OpeningBalances
        new("View OpeningBalances", AppAction.View, AppResource.OpeningBalances),
        new("Update OpeningBalances", AppAction.Update, AppResource.OpeningBalances),

        // PaymentVouchers
        new("View PaymentVouchers", AppAction.View, AppResource.PaymentVouchers),
        new("Search PaymentVouchers", AppAction.Search, AppResource.PaymentVouchers),
        new("Create PaymentVouchers", AppAction.Create, AppResource.PaymentVouchers),
        new("Update PaymentVouchers", AppAction.Update, AppResource.PaymentVouchers),
        new("Delete PaymentVouchers", AppAction.Delete, AppResource.PaymentVouchers),
        new("Export PaymentVouchers", AppAction.Export, AppResource.PaymentVouchers),

        // ReceiptVouchers
        new("View ReceiptVouchers", AppAction.View, AppResource.ReceiptVouchers),
        new("Search ReceiptVouchers", AppAction.Search, AppResource.ReceiptVouchers),
        new("Create ReceiptVouchers", AppAction.Create, AppResource.ReceiptVouchers),
        new("Update ReceiptVouchers", AppAction.Update, AppResource.ReceiptVouchers),
        new("Delete ReceiptVouchers", AppAction.Delete, AppResource.ReceiptVouchers),
        new("Export ReceiptVouchers", AppAction.Export, AppResource.ReceiptVouchers),

        // JournalVouchers
        new("View JournalVouchers", AppAction.View, AppResource.JournalVouchers),
        new("Search JournalVouchers", AppAction.Search, AppResource.JournalVouchers),
        new("Create JournalVouchers", AppAction.Create, AppResource.JournalVouchers),
        new("Update JournalVouchers", AppAction.Update, AppResource.JournalVouchers),
        new("Delete JournalVouchers", AppAction.Delete, AppResource.JournalVouchers),
        new("Export JournalVouchers", AppAction.Export, AppResource.JournalVouchers),

        // Purchases
        new("View Purchases", AppAction.View, AppResource.Purchases),
        new("Search Purchases", AppAction.Search, AppResource.Purchases),
        new("Create Purchases", AppAction.Create, AppResource.Purchases),
        new("Update Purchases", AppAction.Update, AppResource.Purchases),
        new("Delete Purchases", AppAction.Delete, AppResource.Purchases),
        new("Export Purchases", AppAction.Export, AppResource.Purchases),

        // Sales
        new("View Sales", AppAction.View, AppResource.Sales),
        new("Search Sales", AppAction.Search, AppResource.Sales),
        new("Create Sales", AppAction.Create, AppResource.Sales),
        new("Update Sales", AppAction.Update, AppResource.Sales),
        new("Delete Sales", AppAction.Delete, AppResource.Sales),
        new("Export Sales", AppAction.Export, AppResource.Sales),
        new("Print Sales", AppAction.Print, AppResource.Sales),

        // POSSales
        new("View POSSales", AppAction.View, AppResource.POSSales),
        new("Search POSSales", AppAction.Search, AppResource.POSSales),
        new("Create POSSales", AppAction.Create, AppResource.POSSales),
        new("Update POSSales", AppAction.Update, AppResource.POSSales),
        new("Delete POSSales", AppAction.Delete, AppResource.POSSales),
        new("Export POSSales", AppAction.Export, AppResource.POSSales),
        new("Print POSSales", AppAction.Print, AppResource.POSSales),

        // SaleSupplies
        new("View SaleSupplies", AppAction.View, AppResource.SaleSupplies),
        new("Search SaleSupplies", AppAction.Search, AppResource.SaleSupplies),
        new("Create SaleSupplies", AppAction.Create, AppResource.SaleSupplies),
        new("Update SaleSupplies", AppAction.Update, AppResource.SaleSupplies),
        new("Delete SaleSupplies", AppAction.Delete, AppResource.SaleSupplies),
        new("Export SaleSupplies", AppAction.Export, AppResource.SaleSupplies),

        // PurchaseReturns
        new("View PurchaseReturns", AppAction.View, AppResource.PurchaseReturns),
        new("Search PurchaseReturns", AppAction.Search, AppResource.PurchaseReturns),
        new("Create PurchaseReturns", AppAction.Create, AppResource.PurchaseReturns),
        new("Update PurchaseReturns", AppAction.Update, AppResource.PurchaseReturns),
        new("Delete PurchaseReturns", AppAction.Delete, AppResource.PurchaseReturns),
        new("Export PurchaseReturns", AppAction.Export, AppResource.PurchaseReturns),

        // SaleReturns
        new("View SaleReturns", AppAction.View, AppResource.SaleReturns),
        new("Search SaleReturns", AppAction.Search, AppResource.SaleReturns),
        new("Create SaleReturns", AppAction.Create, AppResource.SaleReturns),
        new("Update SaleReturns", AppAction.Update, AppResource.SaleReturns),
        new("Delete SaleReturns", AppAction.Delete, AppResource.SaleReturns),
        new("Export SaleReturns", AppAction.Export, AppResource.SaleReturns),

        // StockAdjustments
        new("View StockAdjustments", AppAction.View, AppResource.StockAdjustments),
        new("Search StockAdjustments", AppAction.Search, AppResource.StockAdjustments),
        new("Create StockAdjustments", AppAction.Create, AppResource.StockAdjustments),
        new("Update StockAdjustments", AppAction.Update, AppResource.StockAdjustments),
        new("Delete StockAdjustments", AppAction.Delete, AppResource.StockAdjustments),
        new("Export StockAdjustments", AppAction.Export, AppResource.StockAdjustments),

        // BankReconciliations
        new("View BankReconciliations", AppAction.View, AppResource.BankReconciliations),
        new("Search BankReconciliations", AppAction.Search, AppResource.BankReconciliations),
        new("Create BankReconciliations", AppAction.Create, AppResource.BankReconciliations),
        new("Update BankReconciliations", AppAction.Update, AppResource.BankReconciliations),
        new("Delete BankReconciliations", AppAction.Delete, AppResource.BankReconciliations),
        new("Export BankReconciliations", AppAction.Export, AppResource.BankReconciliations),

        // Payrolls
        new("View Payrolls", AppAction.View, AppResource.Payrolls),
        new("Search Payrolls", AppAction.Search, AppResource.Payrolls),
        new("Create Payrolls", AppAction.Create, AppResource.Payrolls),
        new("Update Payrolls", AppAction.Update, AppResource.Payrolls),
        new("Delete Payrolls", AppAction.Delete, AppResource.Payrolls),
        new("Export Payrolls", AppAction.Export, AppResource.Payrolls)
    };

    public static IReadOnlyList<AppPermission> Admin { get; } = new ReadOnlyCollection<AppPermission>(_all);
    public static IReadOnlyList<AppPermission> Basic { get; } = new ReadOnlyCollection<AppPermission>(_all.Where(p => p.IsBasic).ToArray());

    public static IReadOnlyList<AppPermission> Cashier { get; } = new ReadOnlyCollection<AppPermission>(_all.Where(p =>
        p.Resource == AppResource.Dashboard ||
        p.Resource == AppResource.Sales ||
        p.Resource == AppResource.POSSales ||
        p.Resource == AppResource.Customers
    ).ToArray());

    public static IReadOnlyList<AppPermission> InventoryManager { get; } = new ReadOnlyCollection<AppPermission>(_all.Where(p =>
        p.Resource == AppResource.Dashboard ||
        p.Resource == AppResource.InventoryItems ||
        p.Resource == AppResource.ItemCategories ||
        p.Resource == AppResource.Units ||
        p.Resource == AppResource.Purchases ||
        p.Resource == AppResource.PurchaseReturns ||
        p.Resource == AppResource.StockAdjustments ||
        p.Resource == AppResource.Vendors
    ).ToArray());

    public static IReadOnlyList<AppPermission> Accountant { get; } = new ReadOnlyCollection<AppPermission>(_all.Where(p =>
        p.Resource == AppResource.Dashboard ||
        p.Resource == AppResource.ChartOfAccounts ||
        p.Resource == AppResource.DetailAccounts ||
        p.Resource == AppResource.Customers ||
        p.Resource == AppResource.Vendors ||
        p.Resource == AppResource.PaymentVouchers ||
        p.Resource == AppResource.ReceiptVouchers ||
        p.Resource == AppResource.JournalVouchers ||
        p.Resource == AppResource.BankReconciliations ||
        p.Resource == AppResource.OpeningBalances ||
        p.Resource == AppResource.Reports
    ).ToArray());

    public static IReadOnlyList<AppPermission> PayrollManager { get; } = new ReadOnlyCollection<AppPermission>(_all.Where(p =>
        p.Resource == AppResource.Dashboard ||
        p.Resource == AppResource.HRInfo ||
        p.Resource == AppResource.Payrolls
    ).ToArray());
}

public record AppPermission(string Description, string Action, string Resource, bool IsBasic = false)
{
    public string Name => NameFor(Action, Resource);
    public static string NameFor(string action, string resource) => $"Permissions.{resource}.{action}";
}
