using Pgvector;
using Retailer.Application.Common.Interfaces;

namespace Retailer.Infrastructure.AI.Gemini;

public interface IGeminiEmbeddingService : IScopedService
{
    Task<Vector?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
