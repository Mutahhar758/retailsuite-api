using Retailer.Domain.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Retailer.Infrastructure.Multitenancy;

internal class AiToolSeeder
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly ILogger<AiToolSeeder> _logger;

    public AiToolSeeder(TenantDbContext tenantDbContext, ILogger<AiToolSeeder> logger)
    {
        _tenantDbContext = tenantDbContext;
        _logger = logger;
    }

    public async Task SeedAiToolsAsync(CancellationToken cancellationToken = default)
    {
        var existingToolIds = await _tenantDbContext.AiTools
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var toolsToSeed = GetInitialTools();

        foreach (var tool in toolsToSeed)
        {
            if (!existingToolIds.Contains(tool.Id))
            {
                _logger.LogInformation("Seeding AI Tool: {ToolId}", tool.Id);
                await _tenantDbContext.AiTools.AddAsync(tool, cancellationToken);
            }
        }

        if (_tenantDbContext.ChangeTracker.HasChanges())
        {
            await _tenantDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI Tools successfully seeded in TenantDbContext.");
        }
    }

    private static List<AiTool> GetInitialTools()
    {
        return new List<AiTool>
        {
            new()
            {
                Id = "filter_payment_listing",
                ToolName = "filter_payment_listing",
                Description = "Filters and navigates to the payment voucher listing screen by date range, cash/bank account, or voucher number.",
                Category = "Payment",
                RouteHint = "/daily-entries/payment-voucher",
                RequiredPermission = "Permissions.PaymentVouchers.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "fromDate": { "type": "string", "description": "Start date in YYYY-MM-DD format" },
                    "toDate": { "type": "string", "description": "End date in YYYY-MM-DD format" },
                    "cashBankAccount": { "type": "string", "description": "Cash or Bank Account name or ID" },
                    "voucherNo": { "type": "string", "description": "Specific voucher number to find" }
                  }
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "draft_payment_voucher",
                ToolName = "draft_payment_voucher",
                Description = "Drafts a new payment voucher for a vendor, supplier, or expense with pre-filled amount and remarks.",
                Category = "Payment",
                RouteHint = "/daily-entries/payment-voucher/new",
                RequiredPermission = "Permissions.PaymentVouchers.Create",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "vendorId": { "type": "string", "description": "The resolved Vendor ID" },
                    "vendorName": { "type": "string", "description": "Name of the vendor/supplier" },
                    "amount": { "type": "number", "description": "Payment amount" },
                    "remarks": { "type": "string", "description": "Purpose or remarks for the payment" },
                    "cashBankAccountId": { "type": "string", "description": "Account ID to pay from (e.g. Cash, Bank)" }
                  },
                  "required": ["amount"]
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "search_vendor",
                ToolName = "search_vendor",
                Description = "Searches the active tenant database for vendors or suppliers matching a given name or keyword to resolve their ID.",
                Category = "Vendor",
                RouteHint = "/setup/vendors",
                RequiredPermission = "Permissions.Vendors.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "description": "Vendor or supplier business name to search for" }
                  },
                  "required": ["name"]
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "search_account",
                ToolName = "search_account",
                Description = "Searches cash and bank accounts in the Chart of Accounts to find the exact account ID.",
                Category = "Accounting",
                RouteHint = "/setup/bank-accounts",
                RequiredPermission = "Permissions.BankAccounts.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "description": "Account name (e.g. 'Cash In Hand', 'Meezan Bank')" }
                  },
                  "required": ["name"]
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "filter_sale_listing",
                ToolName = "filter_sale_listing",
                Description = "Filters and navigates to the sales listing / invoice history by date range or customer.",
                Category = "Sale",
                RouteHint = "/daily-entries/sale",
                RequiredPermission = "Permissions.Sales.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "fromDate": { "type": "string", "description": "Start date in YYYY-MM-DD format" },
                    "toDate": { "type": "string", "description": "End date in YYYY-MM-DD format" },
                    "customerId": { "type": "string", "description": "Customer ID or name filter" }
                  }
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}
