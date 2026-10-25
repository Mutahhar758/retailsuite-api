using Retailer.Domain.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Retailer.Infrastructure.AI.Gemini;

namespace Retailer.Infrastructure.Multitenancy;

internal class AiToolSeeder
{
    private readonly TenantDbContext _tenantDbContext;
    private readonly IGeminiEmbeddingService? _embeddingService;
    private readonly ILogger<AiToolSeeder> _logger;

    public AiToolSeeder(TenantDbContext tenantDbContext, IGeminiEmbeddingService? embeddingService, ILogger<AiToolSeeder> logger)
    {
        _tenantDbContext = tenantDbContext;
        _embeddingService = embeddingService;
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
                if (_embeddingService != null)
                {
                    try
                    {
                        tool.Embedding = await _embeddingService.GenerateEmbeddingAsync($"{tool.ToolName}: {tool.Description}", cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to compute embedding for new tool {ToolId}", tool.Id);
                    }
                }

                _logger.LogInformation("Seeding AI Tool: {ToolId}", tool.Id);
                await _tenantDbContext.AiTools.AddAsync(tool, cancellationToken);
            }
            else
            {
                bool descriptionChanged = existing.Description != tool.Description;
                existing.Description = tool.Description;
                existing.JsonSchema = tool.JsonSchema;
                existing.RouteHint = tool.RouteHint;
                existing.Category = tool.Category;
                existing.RequiredPermission = tool.RequiredPermission;

                if (_embeddingService != null && (existing.Embedding == null || descriptionChanged))
                {
                    try
                    {
                        existing.Embedding = await _embeddingService.GenerateEmbeddingAsync($"{tool.ToolName}: {tool.Description}", cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to recompute embedding for tool {ToolId}", tool.Id);
                    }
                }

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
                Id = "navigate_to_report",
                ToolName = "navigate_to_report",
                Description = "Opens business reports, account statement, ledger, khata, viewing transactions of an account, customer, or vendor (transactions dekhna, hisab kitab, mahine ki transactions, statement nikalna, khata check karna, parchi dekhna), trial balance, stock balance, item ledger, income summary, balance sheet, customer balance recovery, profit by customer, profit by item, manufacturing report.",
                Category = "Reports",
                RouteHint = "/reports/account-statement",
                RequiredPermission = "Permissions.Reports.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "reportType": {
                      "type": "string",
                      "enum": ["account_statement","account_statement_with_due","account_balance","trial_balance","stock_balance","item_ledger","income_summary","balance_sheet","customer_balance_recovery","profit_by_customer","profit_by_item","purchase_supply_comparison","customer_bill","manufacturing"],
                      "description": "Type of report to open: 'account_statement' (general ledger/account khata), 'account_statement_with_due', 'account_balance' (schedule of account balances), 'trial_balance', 'stock_balance', 'item_ledger' (product movement), 'income_summary' (P&L/income), 'balance_sheet', 'customer_balance_recovery', 'profit_by_customer', 'profit_by_item', 'purchase_supply_comparison', 'customer_bill', 'manufacturing'"
                    },
                    "account": { "type": "string", "description": "Resolved account ID/number for account-based reports" },
                    "accountTitle": { "type": "string", "description": "Display name/title of the account" },
                    "item": { "type": "string", "description": "Resolved item ID for item-based reports" },
                    "itemTitle": { "type": "string", "description": "Display title of the item" },
                    "category": { "type": "string", "description": "Category code for stock balance" },
                    "fromDate": { "type": "string", "description": "Start date YYYY-MM-DD" },
                    "toDate": { "type": "string", "description": "End date YYYY-MM-DD" },
                    "dateBasis": { "type": "string", "enum": ["VoucherDate","ClearingDate"], "description": "Date basis for filtering (default VoucherDate)" },
                    "stockFilter": { "type": "string", "enum": ["All","Zero","Negative","Positive"], "description": "Stock balance filter" },
                    "balanceFilter": { "type": "string", "enum": ["All","HasBalance","ZeroBalance","NegativeBalance"], "description": "Customer recovery filter" }
                  },
                  "required": ["reportType"]
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "search_account",
                ToolName = "search_account",
                Description = "Searches accounts in the Chart of Accounts (customers, vendors, cash in hand, bank accounts, expenses, ledger accounts, Ali, test account) to find account ID, ledger balance, balance inquiry (kitna balance hai, kitne paise bante hain, dena hai ya lena hai, kitna baqi hai).",
                Category = "Accounting",
                RouteHint = "/setup/chart-of-accounts",
                RequiredPermission = "Permissions.BankAccounts.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "names": { "type": "array", "items": { "type": "string" }, "description": "List of possible account names, search keywords, or variations (e.g. ['ali', 'ali raza'])" },
                    "name": { "type": "string", "description": "Account or customer name to search" },
                    "includeBalance": { "type": "boolean", "description": "Whether to return current balance" }
                  }
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "search_vendor",
                ToolName = "search_vendor",
                Description = "Searches vendors and suppliers in database by name or keyword, supplier balance inquiry, vendor balance, vendor ka hisab, supplier ko kitne paise dene hain, vendor ledger balance, payables.",
                Category = "Vendor",
                RouteHint = "/setup/vendors",
                RequiredPermission = "Permissions.Vendors.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "names": { "type": "array", "items": { "type": "string" }, "description": "List of possible vendor names, search keywords, or variations" },
                    "name": { "type": "string", "description": "Vendor name or search keyword" },
                    "includeBalance": { "type": "boolean", "description": "Whether to return current balance" }
                  }
                }
                """,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "search_item",
                ToolName = "search_item",
                Description = "Searches inventory products and items by title, product name, barcode, item key, milk pack, panadol, mobile, stock item, product movement, item details.",
                Category = "Inventory",
                RouteHint = "/setup/item-details",
                RequiredPermission = "Permissions.Items.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "names": { "type": "array", "items": { "type": "string" }, "description": "List of possible item names, keywords, or variations (e.g. ['milk', 'milk pack'])" },
                    "name": { "type": "string", "description": "Item name to search" }
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
                Description = "Drafts a new payment voucher, pay vendor, pay supplier, expense payment, vendor ko payment karni hai, paise ada karne hain, payment charhana, parchi banana, payment voucher entry.",
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
                Id = "filter_payment_listing",
                ToolName = "filter_payment_listing",
                Description = "Filters and navigates to payment voucher listing screen, payment history, payment vouchers dekhna, payment records, past payments, payments list.",
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
                Id = "draft_receipt_voucher",
                ToolName = "draft_receipt_voucher",
                Description = "Drafts a new receipt voucher, receive money from customer or debtor, customer se paise wasool hue, vasooli ki entry, payment receive karna, jama karwana, receipt voucher banana, customer recovery.",
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
                Id = "filter_receipt_listing",
                ToolName = "filter_receipt_listing",
                Description = "Filters and navigates to receipt voucher listing screen, receipt history, customer recovery list, wasooli records dekhna, past receipt vouchers, receipts list.",
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
                Id = "filter_sale_listing",
                ToolName = "filter_sale_listing",
                Description = "Filters and navigates to sales listing, invoice history, sales invoices dekhna, sale records, bills history, daily sales list.",
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
