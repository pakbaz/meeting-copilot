using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Memory.Repositories;

public class CosmosSpeakerRepository : CosmosDbService, ISpeakerRepository
{
    public CosmosSpeakerRepository(
        CosmosClient cosmosClient,
        string databaseName,
        ILogger<CosmosSpeakerRepository> logger)
        : base(cosmosClient, databaseName, "speakers", logger)
    {
    }

    public async Task<Speaker?> GetByIdAsync(string speakerId, string meetingId, CancellationToken cancellationToken = default)
    {
        return await GetItemAsync<Speaker>(speakerId, meetingId, cancellationToken);
    }

    public async Task<Speaker> CreateAsync(Speaker speaker, CancellationToken cancellationToken = default)
    {
        return await UpsertItemAsync(speaker, speaker.MeetingId, cancellationToken);
    }

    public async Task<Speaker> UpdateAsync(Speaker speaker, CancellationToken cancellationToken = default)
    {
        return await UpsertItemAsync(speaker, speaker.MeetingId, cancellationToken);
    }

    public async Task<List<Speaker>> GetByMeetingIdAsync(string meetingId, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.meetingId = @meetingId ORDER BY c.priorityRank";
        return await QueryItemsAsync<Speaker>(query, new Dictionary<string, object> { ["meetingId"] = meetingId }, cancellationToken);
    }

    public async Task<Speaker?> GetBySpeakerLabelAsync(string meetingId, string speakerLabel, CancellationToken cancellationToken = default)
    {
        var query = "SELECT * FROM c WHERE c.meetingId = @meetingId AND c.speakerLabel = @speakerLabel";
        var results = await QueryItemsAsync<Speaker>(
            query,
            new Dictionary<string, object>
            {
                ["meetingId"] = meetingId,
                ["speakerLabel"] = speakerLabel
            },
            cancellationToken);

        return results.FirstOrDefault();
    }
}
