using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        _tenantDbContext = tenantDbContext;
        _logger = logger;
    }

    public async Task<AiAssistantResponse> ProcessPromptAsync(AiAssistantRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new AiAssistantResponse { Success = false, ErrorMessage = "Prompt cannot be empty.", ReplyText = "Prompt cannot be empty." };
        }

        var correlationId = Guid.NewGuid().ToString("N");

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

        // Fast In-Memory / Database Tool Routing without blocking on slow embedding network calls
        var targetedTools = await _toolRouterService.GetTargetedToolsAsync(request.CurrentRoute, null, request.Prompt, cancellationToken);

        // Always ensure all core ERP tools are available to Gemini
        EnsureToolAvailable(targetedTools, "search_vendor", "search_vendor",
            "Searches for vendors/suppliers in database by possible names or keywords. Set includeBalance=true to retrieve their current balance.",
            """{"type":"object","properties":{"names":{"type":"array","items":{"type":"string"},"description":"List of possible vendor names, search keywords, or variations (e.g. ['test', 'test vendor'])"},"name":{"type":"string","description":"Vendor name or search keyword"},"includeBalance":{"type":"boolean","description":"Include ledger balance"}}}""");

        EnsureToolAvailable(targetedTools, "search_account", "search_account",
            "Searches accounts in the Chart of Accounts (customers, vendors, cash, bank). Set includeBalance=true to retrieve current balance.",
            """{"type":"object","properties":{"names":{"type":"array","items":{"type":"string"},"description":"List of possible account names, search keywords, or variations (e.g. ['test', 'test account'])"},"name":{"type":"string","description":"Account or customer name to search"},"includeBalance":{"type":"boolean","description":"Include ledger balance"}}}""");

        EnsureToolAvailable(targetedTools, "filter_payment_listing", "filter_payment_listing",
            "Filters and navigates to the payment voucher listing screen by date range, cash/bank account, or voucher number.",
            """{"type":"object","properties":{"fromDate":{"type":"string","description":"Start date YYYY-MM-DD"},"toDate":{"type":"string","description":"End date YYYY-MM-DD"},"cashBankAccount":{"type":"string","description":"Cash/Bank account"},"voucherNo":{"type":"string","description":"Voucher number"}}}""");

        EnsureToolAvailable(targetedTools, "draft_payment_voucher", "draft_payment_voucher",
            "Drafts a new payment voucher for a vendor, supplier, or expense with pre-filled amount and remarks.",
            """{"type":"object","properties":{"vendorId":{"type":"string","description":"Resolved Vendor ID"},"vendorName":{"type":"string","description":"Vendor name"},"amount":{"type":"number","description":"Payment amount"},"remarks":{"type":"string","description":"Remarks"},"cashBankAccountId":{"type":"string","description":"Account ID to pay from"}},"required":["amount"]}""");

        EnsureToolAvailable(targetedTools, "filter_receipt_listing", "filter_receipt_listing",
            "Filters and navigates to the receipt voucher listing screen by date range, cash/bank account, or voucher number.",
            """{"type":"object","properties":{"fromDate":{"type":"string","description":"Start date YYYY-MM-DD"},"toDate":{"type":"string","description":"End date YYYY-MM-DD"},"cashBankAccount":{"type":"string","description":"Cash/Bank account"},"voucherNo":{"type":"string","description":"Voucher number"}}}""");

        EnsureToolAvailable(targetedTools, "draft_receipt_voucher", "draft_receipt_voucher",
            "Drafts a new receipt voucher from a customer/debtor with pre-filled amount and remarks.",
            """{"type":"object","properties":{"customerId":{"type":"string","description":"Customer or account ID"},"customerName":{"type":"string","description":"Customer name"},"amount":{"type":"number","description":"Amount received"},"remarks":{"type":"string","description":"Remarks"},"cashBankAccountId":{"type":"string","description":"Cash/Bank receiving account"}},"required":["amount"]}""");

        EnsureToolAvailable(targetedTools, "filter_sale_listing", "filter_sale_listing",
            "Filters and navigates to the sales listing screen by date range, customer, or invoice number.",
            """{"type":"object","properties":{"fromDate":{"type":"string","description":"Start date YYYY-MM-DD"},"toDate":{"type":"string","description":"End date YYYY-MM-DD"},"customerId":{"type":"string","description":"Customer ID"}}}""");

        // 3. Prune and scrub conversational history
        var prunedHistory = _contextPrunerService.PruneAndScrubHistory(request.History);

        // 4. Build Gemini Tool Declarations
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

        var systemInstructions = $@"You are the official voice and text AI Assistant for RetailSuite ERP.
Current UTC Date: {DateTime.UtcNow:yyyy-MM-dd (dddd)}.
Active Tenant ID: {_currentTenant.Id ?? "default"}.
Current Screen Route: {request.CurrentRoute ?? "/"}.

CAPABILITIES & RULES:
1. Navigation: When user asks to go to or view a list (e.g. payment listing, receipt listing, sales):
   - Call the appropriate filter/navigation tool (filter_payment_listing, filter_receipt_listing, filter_sale_listing).
   - Calculate explicit YYYY-MM-DD dates for relative terms like 'today', 'yesterday', 'this month', 'last week'.
2. Balance Inquiries & Search: When user asks for the balance of any customer, vendor, bank, cash, or expense account (e.g., 'what is the balance of test account?', 'kitna balance hai?'):
   - Call search_account or search_vendor with includeBalance=true.
   - ALWAYS provide multiple possible names/variations in the `names` array (for instance, if user asks for 'test account', pass names: ['test', 'test account']; if user asks for 'ABC vendor', pass names: ['ABC', 'ABC vendor']) so all matching accounts are retrieved in the very first turn!
   - Reply directly to the user with the exact account title and balance formatted clearly (e.g., 'The current balance of ABC is Rs. 15,000 (Receivable)').
3. Data Entry & Drafting:
   - Payments: When user asks to pay or create a payment voucher, FIRST call search_vendor to resolve their ID, then call draft_payment_voucher.
   - Receipts: When user asks to record received money or create a receipt voucher, FIRST call search_account or search_vendor to resolve customer/account ID, then call draft_receipt_voucher.
4. Language & Presentation:
   - STRICT LANGUAGE MATCH: You must detect and reply in the EXACT language of the user's latest message.
   - If user asks in English (e.g. 'what is balance of test account', 'show receipt vouchers'), reply 100% in English. NEVER mix Roman Urdu or Hindi words (such as 'kya aap', 'taake', 'shukriya', 'mazeed').
   - If user asks in Roman Urdu (e.g. 'test ka balance kitna hai', 'payment voucher dikhao'), reply in Roman Urdu.
   - If user asks in Urdu script (e.g. 'بیلنس بتائیں'), reply in Urdu script.
   - Visual presentation: Always format lists using markdown bullet points (- ) and bold account names or key figures (**name**, **Rs. 500**).
   - Keep spoken replies concise, friendly, and natural for text-to-speech.
5. Human in the loop: Do not claim you saved vouchers permanently; state that you have drafted it for their review.";

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
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            // If Gemini invoked a function call
            if (functionCall.HasValue)
            {
                var modelTurnContent = JsonSerializer.Deserialize<object>(candidate.GetProperty("content").GetRawText());
                var args = functionCall.Value.GetProperty("args");

                // Check if it's a backend read tool
                if (fnName == "search_vendor")
                {
                    var vendorNames = ExtractNamesFromArgs(args);
                    var includeBal = args.TryGetProperty("includeBalance", out var ib) && ib.GetBoolean();
                    var vendors = await SearchVendorsAsync(vendorNames, includeBal, cancellationToken);

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
                                    response = new { results = vendors }
                                }
                            }
                        }
                    });

                    continue;
                }

                if (fnName == "search_account")
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

                // If it's a client action tool:
                if (fnName == "filter_payment_listing")
                {
                    var speech = !string.IsNullOrWhiteSpace(modelText)
                        ? modelText
                        : "Navigating to payment listing with your requested filters.";

                    return new AiAssistantResponse
                    {
                        ActionType = AiActionType.Navigate,
                        TargetRoute = "/daily-entries/payment-voucher",
                        ActionPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()),
                        ReplyText = speech
                    };
                }

                if (fnName == "draft_payment_voucher")
                {
                    var speech = !string.IsNullOrWhiteSpace(modelText)
                        ? modelText
                        : "I've drafted the payment voucher for you. Please review and save.";

                    return new AiAssistantResponse
                    {
                        ActionType = AiActionType.DraftVoucher,
                        TargetRoute = "/daily-entries/payment-voucher/new",
                        ActionPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()),
                        ReplyText = speech
                    };
                }

                if (fnName == "filter_receipt_listing")
                {
                    var speech = !string.IsNullOrWhiteSpace(modelText)
                        ? modelText
                        : "Navigating to receipt listing with your requested filters.";

                    return new AiAssistantResponse
                    {
                        ActionType = AiActionType.Navigate,
                        TargetRoute = "/daily-entries/receipt-voucher",
                        ActionPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()),
                        ReplyText = speech
                    };
                }

                if (fnName == "draft_receipt_voucher")
                {
                    var speech = !string.IsNullOrWhiteSpace(modelText)
                        ? modelText
                        : "I've drafted the receipt voucher for you. Please review and save.";

                    return new AiAssistantResponse
                    {
                        ActionType = AiActionType.DraftVoucher,
                        TargetRoute = "/daily-entries/receipt-voucher/new",
                        ActionPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()),
                        ReplyText = speech
                    };
                }

                if (fnName == "filter_sale_listing")
                {
                    var speech = !string.IsNullOrWhiteSpace(modelText)
                        ? modelText
                        : "Opening sales records.";

                    return new AiAssistantResponse
                    {
                        ActionType = AiActionType.Navigate,
                        TargetRoute = "/daily-entries/sale",
                        ActionPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText()),
                        ReplyText = speech
                    };
                }
            }

            // Normal conversational answer (e.g. balance inquiry answered)
            if (!string.IsNullOrWhiteSpace(modelText))
            {
                return new AiAssistantResponse
                {
                    ActionType = AiActionType.Answer,
                    ReplyText = modelText
                };
            }
        }

        return new AiAssistantResponse
        {
            Success = !string.IsNullOrWhiteSpace(lastModelText),
            ActionType = AiActionType.Answer,
            ReplyText = !string.IsNullOrWhiteSpace(lastModelText)
                ? lastModelText
                : "I've processed your request. Please check your accounts or try rephrasing."
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

    private async Task<List<object>> SearchVendorsAsync(List<string> names, bool includeBalance, CancellationToken cancellationToken)
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
