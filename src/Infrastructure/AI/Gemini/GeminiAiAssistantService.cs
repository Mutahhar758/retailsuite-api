using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Domain.Legacy;
using Retailer.Domain.Multitenancy;
using Retailer.Infrastructure.AI.ContextPruner;
using Retailer.Infrastructure.AI.ToolRouter;
using Retailer.Infrastructure.Multitenancy;

namespace Retailer.Infrastructure.AI.Gemini;

public class GeminiAiAssistantService : IAiAssistantService
{
    private readonly IConfiguration _config;
    private readonly ICurrentTenant _currentTenant;
    private readonly IGeminiEmbeddingService _embeddingService;
    private readonly IToolRouterService _toolRouterService;
    private readonly IContextPrunerService _contextPrunerService;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly IRepository<GlEntry> _glRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<ItemTransaction> _itemTransactionRepository;
    private readonly TenantDbContext _tenantDbContext;
    private readonly ILogger<GeminiAiAssistantService> _logger;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(40) };

    public GeminiAiAssistantService(
        IConfiguration config,
        ICurrentTenant currentTenant,
        IGeminiEmbeddingService embeddingService,
        IToolRouterService toolRouterService,
        IContextPrunerService contextPrunerService,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        IRepository<GlEntry> glRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<ItemTransaction> itemTransactionRepository,
        TenantDbContext tenantDbContext,
        ILogger<GeminiAiAssistantService> logger)
    {
        _config = config;
        _currentTenant = currentTenant;
        _embeddingService = embeddingService;
        _toolRouterService = toolRouterService;
        _contextPrunerService = contextPrunerService;
        _chartOfAccountRepository = chartOfAccountRepository;
        _glRepository = glRepository;
        _itemRepository = itemRepository;
        _itemTransactionRepository = itemTransactionRepository;
        _tenantDbContext = tenantDbContext;
        _logger = logger;
    }

    public async Task<AiAssistantResponse> ProcessPromptAsync(AiAssistantRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new AiAssistantResponse { Success = false, ErrorMessage = "Prompt cannot be empty.", ReplyText = "Prompt cannot be empty." };
        }

        // UUIDv7 (RFC 9562): Time-ordered UUID with millisecond precision timestamp
        // Standard sortable format: 0192df4b-7013-7186-9032-45e69e2c694a
        var correlationId = Guid.CreateVersion7().ToString();

        try
        {
            return await ExecuteAgentLoopAsync(request, correlationId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("AI Assistant chat request was canceled by client.");
            return new AiAssistantResponse
            {
                Success = false,
                ErrorMessage = "Request was canceled by client.",
                ReplyText = "Request was canceled."
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "AI request timed out after {Timeout} seconds.", _httpClient.Timeout.TotalSeconds);
            return new AiAssistantResponse
            {
                Success = false,
                ErrorMessage = $"AI request timed out after {_httpClient.Timeout.TotalSeconds} seconds.",
                ReplyText = "AI service timed out. Please try again."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP connection error while communicating with AI service: {Message}", ex.Message);
            return new AiAssistantResponse
            {
                Success = false,
                ErrorMessage = $"AI service connection error: {ex.Message}",
                ReplyText = "Could not connect to AI service. Please check network connection."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AI assistant: {Message}", ex.Message);
            return new AiAssistantResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ReplyText = $"AI assistant error: {ex.Message}"
            };
        }
    }

    private async Task<AiAssistantResponse> ExecuteAgentLoopAsync(AiAssistantRequest request, string correlationId, CancellationToken cancellationToken)
    {
        var geminiSection = _config.GetSection("GeminiSettings");
        var apiKey = new[]
        {
            geminiSection["ApiKey"],
            _config["GeminiSettings:ApiKey"],
            _config["Gemini:ApiKey"],
            _config["GeminiApiKey"],
            Environment.GetEnvironmentVariable("GEMINI_API_KEY"),
            Environment.GetEnvironmentVariable("GeminiSettings__ApiKey"),
            Environment.GetEnvironmentVariable("Gemini__ApiKey"),
            Environment.GetEnvironmentVariable("GeminiApiKey")
        }.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("AI API key is not configured.");
            return new AiAssistantResponse
            {
                Success = false,
                ErrorMessage = "AI API key is not configured.",
                ReplyText = "AI service key is not configured. Please set the API key in the server configuration."
            };
        }

        var model = !string.IsNullOrWhiteSpace(geminiSection["Model"]) && geminiSection["Model"] != "gemini-2.5-flash"
            ? geminiSection["Model"]
            : "gemini-3.5-flash-lite";

        var baseEndpoint = (geminiSection["Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models").TrimEnd('/');
        var endpoint = $"{baseEndpoint}/{model}:generateContent?key={apiKey}";
        var cleanEndpoint = $"{baseEndpoint}/{model}:generateContent";

        // Fast In-Memory / Database Task & Tool Routing (Vector search on task intent + screen route anchor)
        var routedTask = await _toolRouterService.RouteTaskAsync(request.CurrentRoute, null, request.Prompt, cancellationToken);
        var targetedTools = routedTask.Tools;

        // Fallback safety if no tools were matched and not explicitly general assistant
        if (targetedTools.Count == 0 && routedTask.MatchedTask?.Id != "task_general_assistant")
        {
            EnsureToolAvailable(targetedTools, "search_account", "search_account",
                "Searches accounts in the Chart of Accounts (customers, vendors, cash, bank). Set includeBalance=true to retrieve current balance.",
                """{"type":"object","properties":{"names":{"type":"array","items":{"type":"string"},"description":"List of possible account names, search keywords, or variations"},"name":{"type":"string","description":"Account or customer name to search"},"includeBalance":{"type":"boolean","description":"Include ledger balance"}}}""");

            EnsureToolAvailable(targetedTools, "navigate_to_report", "navigate_to_report",
                "Navigates to a specific business or accounting report in RetailSuite with pre-filled parameters (dates, accounts, items, filters).",
                """{"type":"object","properties":{"reportType":{"type":"string","description":"Type of report to open: 'account_statement', 'trial_balance', 'stock_balance', 'item_ledger'"},"account":{"type":"string","description":"Resolved account ID"},"fromDate":{"type":"string","description":"Start date YYYY-MM-DD"},"toDate":{"type":"string","description":"End date YYYY-MM-DD"}},"required":["reportType"]}""");
        }

        // 3. Prune and scrub conversational history (JSON Scrubber)
        var prunedHistory = _contextPrunerService.PruneAndScrubHistory(request.History);

        // 4. Build Gemini Tool Declarations (Only the targeted tools for this task)
        var functionDeclarations = new List<object>();
        foreach (var tool in targetedTools)
        {
            try
            {
                using var schemaDoc = JsonDocument.Parse(tool.JsonSchema);
                var schemaObj = JsonSerializer.Deserialize<object>(tool.JsonSchema);
                functionDeclarations.Add(new
                {
                    name = tool.ToolName,
                    description = tool.Description,
                    parameters = schemaObj
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse schema for tool {ToolName}", tool.ToolName);
            }
        }

        // 5. Dynamic Task-Specific System Instructions
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are the official voice and text AI Assistant for RetailSuite ERP.");
        promptBuilder.AppendLine($"Current UTC Date: {DateTime.UtcNow:yyyy-MM-dd (dddd)}.");
        promptBuilder.AppendLine($"Active Tenant ID: {_currentTenant.Id ?? "default"}.");
        if (!string.IsNullOrWhiteSpace(request.CurrentRoute))
        {
            promptBuilder.AppendLine($"Current Screen: {request.CurrentRoute}.");
        }
        promptBuilder.AppendLine("Always reply strictly in the exact same language and script used by the user in their prompt (English, Roman Urdu, Urdu, etc.).");
        promptBuilder.AppendLine("Format responses cleanly using markdown bullet points and bold key figures or names. Keep spoken replies concise and natural for speech.");
        promptBuilder.AppendLine("IMPORTANT: When a user asks a question about an account balance or item stock quantity (e.g. 'kitna balance hai?', 'stock kitna hai?', 'daal chana ka stock kitna hai?'), reply directly in chat using search_account or search_item(includeStockBalance=true). NEVER call navigate_to_report unless the user explicitly used words like 'report', 'statement', 'ledger', 'kholo', 'open', 'dikhao', 'show report', or 'print'.");

        if (!string.IsNullOrWhiteSpace(routedTask.TaskSpecificSystemPrompt))
        {
            promptBuilder.AppendLine(routedTask.TaskSpecificSystemPrompt);
        }

        var systemInstructions = promptBuilder.ToString();

        // Build Contents array
        var contents = new List<object>();

        foreach (var msg in prunedHistory)
        {
            var role = msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) || msg.Role.Equals("model", StringComparison.OrdinalIgnoreCase)
                ? "model"
                : "user";

            contents.Add(new
            {
                role,
                parts = new[] { new { text = msg.Content } }
            });
        }

        var activeFilterSummary = request.ActiveFilters != null && request.ActiveFilters.Count > 0
            ? $" [Active Screen Filters: {JsonSerializer.Serialize(request.ActiveFilters)}]"
            : "";

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = $"[Current Screen: {request.CurrentRoute ?? "/"}{activeFilterSummary}] {request.Prompt}" } }
        });

        // 5. Agentic Recursive Loop (up to 5 tool execution turns)
        int maxLoopTurns = 5;
        int turnIndex = 0;
        string? lastModelText = null;
        AiActionType? pendingActionType = null;
        string? pendingTargetRoute = null;
        Dictionary<string, object>? pendingActionPayload = null;

        while (maxLoopTurns-- > 0)
        {
            turnIndex++;
            var requestBody = new
            {
                contents,
                systemInstruction = new
                {
                    parts = new[] { new { text = systemInstructions } }
                },
                tools = new[]
                {
                    new { functionDeclarations }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 800
                }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var jsonContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var stopwatch = Stopwatch.StartNew();
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, error);

                await LogGeminiApiCallAsync(new GeminiApiLog
                {
                    CorrelationId = correlationId,
                    TenantId = _currentTenant?.Id,
                    Source = "AiAssistant",
                    TurnIndex = turnIndex,
                    Model = model,
                    Endpoint = cleanEndpoint,
                    Prompt = request.Prompt,
                    CurrentRoute = request.CurrentRoute,
                    RequestBody = requestJson,
                    ResponseBody = error,
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = error,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                return new AiAssistantResponse
                {
                    Success = false,
                    ErrorMessage = $"AI API error ({response.StatusCode}): {error}",
                    ReplyText = $"AI request failed ({response.StatusCode})."
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            int? inputTokens = null;
            int? outputTokens = null;
            int? totalTokens = null;

            if (doc.RootElement.TryGetProperty("usageMetadata", out var usageEl))
            {
                if (usageEl.TryGetProperty("promptTokenCount", out var pt) && pt.TryGetInt32(out var ptVal))
                    inputTokens = ptVal;
                if (usageEl.TryGetProperty("candidatesTokenCount", out var ct) && ct.TryGetInt32(out var ctVal))
                    outputTokens = ctVal;
                if (usageEl.TryGetProperty("totalTokenCount", out var tt) && tt.TryGetInt32(out var ttVal))
                    totalTokens = ttVal;
            }

            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                await LogGeminiApiCallAsync(new GeminiApiLog
                {
                    CorrelationId = correlationId,
                    TenantId = _currentTenant?.Id,
                    Source = "AiAssistant",
                    TurnIndex = turnIndex,
                    Model = model,
                    Endpoint = cleanEndpoint,
                    Prompt = request.Prompt,
                    CurrentRoute = request.CurrentRoute,
                    RequestBody = requestJson,
                    ResponseBody = responseJson,
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    TotalTokens = totalTokens,
                    ErrorMessage = "No candidates returned by Gemini.",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);

                return new AiAssistantResponse { Success = false, ErrorMessage = "No response from Gemini." };
            }

            var candidate = candidates[0];
            var parts = candidate.GetProperty("content").GetProperty("parts");

            string? modelText = null;
            JsonElement? functionCall = null;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textEl))
                {
                    modelText = textEl.GetString();
                    if (!string.IsNullOrWhiteSpace(modelText))
                    {
                        lastModelText = modelText;
                    }
                }

                if (part.TryGetProperty("functionCall", out var fnEl))
                {
                    functionCall = fnEl;
                }
            }

            var rawFnName = functionCall.HasValue && functionCall.Value.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() ?? ""
                : "";
            var fnName = rawFnName.Contains(':') ? rawFnName.Split(':').Last() : rawFnName;

            await LogGeminiApiCallAsync(new GeminiApiLog
            {
                CorrelationId = correlationId,
                TenantId = _currentTenant?.Id,
                Source = "AiAssistant",
                TurnIndex = turnIndex,
                Model = model,
                Endpoint = cleanEndpoint,
                Prompt = request.Prompt,
                CurrentRoute = request.CurrentRoute,
                RequestBody = requestJson,
                ResponseBody = responseJson,
                StatusCode = (int)response.StatusCode,
                IsSuccess = true,
                DurationMs = stopwatch.ElapsedMilliseconds,
                ToolCalls = string.IsNullOrEmpty(rawFnName) ? null : rawFnName,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = totalTokens,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            // If Gemini invoked a function call
            if (functionCall.HasValue)
            {
                var modelTurnContent = JsonSerializer.Deserialize<object>(candidate.GetProperty("content").GetRawText());
                var args = functionCall.Value.GetProperty("args");

                // Check if it's a backend read tool
                if (fnName == "search_account" || fnName == "search_vendor")
                {
                    var accountNames = ExtractNamesFromArgs(args);
                    var includeBal = args.TryGetProperty("includeBalance", out var ib) && ib.GetBoolean();
                    var accounts = await SearchAccountsAsync(accountNames, includeBal, cancellationToken);

                    contents.Add(modelTurnContent!);

                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new { results = accounts }
                                }
                            }
                        }
                    });

                    continue;
                }

                if (fnName == "search_item")
                {
                    var itemNames = ExtractNamesFromArgs(args);
                    var includeStock = args.TryGetProperty("includeStockBalance", out var isb) && isb.GetBoolean();
                    var items = await SearchItemsAsync(itemNames, includeStock, cancellationToken);

                    contents.Add(modelTurnContent!);

                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new { results = items }
                                }
                            }
                        }
                    });

                    continue;
                }

                if (fnName == "navigate_to_report")
                {
                    var reportType = args.TryGetProperty("reportType", out var rt) ? rt.GetString() : "";
                    var targetRoute = reportType switch
                    {
                        "account_statement" => "/reports/account-statement",
                        "account_statement_with_due" => "/reports/account-statement-with-due",
                        "account_balance" => "/reports/account-balance",
                        "trial_balance" => "/reports/trial-balance",
                        "stock_balance" => "/reports/stock-balance",
                        "item_ledger" => "/reports/item-ledger",
                        "income_summary" => "/reports/income-summary",
                        "balance_sheet" => "/reports/balance-sheet",
                        "customer_balance_recovery" => "/reports/customer-balance-recovery",
                        "profit_by_customer" => "/reports/profit-by-customer",
                        "profit_by_item" => "/reports/profit-by-item",
                        "purchase_supply_comparison" => "/reports/purchase-supply-comparison",
                        "customer_bill" => "/reports/customer-bill",
                        "manufacturing" => "/reports/manufacturing",
                        _ => "/reports/account-statement"
                    };

                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()) ?? new();
                    pendingActionType = AiActionType.Navigate;
                    pendingTargetRoute = targetRoute;
                    pendingActionPayload = payload;

                    contents.Add(modelTurnContent!);
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new { status = "success", report = reportType, target = targetRoute }
                                }
                            }
                        }
                    });

                    continue;
                }

                // If it's a client action tool:
                if (fnName == "filter_payment_listing")
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()) ?? new();
                    pendingActionType = AiActionType.Navigate;
                    pendingTargetRoute = "/daily-entries/payment-voucher";
                    pendingActionPayload = payload;

                    contents.Add(modelTurnContent!);
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new { status = "success", target = pendingTargetRoute }
                                }
                            }
                        }
                    });

                    continue;
                }

                if (fnName == "draft_payment_voucher")
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()) ?? new();
                    pendingActionType = AiActionType.DraftVoucher;
                    pendingTargetRoute = "/daily-entries/payment-voucher/new";
                    pendingActionPayload = payload;

                    contents.Add(modelTurnContent!);
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new
                                    {
                                        status = "drafted",
                                        vendor = payload.GetValueOrDefault("vendorName"),
                                        amount = payload.GetValueOrDefault("amount")
                                    }
                                }
                            }
                        }
                    });

                    continue;
                }

                if (fnName == "filter_receipt_listing")
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()) ?? new();
                    pendingActionType = AiActionType.Navigate;
                    pendingTargetRoute = "/daily-entries/receipt-voucher";
                    pendingActionPayload = payload;

                    contents.Add(modelTurnContent!);
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new { status = "success", target = pendingTargetRoute }
                                }
                            }
                        }
                    });

                    continue;
                }

                if (fnName == "draft_receipt_voucher")
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()) ?? new();
                    pendingActionType = AiActionType.DraftVoucher;
                    pendingTargetRoute = "/daily-entries/receipt-voucher/new";
                    pendingActionPayload = payload;

                    contents.Add(modelTurnContent!);
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new
                                    {
                                        status = "drafted",
                                        customer = payload.GetValueOrDefault("customerName"),
                                        amount = payload.GetValueOrDefault("amount")
                                    }
                                }
                            }
                        }
                    });

                    continue;
                }

                if (fnName == "filter_sale_listing")
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()) ?? new();
                    pendingActionType = AiActionType.Navigate;
                    pendingTargetRoute = "/daily-entries/sale";
                    pendingActionPayload = payload;

                    contents.Add(modelTurnContent!);
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = rawFnName,
                                    response = new { status = "success", target = pendingTargetRoute }
                                }
                            }
                        }
                    });

                    continue;
                }
            }

            // Normal conversational answer or final response after tool execution
            if (!string.IsNullOrWhiteSpace(modelText))
            {
                return new AiAssistantResponse
                {
                    Success = true,
                    ActionType = pendingActionType ?? AiActionType.Answer,
                    TargetRoute = pendingTargetRoute,
                    ActionPayload = pendingActionPayload,
                    ReplyText = modelText
                };
            }
        }

        return new AiAssistantResponse
        {
            Success = !string.IsNullOrWhiteSpace(lastModelText),
            ActionType = pendingActionType ?? AiActionType.Answer,
            TargetRoute = pendingTargetRoute,
            ActionPayload = pendingActionPayload,
            ReplyText = !string.IsNullOrWhiteSpace(lastModelText)
                ? lastModelText
                : "Command processed."
        };
    }

    private async Task LogGeminiApiCallAsync(GeminiApiLog log, CancellationToken cancellationToken)
    {
        try
        {
            _tenantDbContext.GeminiApiLogs.Add(log);
            await _tenantDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist GeminiApiLog (Turn {Turn})", log.TurnIndex);
        }
    }

    private static List<string> ExtractNamesFromArgs(JsonElement args)
    {
        var names = new List<string>();
        if (args.TryGetProperty("names", out var namesArray) && namesArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in namesArray.EnumerateArray())
            {
                var s = el.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    var trimmed = s.Trim();
                    if (!names.Contains(trimmed)) names.Add(trimmed);
                }
            }
        }

        if (args.TryGetProperty("name", out var singleName))
        {
            var s = singleName.GetString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                var trimmed = s.Trim();
                if (!names.Contains(trimmed)) names.Add(trimmed);
            }
        }

        return names;
    }

    private async Task<List<object>> SearchAccountsAsync(List<string> names, bool includeBalance, CancellationToken cancellationToken)
    {
        if (names == null || names.Count == 0) return new List<object>();

        var foundItems = new Dictionary<string, string>(); // Id -> Title

        foreach (var rawName in names)
        {
            var keyword = rawName.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(keyword)) continue;

            var matches = await _chartOfAccountRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.AccLevel == 5 && (x.Title!.ToLower().Contains(keyword) || x.Id.ToLower().Contains(keyword)))
                .Take(6)
                .Select(x => new { id = x.Id, name = x.Title })
                .ToListAsync(cancellationToken);

            foreach (var m in matches)
            {
                if (!foundItems.ContainsKey(m.id))
                {
                    foundItems[m.id] = m.name;
                }
            }

            if (foundItems.Count >= 10) break;
        }

        var list = foundItems.Select(x => new { id = x.Key, name = x.Value }).Take(10).ToList();
        if (!includeBalance)
        {
            return list.Cast<object>().ToList();
        }

        var results = new List<object>();
        foreach (var item in list)
        {
            var dr = await _glRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.DrAccountId == item.id)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

            var cr = await _glRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.CrAccountId == item.id)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;

            var balance = dr - cr;
            results.Add(new
            {
                id = item.id,
                name = item.name,
                currentBalance = balance,
                balanceType = balance > 0 ? "Debit (Receivable)" : balance < 0 ? "Credit (Payable)" : "Zero Balance"
            });
        }
        return results;
    }

    private async Task<List<object>> SearchItemsAsync(List<string> names, bool includeStockBalance, CancellationToken cancellationToken)
    {
        if (names == null || names.Count == 0) return new List<object>();

        var foundItems = new Dictionary<string, (string title, decimal price, string? unit)>();

        foreach (var rawName in names)
        {
            var keyword = rawName.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(keyword)) continue;

            var matches = await _itemRepository.GetAll()
                .AsNoTracking()
                .Include(x => x.PrimaryUnit)
                .Where(x => x.Title.ToLower().Contains(keyword) || (x.Barcode != null && x.Barcode.ToLower().Contains(keyword)) || (x.ItemKey != null && x.ItemKey.ToLower().Contains(keyword)))
                .Take(6)
                .Select(x => new { id = x.Id, title = x.Title, price = x.PriRate, unit = x.PrimaryUnit != null ? x.PrimaryUnit.Title : null })
                .ToListAsync(cancellationToken);

            foreach (var m in matches)
            {
                if (!foundItems.ContainsKey(m.id))
                {
                    foundItems[m.id] = (m.title, m.price, m.unit);
                }
            }

            if (foundItems.Count >= 10) break;
        }

        var list = foundItems.Select(x => new
        {
            id = x.Key,
            title = x.Value.title,
            price = x.Value.price,
            unit = x.Value.unit
        }).Take(10).ToList();

        if (!includeStockBalance)
        {
            return list.Cast<object>().ToList();
        }

        var results = new List<object>();
        foreach (var item in list)
        {
            var inQty = await _itemTransactionRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.ItemId == item.id)
                .SumAsync(x => (decimal?)x.QtyIn, cancellationToken) ?? 0;

            var outQty = await _itemTransactionRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.ItemId == item.id)
                .SumAsync(x => (decimal?)x.QtyOut, cancellationToken) ?? 0;

            var stockBalance = inQty - outQty;

            results.Add(new
            {
                id = item.id,
                title = item.title,
                price = item.price,
                unit = item.unit,
                stockBalance = stockBalance,
                stockStatus = stockBalance <= 0 ? "Out of Stock" : "In Stock"
            });
        }

        return results;
    }

    private static void EnsureToolAvailable(List<AiTool> tools, string id, string name, string description, string schema)
    {
        if (!tools.Any(t => t.Id == id))
        {
            tools.Add(new AiTool
            {
                Id = id,
                ToolName = name,
                Description = description,
                JsonSchema = schema
            });
        }
    }
}
