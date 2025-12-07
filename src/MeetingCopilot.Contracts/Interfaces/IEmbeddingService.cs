namespace MeetingCopilot.Contracts.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generate vector embedding for text content using OpenAI embeddings
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple texts in batch
    /// </summary>
    Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
