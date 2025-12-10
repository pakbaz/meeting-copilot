using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory;

public class VectorSearchService
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(
        CosmosClient cosmosClient,
        string databaseName,
        IEmbeddingService embeddingService,
        ILogger<VectorSearchService> logger)
    {
        _cosmosClient = cosmosClient;
        _databaseName = databaseName;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Search for similar interactions using vector similarity
    /// </summary>
    public async Task<List<Interaction>> SearchSimilarInteractionsAsync(
        string userId,
        string query,
        int limit = 5,
        double minSimilarity = 0.7,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var container = _cosmosClient.GetContainer(_databaseName, "interactions");

        var sqlQuery = @"
            SELECT TOP @limit c.*, VectorDistance(c.contentVector, @embedding) AS similarity
            FROM c
            WHERE c.userId = @userId
            AND VectorDistance(c.contentVector, @embedding) < @threshold
            ORDER BY VectorDistance(c.contentVector, @embedding)";

        var queryDefinition = new QueryDefinition(sqlQuery)
            .WithParameter("@limit", limit)
            .WithParameter("@userId", userId)
            .WithParameter("@embedding", queryEmbedding)
            .WithParameter("@threshold", 1.0 - minSimilarity);

        var results = new List<Interaction>();
        using var iterator = container.GetItemQueryIterator<Interaction>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        _logger.LogInformation("Found {Count} similar interactions for query", results.Count);
        return results;
    }

    /// <summary>
    /// Search for similar insights using vector similarity
    /// </summary>
    public async Task<List<Insight>> SearchSimilarInsightsAsync(
        string userId,
        string query,
        int limit = 5,
        double minSimilarity = 0.7,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var container = _cosmosClient.GetContainer(_databaseName, "insights");

        var sqlQuery = @"
            SELECT TOP @limit c.*, VectorDistance(c.embedding, @embedding) AS similarity
            FROM c
            WHERE c.userId = @userId
            AND VectorDistance(c.embedding, @embedding) < @threshold
            ORDER BY VectorDistance(c.embedding, @embedding)";

        var queryDefinition = new QueryDefinition(sqlQuery)
            .WithParameter("@limit", limit)
            .WithParameter("@userId", userId)
            .WithParameter("@embedding", queryEmbedding)
            .WithParameter("@threshold", 1.0 - minSimilarity);

        var results = new List<Insight>();
        using var iterator = container.GetItemQueryIterator<Insight>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        _logger.LogInformation("Found {Count} similar insights for query", results.Count);
        return results;
    }

    /// <summary>
    /// Get relevant context for RAG from recent interactions and similar past interactions
    /// </summary>
    public async Task<string> GetRAGContextAsync(
        string userId,
        string meetingId,
        string query,
        int recentCount = 5,
        int similarCount = 3,
        CancellationToken cancellationToken = default)
    {
        // Get recent interactions from current meeting
        var recentInteractions = await GetRecentInteractionsAsync(userId, meetingId, recentCount, cancellationToken);
        
        // Get similar interactions from past meetings
        var similarInteractions = await SearchSimilarInteractionsAsync(userId, query, similarCount, 0.7, cancellationToken);

        // Combine and format context
        var contextParts = new List<string>();

        if (recentInteractions.Any())
        {
            contextParts.Add("## Recent Conversation:");
            foreach (var interaction in recentInteractions)
            {
                contextParts.Add($"[{interaction.SpeakerName ?? "Unknown"}]: {interaction.Content}");
            }
        }

        if (similarInteractions.Any())
        {
            contextParts.Add("\n## Relevant Past Context:");
            foreach (var interaction in similarInteractions.Where(i => i.MeetingId != meetingId))
            {
                contextParts.Add($"{interaction.Content}");
            }
        }

        return string.Join("\n", contextParts);
    }

    private async Task<List<Interaction>> GetRecentInteractionsAsync(
        string userId,
        string meetingId,
        int limit,
        CancellationToken cancellationToken)
    {
        var container = _cosmosClient.GetContainer(_databaseName, "interactions");

        var queryDefinition = new QueryDefinition(
            $"SELECT TOP @limit * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId AND c.type = 'Utterance' ORDER BY c.timestamp DESC")
            .WithParameter("@limit", limit)
            .WithParameter("@userId", userId)
            .WithParameter("@meetingId", meetingId);

        var results = new List<Interaction>();
        using var iterator = container.GetItemQueryIterator<Interaction>(queryDefinition);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }
}
