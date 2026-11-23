namespace Retailer.Shared.Common.Constants;

public static class ChartOfAccountConstants
{
    public static class Levels
    {
        public const int Root = 1;
        public const int Group = 2;
        public const int SubGroup = 3;
        public const int Category = 4;
        public const int Detail = 5;
    }

    public static class AccountTypes
    {
        public const string Parent = "Parent";
        public const string Detail = "Detail";
    }

    public static class SuffixFormats
    {
        public const string Default = "D3";
    }

    public static class RootIds
    {
        public const string Assets = "001";
        public const string Liabilities = "002";
        public const string Income = "003";
        public const string Expenses = "004";
        public const string Capital = "005";
    }

    public static class Prefixes
    {
        public const string CashBanks = "001002002";
        public const string Cash = "001002002001";
        public const string Banks = "001002002002";
        public const string VendorBillLegacy = "02";
    }

    public static class DefaultAccountTitles
    {
        public const string Cash = "Cash";
        public const string Bank = "Bank";
        public const string Banks = "Banks";
        public const string Customers = "Customers";
        public const string Suppliers = "Suppliers";
        public const string Sales = "SL";
        public const string Purchases = "PU";
        public const string Inventory = "INVENTORY";
        public const string Capital = "Capital";
        public const string SalesReturn = "SR";
        public const string PurchaseReturn = "PR";
        public const string SalesToRec = "SalesToRec";
        public const string SalesPosting = "SP";
    }
}
