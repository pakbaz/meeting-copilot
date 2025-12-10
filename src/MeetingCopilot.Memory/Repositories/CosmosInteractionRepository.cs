using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory.Repositories;

public class CosmosInteractionRepository : CosmosDbService, IInteractionRepository
{
    public CosmosInteractionRepository(
        CosmosClient cosmosClient,
        string databaseName,
        ILogger<CosmosInteractionRepository> logger)
        : base(cosmosClient, databaseName, "interactions", logger)
    {
    }

    public async Task<Interaction?> GetByIdAsync(string interactionId, string userId, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync<Interaction>(interactionId, userId, cancellationToken);
    }

    public async Task<Interaction> CreateAsync(Interaction interaction, CancellationToken cancellationToken = default)
    {
        return await UpsertItemAsync(interaction, interaction.UserId, cancellationToken);
    }

    public async Task<List<Interaction>> GetByMeetingIdAsync(string userId, string meetingId, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId ORDER BY c.timestamp DESC";
        return await QueryItemsAsync<Interaction>(
            query,
            new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["meetingId"] = meetingId
            },
            cancellationToken);
    }

    public async Task<List<Interaction>> GetRecentAsync(string userId, string meetingId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var query = $"SELECT TOP @limit * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId ORDER BY c.timestamp DESC";
        return await QueryItemsAsync<Interaction>(
            query,
            new Dictionary<string, object>
            {
                ["limit"] = limit,
                ["userId"] = userId,
                ["meetingId"] = meetingId
            },
            cancellationToken);
    }

    public async Task<List<Interaction>> GetByTypeAsync(string userId, string meetingId, InteractionType type, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId AND c.meetingId = @meetingId AND c.type = @type ORDER BY c.timestamp DESC";
        return await QueryItemsAsync<Interaction>(
            query,
            new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["meetingId"] = meetingId,
                ["type"] = type.ToString()
            },
            cancellationToken);
    }
}
