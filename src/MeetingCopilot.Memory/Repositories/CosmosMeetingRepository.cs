using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory.Repositories;

public class CosmosMeetingRepository : CosmosDbService, IMeetingRepository
{
    public CosmosMeetingRepository(
        CosmosClient cosmosClient,
        string databaseName,
        ILogger<CosmosMeetingRepository> logger)
        : base(cosmosClient, databaseName, "meetings", logger)
    {
    }

    public async Task<Meeting?> GetByIdAsync(string meetingId, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync<Meeting>(meetingId, meetingId, cancellationToken);
    }

    public async Task<Meeting> CreateAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        // Ensure MeetingId matches Id for partition key
        var meetingToCreate = meeting with { MeetingId = meeting.Id };
        return await UpsertItemAsync(meetingToCreate, meetingToCreate.MeetingId, cancellationToken);
    }

    public async Task<Meeting> UpdateAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        return await UpsertItemAsync(meeting, meeting.MeetingId, cancellationToken);
    }

    public async Task DeleteAsync(string meetingId, CancellationToken cancellationToken = default)
    {
        await DeleteItemAsync(meetingId, meetingId, cancellationToken);
    }

    public async Task<List<Meeting>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt DESC";
        return await QueryItemsAsync<Meeting>(query, new Dictionary<string, object> { ["userId"] = userId }, cancellationToken);
    }

    public async Task<List<Meeting>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.userId = @userId AND c.status IN ('Setup', 'Active') ORDER BY c.createdAt DESC";
        return await QueryItemsAsync<Meeting>(query, new Dictionary<string, object> { ["userId"] = userId }, cancellationToken);
    }
}
