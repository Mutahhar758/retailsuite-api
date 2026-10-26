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
        // 1. Seed or Update AI Tools
        var existingTools = await _tenantDbContext.AiTools
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        // Remove obsolete search_vendor tool if it exists
        if (existingTools.TryGetValue("search_vendor", out var obsoleteVendorTool))
        {
            _logger.LogInformation("Removing obsolete tool 'search_vendor' (replaced by search_account).");
            _tenantDbContext.AiTools.Remove(obsoleteVendorTool);
            existingTools.Remove("search_vendor");
        }

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

        await _tenantDbContext.SaveChangesAsync(cancellationToken);

        // 2. Seed or Update AI Tasks & Task-Tool Mappings
        await SeedAiTasksAsync(cancellationToken);
    }

    private async Task SeedAiTasksAsync(CancellationToken cancellationToken)
    {
        var existingTasks = await _tenantDbContext.AiTasks
            .Include(t => t.TaskTools)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var taskDefinitions = GetInitialTasks();

        foreach (var def in taskDefinitions)
        {
            var task = def.Task;
            if (!existingTasks.TryGetValue(task.Id, out var existing))
            {
                if (_embeddingService != null)
                {
                    try
                    {
                        task.IntentEmbedding = await _embeddingService.GenerateEmbeddingAsync($"{task.TaskName}: {task.Description}", cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to compute intent embedding for new task {TaskId}", task.Id);
                    }
                }

                _logger.LogInformation("Seeding AI Task: {TaskId}", task.Id);
                await _tenantDbContext.AiTasks.AddAsync(task, cancellationToken);
                await _tenantDbContext.SaveChangesAsync(cancellationToken);

                // Add Task-Tool mappings
                for (int i = 0; i < def.ToolIds.Count; i++)
                {
                    var toolId = def.ToolIds[i];
                    await _tenantDbContext.AiTaskTools.AddAsync(new AiTaskTool
                    {
                        TaskId = task.Id,
                        ToolId = toolId,
                        Order = i
                    }, cancellationToken);
                }
            }
            else
            {
                bool descriptionChanged = existing.Description != task.Description;
                existing.TaskName = task.TaskName;
                existing.Description = task.Description;
                existing.RouteHint = task.RouteHint;
                existing.TaskSpecificSystemPrompt = task.TaskSpecificSystemPrompt;
                existing.IsActive = task.IsActive;

                if (_embeddingService != null && (existing.IntentEmbedding == null || descriptionChanged))
                {
                    try
                    {
                        existing.IntentEmbedding = await _embeddingService.GenerateEmbeddingAsync($"{task.TaskName}: {task.Description}", cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to recompute intent embedding for task {TaskId}", task.Id);
                    }
                }

                _tenantDbContext.AiTasks.Update(existing);

                // Update TaskTools mappings
                var currentToolIds = existing.TaskTools.Select(tt => tt.ToolId).ToHashSet();
                var newToolIds = def.ToolIds.ToHashSet();

                var toRemove = existing.TaskTools.Where(tt => !newToolIds.Contains(tt.ToolId)).ToList();
                foreach (var rem in toRemove)
                {
                    _tenantDbContext.AiTaskTools.Remove(rem);
                }

                for (int i = 0; i < def.ToolIds.Count; i++)
                {
                    var toolId = def.ToolIds[i];
                    var existingMapping = existing.TaskTools.FirstOrDefault(tt => tt.ToolId == toolId);
                    if (existingMapping == null)
                    {
                        await _tenantDbContext.AiTaskTools.AddAsync(new AiTaskTool
                        {
                            TaskId = existing.Id,
                            ToolId = toolId,
                            Order = i
                        }, cancellationToken);
                    }
                    else
                    {
                        existingMapping.Order = i;
                        _tenantDbContext.AiTaskTools.Update(existingMapping);
                    }
                }
            }
        }

        if (_tenantDbContext.ChangeTracker.HasChanges())
        {
            await _tenantDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI Tasks and TaskTools successfully seeded/updated in TenantDbContext.");
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
                Description = "Searches accounts in the Chart of Accounts (customers, vendors, suppliers, cash in hand, bank accounts, expenses, ledger accounts, Ali, test account) to find account ID, ledger balance, balance inquiry (kitna balance hai, kitne paise bante hain, dena hai ya lena hai, kitna baqi hai, payables, receivables).",
                Category = "Accounting",
                RouteHint = "/setup/chart-of-accounts",
                RequiredPermission = "Permissions.BankAccounts.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "names": { "type": "array", "items": { "type": "string" }, "description": "List of possible account names, vendor names, search keywords, or variations (e.g. ['ali', 'ali raza'])" },
                    "name": { "type": "string", "description": "Account, vendor, or customer name to search" },
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
                Description = "Searches inventory products and items by title, product name, barcode, item key, milk pack, panadol, mobile, stock item, product movement, item details. Set includeStockBalance=true to retrieve current on-hand stock balance / available quantity (stock kitna hai, maal kitna bacha hai, kitne carton/packets hain).",
                Category = "Inventory",
                RouteHint = "/setup/item-details",
                RequiredPermission = "Permissions.Items.View",
                JsonSchema = """
                {
                  "type": "object",
                  "properties": {
                    "names": { "type": "array", "items": { "type": "string" }, "description": "List of possible item names, keywords, or variations (e.g. ['milk', 'milk pack'])" },
                    "name": { "type": "string", "description": "Item name to search" },
                    "includeStockBalance": { "type": "boolean", "description": "Whether to return current on-hand stock balance / available quantity" }
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
                    "vendorId": { "type": "string", "description": "The resolved Vendor ID / Account ID" },
                    "vendorName": { "type": "string", "description": "Name of the vendor/supplier/payee" },
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

    private record SeedTaskDefinition(AiTask Task, List<string> ToolIds);

    private static List<SeedTaskDefinition> GetInitialTasks()
    {
        return new List<SeedTaskDefinition>
        {
            new(
                new AiTask
                {
                    Id = "task_draft_payment_voucher",
                    TaskName = "Draft Payment Voucher",
                    Description = "Drafts a payment voucher to pay a vendor, supplier, or business expense. Vendor ko payment karni hai, paise ada karne hain, payment charhana, parchi banana, payment voucher entry, supplier payment.",
                    RouteHint = "/daily-entries/payment-voucher/new",
                    TaskSpecificSystemPrompt = "- Task: Draft Payment Voucher. Always resolve the vendor, supplier, or payee account first using `search_account` (vendors and suppliers are level 5 accounts). Once resolved, call `draft_payment_voucher` with vendorId, vendorName, amount, remarks, and cashBankAccountId if specified.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account", "draft_payment_voucher" }
            ),
            new(
                new AiTask
                {
                    Id = "task_draft_receipt_voucher",
                    TaskName = "Draft Receipt Voucher",
                    Description = "Drafts a receipt voucher when receiving money from a customer or debtor. Customer se paise wasool hue, vasooli ki entry, payment receive karna, jama karwana, receipt voucher banana, customer recovery, cash received.",
                    RouteHint = "/daily-entries/receipt-voucher/new",
                    TaskSpecificSystemPrompt = "- Task: Draft Receipt Voucher. Always resolve the customer or payer account first using `search_account`. Once resolved, call `draft_receipt_voucher` with customerId, customerName, amount, remarks, and cashBankAccountId if specified.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account", "draft_receipt_voucher" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_account_statement",
                    TaskName = "View Account Statement / Ledger (General Ledger / Khata)",
                    Description = "Opens account statement, general ledger, khata, parchi dekhna, customer ledger, vendor ledger, bank ledger, cash statement, transactions dekhna, hisab kitab, mahine ki transactions, statement nikalna, khata check karna.",
                    RouteHint = "/reports/account-statement",
                    TaskSpecificSystemPrompt = "- Task: View Account Statement (Ledger/Khata). If user specifies an account, customer, or vendor, resolve their account ID first using `search_account`. Calculate explicit YYYY-MM-DD fromDate and toDate based on relative terms (e.g. today, this month, this year). Then call `navigate_to_report` with reportType='account_statement', account, accountTitle, fromDate, and toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account", "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_account_statement_with_due",
                    TaskName = "View Account Statement with Due Days (Aging Report)",
                    Description = "Opens account statement with due days, aging report, due date ledger, hisab kitab with overdue days, payment due statement.",
                    RouteHint = "/reports/account-statement-with-due",
                    TaskSpecificSystemPrompt = "- Task: View Account Statement with Due Days. Resolve the account ID using `search_account`. Call `navigate_to_report` with reportType='account_statement_with_due', account, fromDate, toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account", "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_account_balance",
                    TaskName = "View Account Balance Report (Schedule of Balances)",
                    Description = "Opens account balance report, schedule of balances, all control head balances, list of account balances, closing balances summary.",
                    RouteHint = "/reports/account-balance",
                    TaskSpecificSystemPrompt = "- Task: View Account Balance Report. Call `navigate_to_report` with reportType='account_balance' and date filters if provided.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_trial_balance",
                    TaskName = "View Trial Balance Report",
                    Description = "Opens trial balance report, TB, debit credit totals, trial balance summary, all accounts totals, accounting verification.",
                    RouteHint = "/reports/trial-balance",
                    TaskSpecificSystemPrompt = "- Task: View Trial Balance. Call `navigate_to_report` with reportType='trial_balance' with toDate or fromDate/toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_stock_balance",
                    TaskName = "View Stock Balance Report Screen / PDF",
                    Description = "Explicit request to open, view, or navigate to the full Stock Balance report screen or PDF (stock balance report kholo, stock report dikhao, open stock report, view stock balance, inventory report nikalna). MUST ONLY be used when user explicitly asks to view/open the report. Do NOT use for checking a single item's stock in chat.",
                    RouteHint = "/reports/stock-balance",
                    TaskSpecificSystemPrompt = "- Task: View Stock Balance Report. ONLY call `navigate_to_report` if the user explicitly asked to open, show, or navigate to the stock balance report/screen. If the user only asked about stock quantity of a specific item, do NOT navigate; provide the answer directly in chat using search_item.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report", "search_item" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_item_ledger",
                    TaskName = "View Product / Item Ledger (Inventory Movement)",
                    Description = "Opens product movement ledger, item ledger, item transactions, maal kahan se aya aur kahan gaya, product dispatch and receipt history, item card.",
                    RouteHint = "/reports/item-ledger",
                    TaskSpecificSystemPrompt = "- Task: View Product/Item Ledger. Resolve item ID using `search_item`. Call `navigate_to_report` with reportType='item_ledger', item, itemTitle, fromDate, and toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_item", "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_income_summary",
                    TaskName = "View Income Summary (Profit & Loss / P&L)",
                    Description = "Opens income summary, profit and loss report, P&L, munafa report, net income, revenue and expenses, aamdani aur kharcha summary, business profitability.",
                    RouteHint = "/reports/income-summary",
                    TaskSpecificSystemPrompt = "- Task: View Income Summary (Profit & Loss). Call `navigate_to_report` with reportType='income_summary', fromDate, and toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_balance_sheet",
                    TaskName = "View Balance Sheet",
                    Description = "Opens balance sheet, financial position, assets and liabilities, equity, karobar ki maliyat, asaasay aur wajibaat, financial health statement.",
                    RouteHint = "/reports/balance-sheet",
                    TaskSpecificSystemPrompt = "- Task: View Balance Sheet. Call `navigate_to_report` with reportType='balance_sheet' and as-of date (toDate).",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_customer_recovery",
                    TaskName = "View Customer Balance & Recovery Report",
                    Description = "Opens customer balance and recovery report, wasooli list, recovery report, customer receivables, udhaar wasooli list, pending recovery balances.",
                    RouteHint = "/reports/customer-balance-recovery",
                    TaskSpecificSystemPrompt = "- Task: View Customer Balance Recovery. Call `navigate_to_report` with reportType='customer_balance_recovery' and optional balanceFilter ('HasBalance', 'All').",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_profit_by_customer",
                    TaskName = "View Profit by Customer Report",
                    Description = "Opens profit by customer report, customer wise margins, customer profitability, kis grahak ya customer se kitna nafa munafa hua.",
                    RouteHint = "/reports/profit-by-customer",
                    TaskSpecificSystemPrompt = "- Task: View Profit by Customer. Resolve customer if specified via `search_account`. Call `navigate_to_report` with reportType='profit_by_customer', fromDate, toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account", "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_profit_by_item",
                    TaskName = "View Profit by Item Report",
                    Description = "Opens profit by item report, product wise margin, item profitability, kis item par kitna munafa nafa hua, product profit analysis.",
                    RouteHint = "/reports/profit-by-item",
                    TaskSpecificSystemPrompt = "- Task: View Profit by Item. Resolve item if specified via `search_item`. Call `navigate_to_report` with reportType='profit_by_item', fromDate, toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_item", "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_purchase_supply_comparison",
                    TaskName = "View Purchase vs Supply Comparison Report",
                    Description = "Opens purchase vs supply comparison report, milk supply comparison, khareed aur supply ka farq, procurement vs dispatch variance.",
                    RouteHint = "/reports/purchase-supply-comparison",
                    TaskSpecificSystemPrompt = "- Task: View Purchase vs Supply Comparison. Call `navigate_to_report` with reportType='purchase_supply_comparison', fromDate, toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_customer_bill",
                    TaskName = "View Customer Bill (Thermal Statement)",
                    Description = "Opens customer bill report, thermal statement, grahak ka bill, customer print receipt, bill nikalna.",
                    RouteHint = "/reports/customer-bill",
                    TaskSpecificSystemPrompt = "- Task: View Customer Bill. Resolve customer ID using `search_account`. Call `navigate_to_report` with reportType='customer_bill', account, accountTitle.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account", "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_report_manufacturing",
                    TaskName = "View Manufacturing & Production Report",
                    Description = "Opens manufacturing report, production report, karkhana report, factory output, BOM and production cost report.",
                    RouteHint = "/reports/manufacturing",
                    TaskSpecificSystemPrompt = "- Task: View Manufacturing Report. Call `navigate_to_report` with reportType='manufacturing', fromDate, toDate.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_view_reports",
                    TaskName = "View Business & Financial Reports",
                    Description = "Opens business reports menu or general overview when no specific report is requested.",
                    RouteHint = "/reports",
                    TaskSpecificSystemPrompt = "- Task: Reports Menu. Guide user to specific reports or call `navigate_to_report` with reportType='account_statement'.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account", "navigate_to_report" }
            ),
            new(
                new AiTask
                {
                    Id = "task_check_balance_search",
                    TaskName = "Check Account Balance & Search",
                    Description = "Checks balance of customer, vendor, supplier, cash, bank, or ledger account in chat (kitna balance hai, kitne paise bante hain, dena hai ya lena hai, kitna baqi hai, payables, receivables, kitne lene hain, kitne dene hain, test ka balance). Replies directly in chat without opening any report.",
                    RouteHint = "/setup/chart-of-accounts",
                    TaskSpecificSystemPrompt = "- Task: Balance Inquiry & Account Search. Call `search_account` with includeBalance=true. Provide multiple name variations in the `names` array. Clearly explain the current balance in chat, indicating whether it is receivable (Debit) or payable (Credit). Do NOT open any report screen.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_account" }
            ),
            new(
                new AiTask
                {
                    Id = "task_view_voucher_listings",
                    TaskName = "View Voucher & Sales Listings",
                    Description = "Views or filters payment listings, receipt listings, or sales invoices. Payment vouchers dekhna, receipt vouchers list, sales bills dekhna, past invoices, transaction history list, voucher search.",
                    RouteHint = "/daily-entries/payment-voucher",
                    TaskSpecificSystemPrompt = "- Task: Voucher & Sales Listings. Call the appropriate listing tool (`filter_payment_listing`, `filter_receipt_listing`, or `filter_sale_listing`) with explicit YYYY-MM-DD dates and filter criteria.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "filter_payment_listing", "filter_receipt_listing", "filter_sale_listing", "search_account" }
            ),
            new(
                new AiTask
                {
                    Id = "task_search_inventory",
                    TaskName = "Search Inventory & Item Stock Balance",
                    Description = "Checking item stock quantity, available inventory balance, asking how much stock is left for an item, product price, item inquiry in chat (daal chana ka stock kitna hai, tikka masala kitna pada hai, maal kitna bacha hai, stock kitna hai, kitne carton ya packets hain, item stock inquiry). Replies directly in chat without opening any report.",
                    RouteHint = "/setup/item-details",
                    TaskSpecificSystemPrompt = "- Task: Search Inventory & Item Stock Balance. Always call `search_item` with product keywords and set `includeStockBalance=true` when asked about stock/quantity/units. Always answer directly in chat with the item name, available stock quantity, unit, and whether in stock or out of stock. NEVER call navigate_to_report or open any screen unless the user explicitly said 'report kholo' or 'open report'.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string> { "search_item" }
            ),
            new(
                new AiTask
                {
                    Id = "task_general_assistant",
                    TaskName = "General Assistant",
                    Description = "General queries, greetings, how-to questions, help, guidance about RetailSuite, conversational assistance without specialized tools.",
                    RouteHint = null,
                    TaskSpecificSystemPrompt = "- Task: General Assistant. Answer conversationally, concisely, and helpfully about RetailSuite capabilities.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new List<string>()
            )
        };
    }
}
