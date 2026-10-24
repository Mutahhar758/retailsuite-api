using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Retailer.Infrastructure.AI.Gemini;

public class GeminiEmbeddingService : IGeminiEmbeddingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiEmbeddingService> _logger;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    public GeminiEmbeddingService(IConfiguration config, ILogger<GeminiEmbeddingService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<Vector?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var geminiSection = _config.GetSection("GeminiSettings");
        var apiKey = geminiSection["ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Skipping vector embedding generation.");
            return null;
        }

        var embeddingModel = geminiSection["EmbeddingModel"] ?? "text-embedding-004";
        var baseEndpoint = (geminiSection["Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models").TrimEnd('/');
        var endpoint = $"{baseEndpoint}/{embeddingModel}:embedContent?key={apiKey}";

        var payload = new
        {
            model = $"models/{embeddingModel}",
            content = new
            {
                parts = new[] { new { text } }
            }
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
                var floats = new float[valuesEl.GetArrayLength()];
                int i = 0;
                foreach (var item in valuesEl.EnumerateArray())
                {
                    floats[i++] = item.GetSingle();
                }

                return new Vector(floats);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating vector embedding for prompt: {Text}", text);
        }

        return null;
    }
}
