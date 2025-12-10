using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory.Repositories;

public class CosmosInsightRepository : CosmosDbService, IInsightRepository
{
    public CosmosInsightRepository(
        CosmosClient cosmosClient,
        string databaseName,
        ILogger<CosmosInsightRepository> logger)
        : base(cosmosClient, databaseName, "insights", logger)
    {
    }

    public async Task<Insight?> GetByIdAsync(string insightId, string userId, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync<Insight>(insightId, userId, cancellationToken);
    }

    public async Task<Insight> CreateAsync(Insight insight, CancellationToken cancellationToken = default)
    {
        return await UpsertItemAsync(insight, insight.UserId, cancellationToken);
    }

    public async Task<Insight> UpdateAsync(Insight insight, CancellationToken cancellationToken = default)
    {
        return await UpsertItemAsync(insight, insight.UserId, cancellationToken);
    }

    public async Task<List<Insight>> GetByMeetingIdAsync(string userId, string meetingId, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId ORDER BY c.createdAt DESC";
        return await QueryItemsAsync<Insight>(
            query,
            new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["meetingId"] = meetingId
            },
            cancellationToken);
    }

    public async Task<List<Insight>> GetByTypeAsync(string userId, string meetingId, InsightType type, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId AND c.type = @type ORDER BY c.createdAt DESC";
        return await QueryItemsAsync<Insight>(
            query,
            new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["meetingId"] = meetingId,
                ["type"] = type.ToString()
            },
            cancellationToken);
    }

    public async Task<List<Insight>> GetTopKeyPointsAsync(string userId, string meetingId, int limit = 5, CancellationToken cancellationToken = default)
    {
        var query = $"SELECT TOP @limit * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId AND c.type = 'KeyPoint' ORDER BY c.priorityScore DESC";
        return await QueryItemsAsync<Insight>(
            query,
            new Dictionary<string, object>
            {
                ["limit"] = limit,
                ["userId"] = userId,
                ["meetingId"] = meetingId
            },
            cancellationToken);
    }
}
