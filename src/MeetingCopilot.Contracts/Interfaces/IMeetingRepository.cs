using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Interfaces;

public interface IMeetingRepository
{
    Task<Meeting?> GetByIdAsync(string meetingId, CancellationToken cancellationToken = default);
    Task<Meeting> CreateAsync(Meeting meeting, CancellationToken cancellationToken = default);
    Task<Meeting> UpdateAsync(Meeting meeting, CancellationToken cancellationToken = default);
    Task DeleteAsync(string meetingId, CancellationToken cancellationToken = default);
    Task<List<Meeting>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<Meeting>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
