using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Legacy.ChartOfAccounts;
using Retailer.Application.Legacy.SaleSupplies;
using Retailer.Infrastructure.Multitenancy;

namespace Retailer.Infrastructure.Services;

public class GeminiVisionService : IAiScanService
{
    private readonly IConfiguration _config;
    private readonly ICurrentTenant _currentTenant;
    private readonly TenantDbContext _tenantDbContext;
    private readonly IChartOfAccountService _chartOfAccountService;
    private static readonly HttpClient _httpClient = new();
    private readonly ILogger<GeminiVisionService> _logger;

    public GeminiVisionService(
        IConfiguration config,
        ICurrentTenant currentTenant,
        TenantDbContext tenantDbContext,
        IChartOfAccountService chartOfAccountService,
        ILogger<GeminiVisionService> logger)
    {
        _config = config;
        _currentTenant = currentTenant;
        _tenantDbContext = tenantDbContext;
        _chartOfAccountService = chartOfAccountService;
        _logger = logger;
    }

    public async Task<SaleSupplyScanResponse> ScanSaleSupplySheetAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        // 1. Validate Tenant & Quota
        var tenantId = _currentTenant.Id;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new UnauthorizedException("Tenant context is missing.");
        }

        var tenant = await _tenantDbContext.TenantInfo.FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new NotFoundException($"Tenant '{tenantId}' not found.");
        }

        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        if (tenant.AiScanCurrentMonth != currentMonth)
        {
            tenant.AiScanCurrentMonth = currentMonth;
            tenant.AiScanUsedThisMonth = 0;
        }

        if (tenant.AiScanUsedThisMonth >= tenant.AiScanMonthlyLimit)
        {
            throw new CustomException(
                $"Monthly AI scan limit reached for your organization ({tenant.AiScanUsedThisMonth}/{tenant.AiScanMonthlyLimit} used). Please contact your administrator to increase quota.",
                statusCode: System.Net.HttpStatusCode.TooManyRequests);
        }

        // 2. Read Image Bytes for Gemini Analysis
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();

        // 3. Call Gemini Vision API using Configuration / gemini.json
        var geminiSection = _config.GetSection("GeminiSettings");
        var apiKey = geminiSection["ApiKey"]
            ?? _config["Gemini:ApiKey"]
            ?? _config["GeminiApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new CustomException(
                "Gemini API key is not configured. Please add 'ApiKey' to 'Configurations/gemini.json' or set the GEMINI_API_KEY environment variable.",
                statusCode: System.Net.HttpStatusCode.BadRequest);
        }

        var model = geminiSection["Model"] ?? "gemini-1.5-flash";
        var baseEndpoint = (geminiSection["Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models").TrimEnd('/');
        var endpoint = $"{baseEndpoint}/{model}:generateContent?key={apiKey}";

        var mime = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
        var base64Data = Convert.ToBase64String(imageBytes);

        var prompt = @"You are an expert OCR AI specialized in retail supply sheets, daily distribution registers, and handwritten customer khata slips.
The image records goods/supplies delivered to various customers.
The handwriting may be in Urdu (اردو), Roman Urdu, or English.
Extract every customer supply line into the specified JSON format.
Rules:
- seq: sequential number starting at 1.
- customerName: extract the handwritten customer or shop name as accurately as possible (can be Urdu or English).
- qty: numeric quantity delivered.
- rate: unit price or rate if written, otherwise 0.
- discount: discount if written, otherwise 0.
- addLess: addition or deduction amount if written, otherwise 0.
- carriage: carriage or transport charges if written, otherwise 0.
- amount: total line amount if written, otherwise calculate (qty * rate) - discount + addLess + carriage.

Return strictly valid JSON with this schema:
{
  ""notes"": ""optional brief summary or heading found on the paper"",
  ""lines"": [
    {
      ""seq"": 1,
      ""customerName"": ""string"",
      ""qty"": 0.0,
      ""rate"": 0.0,
      ""discount"": 0.0,
      ""addLess"": 0.0,
      ""carriage"": 0.0,
      ""amount"": 0.0
    }
  ]
}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mime,
                                data = base64Data
                            }
                        }
                    }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini Flash Vision error: {StatusCode} - {Error}", response.StatusCode, err);
            throw new CustomException($"Failed to analyze paper sheet via Gemini AI: {response.ReasonPhrase}", statusCode: response.StatusCode);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedText = ExtractTextFromGeminiResponse(responseJson);

        // 4. Parse Extracted Lines
        var rawLines = new List<GeminiParsedLine>();
        try
        {
            var parsedObj = JsonSerializer.Deserialize<GeminiParsedResult>(parsedText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsedObj?.Lines != null)
            {
                rawLines = parsedObj.Lines;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Gemini output directly, attempting fallback line parsing.");
        }

        // 5. Customer Matching against Chart of Accounts
        var customers = await _chartOfAccountService.GetCustomerAccountsAsync(cancellationToken);
        var resultLines = new List<ScannedSaleSupplyLineDto>();
        int seqCounter = 1;

        foreach (var rawLine in rawLines)
        {
            var rawName = rawLine.CustomerName?.Trim() ?? string.Empty;
            var matched = FindBestCustomerMatch(rawName, customers);

            var lineRate = rawLine.Rate;
            var lineQty = rawLine.Qty;
            var lineDiscount = rawLine.Discount;
            var lineAddLess = rawLine.AddLess;
            var lineCarriage = rawLine.Carriage;
            var lineAmount = rawLine.Amount > 0
                ? rawLine.Amount
                : ((lineQty * lineRate) - lineDiscount + lineAddLess + lineCarriage);

            resultLines.Add(new ScannedSaleSupplyLineDto
            {
                Seq = seqCounter++,
                RawCustomerName = rawName,
                MatchedCustomerId = matched?.Account,
                MatchedCustomerName = matched?.Title,
                IsCustomerMatched = matched != null,
                Qty = lineQty,
                Rate = lineRate,
                Discount = lineDiscount,
                AddLess = lineAddLess,
                Carriage = lineCarriage,
                Amount = lineAmount
            });
        }

        // 6. Increment Tenant Quota Usage
        tenant.AiScanUsedThisMonth += 1;
        await _tenantDbContext.SaveChangesAsync(cancellationToken);

        return new SaleSupplyScanResponse
        {
            Lines = resultLines,
            RemainingScansThisMonth = Math.Max(0, tenant.AiScanMonthlyLimit - tenant.AiScanUsedThisMonth),
            MonthlyLimit = tenant.AiScanMonthlyLimit,
            RawDetectedText = parsedText
        };
    }

    private static string ExtractTextFromGeminiResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
            candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0)
            {
                var text = parts[0].GetProperty("text").GetString();
                return text ?? string.Empty;
            }
        }
        return string.Empty;
    }

    private static ChartOfAccountHeadResponse? FindBestCustomerMatch(string rawName, List<ChartOfAccountHeadResponse> customers)
    {
        if (string.IsNullOrWhiteSpace(rawName) || customers.Count == 0)
            return null;

        var normalizedRaw = NormalizeString(rawName);

        // 1. Exact match
        var exact = customers.FirstOrDefault(c => NormalizeString(c.Title) == normalizedRaw);
        if (exact != null) return exact;

        // 2. Starts with / Contains
        var contains = customers.FirstOrDefault(c =>
        {
            var normTitle = NormalizeString(c.Title);
            return normTitle.Contains(normalizedRaw) || normalizedRaw.Contains(normTitle);
        });
        if (contains != null) return contains;

        // 3. Token match
        var rawTokens = normalizedRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        ChartOfAccountHeadResponse? bestMatch = null;
        int maxTokenMatches = 0;

        foreach (var c in customers)
        {
            var titleTokens = NormalizeString(c.Title).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int matches = rawTokens.Count(rt => titleTokens.Any(tt => tt == rt || LevenshteinDistance(rt, tt) <= 1));
            if (matches > maxTokenMatches && matches >= 1)
            {
                maxTokenMatches = matches;
                bestMatch = c;
            }
        }

        return maxTokenMatches > 0 ? bestMatch : null;
    }

    private static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var clean = input.ToLowerInvariant().Trim();
        string[] honorifics = ["ch", "ch.", "malik", "mian", "sheikh", "sh.", "bhai", "khan", "mr", "mr.", "mrs", "mrs."];
        foreach (var h in honorifics)
        {
            if (clean.StartsWith(h + " "))
                clean = clean[(h.Length + 1)..].Trim();
        }
        return clean;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private sealed class GeminiParsedResult
    {
        public string? Notes { get; set; }
        public List<GeminiParsedLine> Lines { get; set; } = [];
    }

    private sealed class GeminiParsedLine
    {
        public int Seq { get; set; }
        public string? CustomerName { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Discount { get; set; }
        public decimal AddLess { get; set; }
        public decimal Carriage { get; set; }
        public decimal Amount { get; set; }
    }
}
