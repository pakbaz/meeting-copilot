namespace MeetingCopilot.Contracts.Interfaces;

/// <summary>
/// Memory provider interface following james-tn/agent-memory pattern
/// </summary>
public interface IMemoryProvider
{
    /// <summary>
    /// Store a memory item with vector embedding
    /// </summary>
    Task<string> StoreAsync<T>(
        string userId,
        string meetingId,
        T item,
        string content,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieve similar memories using vector search
    /// </summary>
    Task<List<T>> SearchSimilarAsync<T>(
        string userId,
        string query,
        int limit = 5,
        double similarityThreshold = 0.7,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Retrieve memories for a specific meeting
    /// </summary>
    Task<List<T>> GetByMeetingAsync<T>(
        string userId,
        string meetingId,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Delete memories older than specified date
    /// </summary>
    Task DeleteOldMemoriesAsync(
        string userId,
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent interactions for context
    /// </summary>
    Task<List<T>> GetRecentAsync<T>(
        string userId,
        string meetingId,
        int limit = 10,
        CancellationToken cancellationToken = default) where T : class;
}
