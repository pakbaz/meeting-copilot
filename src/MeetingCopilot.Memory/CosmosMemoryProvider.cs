using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory;

public class CosmosMemoryProvider : IMemoryProvider
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<CosmosMemoryProvider> _logger;

    public CosmosMemoryProvider(
        CosmosClient cosmosClient,
        string databaseName,
        IEmbeddingService embeddingService,
        ILogger<CosmosMemoryProvider> logger)
    {
        _cosmosClient = cosmosClient;
        _databaseName = databaseName;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<string> StoreAsync<T>(
        string userId,
        string meetingId,
        T item,
        string content,
        CancellationToken cancellationToken = default) where T : class
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken);
        
        // Determine container based on type
        var containerName = GetContainerName<T>();
        var container = _cosmosClient.GetContainer(_databaseName, containerName);

        // Store with embedding
        var itemWithEmbedding = AttachEmbedding(item, embedding);
        var response = await container.UpsertItemAsync(
            itemWithEmbedding,
            new PartitionKey(userId),
            cancellationToken: cancellationToken);

        _logger.LogDebug("Stored memory item in {Container} for user {UserId}", containerName, userId);
        return response.Resource?.GetType().GetProperty("Id")?.GetValue(response.Resource)?.ToString() ?? string.Empty;
    }

    public async Task<List<T>> SearchSimilarAsync<T>(
        string userId,
        string query,
        int limit = 5,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default) where T : class
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        
        var containerName = GetContainerName<T>();
        var container = _cosmosClient.GetContainer(_databaseName, containerName);

        // Use vector search
        var vectorPath = GetVectorPath<T>();
        var sqlQuery = $@"
            SELECT TOP @limit c.*, VectorDistance(c.{vectorPath}, @embedding) AS similarityScore
            FROM c
            WHERE c.userId = @userId
            AND VectorDistance(c.{vectorPath}, @embedding) < @threshold
            ORDER BY VectorDistance(c.{vectorPath}, @embedding)";

        var queryDefinition = new QueryDefinition(sqlQuery)
            .WithParameter("@limit", limit)
            .WithParameter("@userId", userId)
            .WithParameter("@embedding", queryEmbedding)
            .WithParameter("@threshold", 1.0 - similarityThreshold); // Cosine distance

        var results = new List<T>();
        using var iterator = container.GetItemQueryIterator<T>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        _logger.LogDebug("Found {Count} similar items for query in {Container}", results.Count, containerName);
        return results;
    }

    public async Task<List<T>> GetByMeetingAsync<T>(
        string userId,
        string meetingId,
        CancellationToken cancellationToken = default) where T : class
    {
        var containerName = GetContainerName<T>();
        var container = _cosmosClient.GetContainer(_databaseName, containerName);

        var queryDefinition = new QueryDefinition(
            "SELECT * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId ORDER BY c.timestamp DESC")
            .WithParameter("@userId", userId)
            .WithParameter("@meetingId", meetingId);

        var results = new List<T>();
        using var iterator = container.GetItemQueryIterator<T>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task DeleteOldMemoriesAsync(
        string userId,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        // Implementation would set TTL on old items
        // For now, log the operation
        _logger.LogInformation("DeleteOldMemories for user {UserId} older than {Date}", userId, olderThan);
        await Task.CompletedTask;
    }

    public async Task<List<T>> GetRecentAsync<T>(
        string userId,
        string meetingId,
        int limit = 10,
        CancellationToken cancellationToken = default) where T : class
    {
        var containerName = GetContainerName<T>();
        var container = _cosmosClient.GetContainer(_databaseName, containerName);

        var queryDefinition = new QueryDefinition(
            $"SELECT TOP @limit * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId ORDER BY c.timestamp DESC")
            .WithParameter("@limit", limit)
            .WithParameter("@userId", userId)
            .WithParameter("@meetingId", meetingId);

        var results = new List<T>();
        using var iterator = container.GetItemQueryIterator<T>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    private static string GetContainerName<T>()
    {
        var typeName = typeof(T).Name.ToLowerInvariant();
        return typeName switch
        {
            "interaction" => "interactions",
            "insight" => "insights",
            "speaker" => "speakers",
            "meeting" => "meetings",
            "sessionsummary" => "session_summaries",
            _ => throw new ArgumentException($"Unknown type: {typeof(T).Name}")
        };
    }

    private static string GetVectorPath<T>()
    {
        var typeName = typeof(T).Name.ToLowerInvariant();
        return typeName switch
        {
            "interaction" => "contentVector",
            "insight" => "embedding",
            _ => throw new ArgumentException($"Type {typeof(T).Name} does not support vector search")
        };
    }

    private static object AttachEmbedding<T>(T item, float[] embedding) where T : class
    {
        // Use reflection to set the embedding property
        var itemType = item.GetType();
        var embeddingProperty = itemType.GetProperty("ContentVector") ?? itemType.GetProperty("Embedding");
        
        if (embeddingProperty != null && embeddingProperty.CanWrite)
        {
            // Create a copy with the embedding set
            var properties = itemType.GetProperties();
            var values = new Dictionary<string, object?>();
            
            foreach (var prop in properties)
            {
                if (prop.Name == embeddingProperty.Name)
                {
                    values[prop.Name] = embedding;
                }
                else
                {
                    values[prop.Name] = prop.GetValue(item);
                }
            }
            
            // For records, use with expression pattern (simplified - just return original for now)
            // In production, would need proper record copying
        }

        return item;
    }
}
