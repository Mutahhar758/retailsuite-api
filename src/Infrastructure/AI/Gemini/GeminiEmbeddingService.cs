using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.AI.Gemini;

public class GeminiEmbeddingService : IGeminiEmbeddingService
{
    private const int TargetDimension = 768;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiEmbeddingService> _logger;
    private readonly ICurrentTenant? _currentTenant;
    private readonly ITenantTokenUsageService? _tokenUsageService;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public GeminiEmbeddingService(
        IConfiguration config,
        ILogger<GeminiEmbeddingService> logger,
        ICurrentTenant? currentTenant = null,
        ITenantTokenUsageService? tokenUsageService = null)
    {
        _config = config;
        _logger = logger;
        _currentTenant = currentTenant;
        _tokenUsageService = tokenUsageService;
    }

    public async Task<Vector?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

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
            _logger.LogWarning("Gemini API key is not configured. Skipping vector embedding generation.");
            return null;
        }

        var embeddingModel = geminiSection["EmbeddingModel"] ?? "gemini-embedding-2";
        var baseEndpoint = (geminiSection["Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models").TrimEnd('/');
        var endpoint = $"{baseEndpoint}/{embeddingModel}:embedContent?key={apiKey}";

        var payload = new
        {
            model = $"models/{embeddingModel}",
            content = new
            {
                parts = new[] { new { text } }
            },
            outputDimensionality = TargetDimension
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini embedding request failed ({StatusCode}): {Error}", response.StatusCode, errorMsg);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("embedding", out var embeddingEl) &&
                embeddingEl.TryGetProperty("values", out var valuesEl) &&
                valuesEl.ValueKind == JsonValueKind.Array)
            {
                var count = valuesEl.GetArrayLength();
                var rawFloats = new float[count];
                int i = 0;
                foreach (var item in valuesEl.EnumerateArray())
                {
                    rawFloats[i++] = item.GetSingle();
                }

                // Ensure exactly TargetDimension (768) elements to match PostgreSQL pgvector column vector(768)
                float[] finalFloats;
                if (rawFloats.Length == TargetDimension)
                {
                    finalFloats = rawFloats;
                }
                else if (rawFloats.Length > TargetDimension)
                {
                    // Truncate to 768 dimensions (MRL prefix)
                    finalFloats = new float[TargetDimension];
                    Array.Copy(rawFloats, finalFloats, TargetDimension);
                }
                else
                {
                    // Pad with zeroes if fewer dimensions returned
                    finalFloats = new float[TargetDimension];
                    Array.Copy(rawFloats, finalFloats, rawFloats.Length);
                }

                // Record token consumption for embedding model
                if (_tokenUsageService != null)
                {
                    int promptTokens = 0;
                    if (doc.RootElement.TryGetProperty("usageMetadata", out var usageEl) &&
                        usageEl.TryGetProperty("promptTokenCount", out var pt) && pt.TryGetInt32(out var ptVal))
                    {
                        promptTokens = ptVal;
                    }
                    else
                    {
                        // Fallback token approximation for embeddings (approx 4 chars/token)
                        promptTokens = Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
                    }

                    _ = _tokenUsageService.RecordUsageAsync(
                        tenantId: _currentTenant?.Id ?? "GLOBAL",
                        model: embeddingModel,
                        inputTokens: promptTokens,
                        outputTokens: 0,
                        totalTokens: promptTokens,
                        cancellationToken: cancellationToken);
                }

                return new Vector(finalFloats);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating vector embedding for prompt: {Text}", text);
        }

        return null;
    }
}
