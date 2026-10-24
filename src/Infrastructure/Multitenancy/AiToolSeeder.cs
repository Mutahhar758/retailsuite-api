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
        var existingTools = await _tenantDbContext.AiTools
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var toolsToSeed = GetInitialTools();

        foreach (var tool in toolsToSeed)
        {
            if (!existingTools.TryGetValue(tool.Id, out var existing))
            {
                _logger.LogInformation("Seeding AI Tool: {ToolId}", tool.Id);
                await _tenantDbContext.AiTools.AddAsync(tool, cancellationToken);
            }
            else
            {
                existing.Description = tool.Description;
                existing.JsonSchema = tool.JsonSchema;
                existing.RouteHint = tool.RouteHint;
                existing.Category = tool.Category;
                existing.RequiredPermission = tool.RequiredPermission;
                _tenantDbContext.AiTools.Update(existing);
            }
        }

        if (_tenantDbContext.ChangeTracker.HasChanges())
        {
            await _tenantDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI Tools successfully seeded/updated in TenantDbContext.");
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
                Id = "filter_receipt_listing",
                ToolName = "filter_receipt_listing",
                Description = "Filters and navigates to the receipt voucher listing screen by date range, cash/bank account, or voucher number.",
                Category = "Receipt",
                RouteHint = "/daily-entries/receipt-voucher",
                RequiredPermission = "Permissions.ReceiptVouchers.View",
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
                Id = "draft_receipt_voucher",
                ToolName = "draft_receipt_voucher",
                Description = "Drafts a new receipt voucher for receiving money from a customer, debtor, or income account with pre-filled amount and remarks.",
                Category = "Receipt",
                RouteHint = "/daily-entries/receipt-voucher/new",
                RequiredPermission = "Permissions.ReceiptVouchers.Create",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "customerId": { "type": "string", "description": "The resolved Customer/Payer Account ID" },
                    "customerName": { "type": "string", "description": "Name of the customer or paying account" },
                    "amount": { "type": "number", "description": "Received amount" },
                    "remarks": { "type": "string", "description": "Remarks or receipt narration" },
                    "cashBankAccountId": { "type": "string", "description": "Cash/Bank account receiving the deposit" }
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
                Description = "Searches the active tenant database for vendors or suppliers matching a given name or keyword. Set includeBalance=true to fetch their current ledger balance.",
                Category = "Vendor",
                RouteHint = "/setup/vendors",
                RequiredPermission = "Permissions.Vendors.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "description": "Vendor or supplier business name to search for" },
                    "includeBalance": { "type": "boolean", "description": "Whether to return the current ledger balance (dr - cr) for each matched vendor" }
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
                Description = "Searches accounts in the Chart of Accounts by title or code (customers, vendors, cash, bank, expenses). Set includeBalance=true to fetch current balance.",
                Category = "Accounting",
                RouteHint = "/setup/bank-accounts",
                RequiredPermission = "Permissions.BankAccounts.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "description": "Account name (e.g. 'Cash In Hand', 'Meezan Bank', 'Customer ABC')" },
                    "includeBalance": { "type": "boolean", "description": "Whether to return the current ledger balance (dr - cr) for each matched account" }
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
