using Azure;
using Azure.AI.OpenAI;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace MeetingCopilot.Memory;

public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        EmbeddingClient embeddingClient,
        ILogger<EmbeddingService> logger)
    {
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<float>();
        }

        try
        {
            var response = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            var embedding = response.Value.ToFloats().ToArray();
            _logger.LogDebug("Generated embedding with {Dimensions} dimensions", embedding.Length);
            
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text");
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (textList.Count == 0)
        {
            return new List<float[]>();
        }

        try
        {
            var embeddings = new List<float[]>();
            foreach (var text in textList)
            {
                var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
                embeddings.Add(embedding);
            }

            _logger.LogDebug("Generated {Count} embeddings", embeddings.Count);
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embeddings for batch");
            throw;
        }
    }
}
