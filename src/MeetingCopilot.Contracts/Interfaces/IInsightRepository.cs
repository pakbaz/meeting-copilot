using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Interfaces;

public interface IInsightRepository
{
    Task<Insight?> GetByIdAsync(string insightId, string userId, CancellationToken cancellationToken = default);
    Task<Insight> CreateAsync(Insight insight, CancellationToken cancellationToken = default);
    Task<Insight> UpdateAsync(Insight insight, CancellationToken cancellationToken = default);
    Task<List<Insight>> GetByMeetingIdAsync(string userId, string meetingId, CancellationToken cancellationToken = default);
    Task<List<Insight>> GetByTypeAsync(string userId, string meetingId, InsightType type, CancellationToken cancellationToken = default);
    Task<List<Insight>> GetTopKeyPointsAsync(string userId, string meetingId, int limit = 5, CancellationToken cancellationToken = default);
}
