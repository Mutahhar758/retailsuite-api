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

namespace Retailer.Infrastructure.AI.Gemini;

public class GeminiAiAssistantService : IAiAssistantService
{
    private readonly IConfiguration _config;
    private readonly ICurrentTenant _currentTenant;
    private readonly IGeminiEmbeddingService _embeddingService;
    private readonly IToolRouterService _toolRouterService;
    private readonly IContextPrunerService _contextPrunerService;
    private readonly IRepository<ChartOfAccount> _chartOfAccountRepository;
    private readonly ILogger<GeminiAiAssistantService> _logger;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public GeminiAiAssistantService(
        IConfiguration config,
        ICurrentTenant currentTenant,
        IGeminiEmbeddingService embeddingService,
        IToolRouterService toolRouterService,
        IContextPrunerService contextPrunerService,
        IRepository<ChartOfAccount> chartOfAccountRepository,
        ILogger<GeminiAiAssistantService> logger)
    {
        _config = config;
        _currentTenant = currentTenant;
        _embeddingService = embeddingService;
        _toolRouterService = toolRouterService;
        _contextPrunerService = contextPrunerService;
        _chartOfAccountRepository = chartOfAccountRepository;
        _logger = logger;
    }

    public async Task<AiAssistantResponse> ProcessPromptAsync(AiAssistantRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new AiAssistantResponse { Success = false, ErrorMessage = "Prompt cannot be empty." };
        }

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
            return FallbackSimpleNavigation(request);
        }

        var model = geminiSection["Model"] ?? "gemini-2.5-flash";
        var baseEndpoint = (geminiSection["Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models").TrimEnd('/');
        var endpoint = $"{baseEndpoint}/{model}:generateContent?key={apiKey}";

        // 1. Vector embedding for prompt
        var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Prompt, cancellationToken);

        // 2. Hybrid Tool Routing: UI Context + Semantic pgvector search
        var targetedTools = await _toolRouterService.GetTargetedToolsAsync(request.CurrentRoute, embedding, request.Prompt, cancellationToken);

        // Always ensure search_vendor and search_account are available if the user is asking to create/draft
        var lowerPrompt = request.Prompt.ToLowerInvariant();
        if (lowerPrompt.Contains("payment") || lowerPrompt.Contains("voucher") || lowerPrompt.Contains("create") || lowerPrompt.Contains("pay"))
        {
            if (!targetedTools.Any(t => t.Id == "search_vendor"))
            {
                targetedTools.Add(new AiTool
                {
                    Id = "search_vendor",
                    ToolName = "search_vendor",
                    Description = "Searches for vendors/suppliers in database by name to resolve their ID.",
                    JsonSchema = """{"type":"object","properties":{"name":{"type":"string","description":"Vendor name to search"}},"required":["name"]}"""
                });
            }
        }

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
1. Navigation: When user asks to go to or view a list (e.g. payment listing, sales), call the appropriate filter/navigation tool and calculate explicit YYYY-MM-DD dates for relative terms like 'this month', 'today', 'last week'.
2. Data Entry & Drafting: When user asks to create or record a voucher:
   - If a vendor or supplier name is mentioned, FIRST call search_vendor to resolve their database ID.
   - Once resolved, call draft_payment_voucher with the vendorId, vendorName, and amount.
3. Language: Seamlessly understand English, Urdu (اردو), and Roman Urdu. Reply in the user's spoken language. Keep spoken replies concise, friendly, and natural for text-to-speech.
4. Human in the loop: Do not claim you saved the voucher permanently; state that you have drafted it for their review.";

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

        // 5. Agentic Recursive Loop (up to 2 tool execution turns)
        int maxLoopTurns = 2;
        while (maxLoopTurns-- > 0)
        {
            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstructions } } },
                contents = contents.ToArray(),
                tools = functionDeclarations.Count > 0 ? new[] { new { function_declarations = functionDeclarations.ToArray() } } : null,
                generationConfig = new
                {
                    temperature = 0.2
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error ({StatusCode}): {Error}", response.StatusCode, error);
                return FallbackSimpleNavigation(request);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            var candidate = doc.RootElement.GetProperty("candidates")[0];
            var contentEl = candidate.GetProperty("content");
            var parts = contentEl.GetProperty("parts");

            string? modelText = null;
            JsonElement? functionCall = null;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textEl))
                {
                    modelText = textEl.GetString();
                }

                if (part.TryGetProperty("functionCall", out var fnEl))
                {
                    functionCall = fnEl;
                }
            }

            // If Gemini invoked a function call
            if (functionCall.HasValue)
            {
                var fnName = functionCall.Value.GetProperty("name").GetString();
                var args = functionCall.Value.GetProperty("args");

                // Check if it's a backend read tool
                if (fnName == "search_vendor")
                {
                    var vendorName = args.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var vendors = await SearchVendorsAsync(vendorName, cancellationToken);

                    // Add functionCall to history
                    contents.Add(new
                    {
                        role = "model",
                        parts = new object[]
                        {
                            new { functionCall = new { name = fnName, args = JsonSerializer.Deserialize<object>(args.GetRawText()) } }
                        }
                    });

                    // Add functionResponse to history
                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = fnName,
                                    response = new { results = vendors }
                                }
                            }
                        }
                    });

                    // Continue loop so Gemini can now generate the draft_payment_voucher tool call
                    continue;
                }

                if (fnName == "search_account")
                {
                    var accountName = args.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
                    var accounts = await SearchAccountsAsync(accountName, cancellationToken);

                    contents.Add(new
                    {
                        role = "model",
                        parts = new object[]
                        {
                            new { functionCall = new { name = fnName, args = JsonSerializer.Deserialize<object>(args.GetRawText()) } }
                        }
                    });

                    contents.Add(new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                functionResponse = new
                                {
                                    name = fnName,
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

            // Normal conversational answer
            return new AiAssistantResponse
            {
                ActionType = AiActionType.Answer,
                ReplyText = modelText ?? "I've processed your request."
            };
        }

        return FallbackSimpleNavigation(request);
    }

    private async Task<List<object>> SearchVendorsAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return new List<object>();

        var query = _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && (EF.Functions.ILike(x.Title, $"%{name}%") || EF.Functions.ILike(x.Id, $"%{name}%")))
            .Take(5);

        var list = await query.Select(x => new { id = x.Id, name = x.Title }).ToListAsync(cancellationToken);
        return list.Cast<object>().ToList();
    }

    private async Task<List<object>> SearchAccountsAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return new List<object>();

        var query = _chartOfAccountRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.AccLevel == 5 && (EF.Functions.ILike(x.Title, $"%{name}%") || EF.Functions.ILike(x.Id, $"%{name}%")))
            .Take(5);

        var list = await query.Select(x => new { id = x.Id, name = x.Title }).ToListAsync(cancellationToken);
        return list.Cast<object>().ToList();
    }

    private static AiAssistantResponse FallbackSimpleNavigation(AiAssistantRequest request)
    {
        var lower = request.Prompt.ToLowerInvariant();
        if (lower.Contains("payment") || lower.Contains("voucher"))
        {
            return new AiAssistantResponse
            {
                ActionType = AiActionType.Navigate,
                TargetRoute = "/daily-entries/payment-voucher",
                ReplyText = "Navigating to payment vouchers."
            };
        }

        if (lower.Contains("sale") || lower.Contains("invoice"))
        {
            return new AiAssistantResponse
            {
                ActionType = AiActionType.Navigate,
                TargetRoute = "/daily-entries/sale",
                ReplyText = "Navigating to sales listing."
            };
        }

        return new AiAssistantResponse
        {
            ActionType = AiActionType.Answer,
            ReplyText = "I understand your request. You can ask me to navigate to payment listings, sales, or draft vouchers."
        };
    }
}
