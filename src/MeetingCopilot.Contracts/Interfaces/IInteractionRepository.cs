using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Interfaces;

public interface IInteractionRepository
{
    Task<Interaction?> GetByIdAsync(string interactionId, string userId, CancellationToken cancellationToken = default);
    Task<Interaction> CreateAsync(Interaction interaction, CancellationToken cancellationToken = default);
    Task<List<Interaction>> GetByMeetingIdAsync(string userId, string meetingId, CancellationToken cancellationToken = default);
    Task<List<Interaction>> GetRecentAsync(string userId, string meetingId, int limit = 10, CancellationToken cancellationToken = default);
    Task<List<Interaction>> GetByTypeAsync(string userId, string meetingId, InteractionType type, CancellationToken cancellationToken = default);
}
