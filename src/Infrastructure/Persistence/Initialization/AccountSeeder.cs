using Retailer.Application.Common.Persistence;
using Retailer.Domain.Legacy;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Persistence.Initialization;

public class AccountSeeder : ICustomSeeder
{
    private readonly IRepository<ChartOfAccount> _coaRepository;
    private readonly IRepository<DefaultAccount> _defaultAccRepository;
    private readonly ILogger<AccountSeeder> _logger;

    public AccountSeeder(
        IRepository<ChartOfAccount> coaRepository,
        IRepository<DefaultAccount> defaultAccRepository,
        ILogger<AccountSeeder> logger)
    {
        _coaRepository = coaRepository;
        _defaultAccRepository = defaultAccRepository;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await SeedChartOfAccountsAsync(cancellationToken);
        await EnsureCoreAccountHierarchyAsync(cancellationToken);
        await SeedDefaultAccountsAsync(cancellationToken);
    }

    private static List<ChartOfAccount> GetCoreAccounts()
    {
        return new List<ChartOfAccount>
        {
            new() { Id = "001", Title = "ASSETS", ParentId = null, AccType = "Parent", AccLevel = 1 },
            new() { Id = "002", Title = "LIABILITIES", ParentId = null, AccType = "Parent", AccLevel = 1 },
            new() { Id = "003", Title = "INCOME", ParentId = null, AccType = "Parent", AccLevel = 1 },
            new() { Id = "004", Title = "EXPENSES", ParentId = null, AccType = "Parent", AccLevel = 1 },
            new() { Id = "005", Title = "CAPITAL", ParentId = null, AccType = "Parent", AccLevel = 1 },

            // --- FIXED ASSETS ---
            new() { Id = "001001", Title = "Fixed Assets", ParentId = "001", AccType = "Parent", AccLevel = 2 },
            // Level 3: Parents
            new() { Id = "001001001", Title = "Equipment & Machinery", ParentId = "001001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001001002", Title = "Furniture & Fixtures", ParentId = "001001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001001003", Title = "Computers & Electronics", ParentId = "001001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001001004", Title = "Vehicles", ParentId = "001001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001001005", Title = "Land & Buildings", ParentId = "001001", AccType = "Parent", AccLevel = 3 },
            // Level 4: Subcategories under Fixed Assets
            new() { Id = "001001001001", Title = "Store & Retail Equipment", ParentId = "001001001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001001002", Title = "Appliances & Climate", ParentId = "001001001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001002001", Title = "Display Racks & Shelving", ParentId = "001001002", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001002002", Title = "Counters, Desks & Chairs", ParentId = "001001002", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001003001", Title = "POS Terminals, Barcode & Printers", ParentId = "001001003", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001003002", Title = "CCTV & Security Systems", ParentId = "001001003", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001004001", Title = "Delivery Bikes & Vans", ParentId = "001001004", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001005001", Title = "Shop & Warehouse Premises", ParentId = "001001005", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001001005002", Title = "Renovation & Leasehold Improvements", ParentId = "001001005", AccType = "Parent", AccLevel = 4 },

            // --- CURRENT ASSETS ---
            new() { Id = "001002", Title = "Current Assets", ParentId = "001", AccType = "Parent", AccLevel = 2 },
            new() { Id = "001002001", Title = "Customer Receivable", ParentId = "001002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001002001001", Title = "Customers", ParentId = "001002001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001002002", Title = "Cash & Banks", ParentId = "001002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001002002001", Title = "Cash", ParentId = "001002002", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001002002001001", Title = "Cash On Hand", ParentId = "001002002001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "001002002002", Title = "Banks", ParentId = "001002002", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001002002002001", Title = "Cash In Banks", ParentId = "001002002002", AccType = "Detail", AccLevel = 5 },
            new() { Id = "001002003", Title = "MERCHANDISE INVENTORY", ParentId = "001002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001002003001", Title = "INVENTORY", ParentId = "001002003", AccType = "Parent", AccLevel = 4 },
            new() { Id = "001002003001001", Title = "INVENTORY", ParentId = "001002003001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "001002004", Title = "OFFICE SUPPLIES", ParentId = "001002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001002005", Title = "PREPAID INSURANCE", ParentId = "001002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "001003", Title = "Other Assets", ParentId = "001", AccType = "Parent", AccLevel = 2 },

            // --- LIABILITIES ---
            new() { Id = "002001", Title = "CURRENT LIABILITIES", ParentId = "002", AccType = "Parent", AccLevel = 2 },
            new() { Id = "002001001", Title = "ACCOUNT PAYABLE", ParentId = "002001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "002001001001", Title = "VENDERS", ParentId = "002001001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "002001002", Title = "BILL PAYABLE", ParentId = "002001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "002001003", Title = "SINGER PAYABLE", ParentId = "002001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "002001004", Title = "BANK OVER DRAFT (Loan)", ParentId = "002001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "002002", Title = "LONG TERM LIABILITIES", ParentId = "002", AccType = "Parent", AccLevel = 2 },
            new() { Id = "002002001", Title = "BANK LOAN PAYABLE", ParentId = "002002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "002002002", Title = "DEPENTURE PAYABLE", ParentId = "002002", AccType = "Parent", AccLevel = 3 },

            // --- INCOME ---
            new() { Id = "003001", Title = "SALES INCOME", ParentId = "003", AccType = "Parent", AccLevel = 2 },
            new() { Id = "003001001", Title = "SALES INCOME", ParentId = "003001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "003001001001", Title = "SALES", ParentId = "003001001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "003001001001001", Title = "SALES", ParentId = "003001001001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "003001002", Title = "SALE RETURN", ParentId = "003001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "003002", Title = "OTHER INCOME", ParentId = "003", AccType = "Parent", AccLevel = 2 },

            // --- EXPENSES ---
            new() { Id = "004001", Title = "PURCHASES EXP", ParentId = "004", AccType = "Parent", AccLevel = 2 },
            new() { Id = "004001001", Title = "PURCHASE ", ParentId = "004001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "004001001001", Title = "PURCHASE", ParentId = "004001001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "004001001001001", Title = "Purchase", ParentId = "004001001001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004001002", Title = "PURCHASE RETURN ", ParentId = "004001", AccType = "Parent", AccLevel = 3 },

            // Operating Expenses (004002)
            new() { Id = "004002", Title = "OPERATING EXPENSES", ParentId = "004", AccType = "Parent", AccLevel = 2 },
            // Level 3 Parents
            new() { Id = "004002001", Title = "Administrative & Office Expenses", ParentId = "004002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "004002002", Title = "Selling & Distribution Expenses", ParentId = "004002", AccType = "Parent", AccLevel = 3 },
            new() { Id = "004002003", Title = "Financial Expenses", ParentId = "004002", AccType = "Parent", AccLevel = 3 },

            // Level 4 Subcategories under Administrative
            new() { Id = "004002001001", Title = "Utility Bills", ParentId = "004002001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "004002001002", Title = "Rent & Premises", ParentId = "004002001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "004002001003", Title = "Repairs & Maintenance", ParentId = "004002001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "004002001004", Title = "Refreshments & Staff Welfare", ParentId = "004002001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "004002001005", Title = "Printing, Stationery & Supplies", ParentId = "004002001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "004002001006", Title = "Cleaning & Janitorial", ParentId = "004002001", AccType = "Parent", AccLevel = 4 },

            // Level 4 Subcategories under Selling & Distribution
            new() { Id = "004002002001", Title = "Delivery & Fuel Charges", ParentId = "004002002", AccType = "Parent", AccLevel = 4 },
            new() { Id = "004002002002", Title = "Advertising & Marketing", ParentId = "004002002", AccType = "Parent", AccLevel = 4 },

            // Level 4 Subcategories under Financial
            new() { Id = "004002003001", Title = "Bank Charges & Fees", ParentId = "004002003", AccType = "Parent", AccLevel = 4 },

            // Level 5 Pre-seeded Detail Accounts
            new() { Id = "004002001001001", Title = "Electricity Bill", ParentId = "004002001001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001001002", Title = "Internet & Telephone", ParentId = "004002001001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001001003", Title = "Water & Gas Bill", ParentId = "004002001001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001002001", Title = "Store Rent", ParentId = "004002001002", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001003001", Title = "General Maintenance & Repairs", ParentId = "004002001003", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001004001", Title = "Tea & Refreshments", ParentId = "004002001004", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001004002", Title = "Staff Meals", ParentId = "004002001004", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001005001", Title = "Packaging Materials & Bags", ParentId = "004002001005", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002001005002", Title = "POS Rolls & Receipt Paper", ParentId = "004002001005", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002002001001", Title = "Fuel Expenses", ParentId = "004002002001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002002002001", Title = "Promotion & Banners", ParentId = "004002002002", AccType = "Detail", AccLevel = 5 },
            new() { Id = "004002003001001", Title = "Bank Service Charges", ParentId = "004002003001", AccType = "Detail", AccLevel = 5 },

            // --- CAPITAL ---
            new() { Id = "005001", Title = "CAPITAL", ParentId = "005", AccType = "Parent", AccLevel = 2 },
            new() { Id = "005001001", Title = "CAPITAL", ParentId = "005001", AccType = "Parent", AccLevel = 3 },
            new() { Id = "005001001001", Title = "EQUITY", ParentId = "005001001", AccType = "Parent", AccLevel = 4 },
            new() { Id = "005001001001001", Title = "Owner's Equity", ParentId = "005001001001", AccType = "Detail", AccLevel = 5 },
            new() { Id = "005002", Title = "DRAWING", ParentId = "005", AccType = "Parent", AccLevel = 2 },
        };
    }

    private async Task SeedChartOfAccountsAsync(CancellationToken cancellationToken)
    {
        if (!await _coaRepository.GetAll().AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding Chart of Accounts.");
            var accounts = GetCoreAccounts();
            var levels = accounts.GroupBy(a => a.AccLevel).OrderBy(g => g.Key);
            foreach (var level in levels)
            {
                await _coaRepository.AddRangeAsync(level.ToList(), true);
            }
        }
    }

    private async Task EnsureCoreAccountHierarchyAsync(CancellationToken cancellationToken)
    {
        // Safe idempotent update for existing databases so they receive missing Level 3 & Level 4 parents
        var existingIds = (await _coaRepository.GetAll()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var missing = GetCoreAccounts()
            .Where(x => !existingIds.Contains(x.Id))
            .GroupBy(x => x.AccLevel)
            .OrderBy(g => g.Key);

        foreach (var levelGroup in missing)
        {
            var accountsToAdd = levelGroup.ToList();
            if (accountsToAdd.Count > 0)
            {
                _logger.LogInformation("Adding {Count} missing core accounts at level {Level}.", accountsToAdd.Count, levelGroup.Key);
                await _coaRepository.AddRangeAsync(accountsToAdd, true);
            }
        }
    }

    private async Task SeedDefaultAccountsAsync(CancellationToken cancellationToken)
    {
        if (!await _defaultAccRepository.GetAll().AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seeding Default Accounts.");
            var defaultAccounts = new List<DefaultAccount>
            {
                new() { Title = "SL", A = null, AccountId = "003001001001001", MapAccountId = null },
                new() { Title = "PU", A = null, AccountId = "004001001001001", MapAccountId = null },
                new() { Title = "Bank", A = null, AccountId = "001002002002001", MapAccountId = null },
                new() { Title = "Cash", A = null, AccountId = "001002002001001", MapAccountId = null },
                new() { Title = "INVENTORY", A = null, AccountId = "001002003001001", MapAccountId = null },
                new() { Title = "Capital", A = null, AccountId = "005001001001001", MapAccountId = null },
                new() { Title = "Customers", A = null, AccountId = "001002001001", MapAccountId = null },
                new() { Title = "Suppliers", A = null, AccountId = "002001001001", MapAccountId = null },
                new() { Title = "SR", A = null, AccountId = "003001001001001", MapAccountId = null },
                new() { Title = "PR", A = null, AccountId = "004001001001001", MapAccountId = null },
                new() { Title = "SalesToRec", A = "\\rptHeader.jpg", AccountId = "003001001001001", MapAccountId = "001002001" },
                new() { Title = "SP", A = null, AccountId = "003001001001001", MapAccountId = null },
            };

            await _defaultAccRepository.AddRangeAsync(defaultAccounts);
            await _defaultAccRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
