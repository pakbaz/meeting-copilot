using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Interfaces;

public interface ISpeakerRepository
{
    Task<Speaker?> GetByIdAsync(string speakerId, string meetingId, CancellationToken cancellationToken = default);
    Task<Speaker> CreateAsync(Speaker speaker, CancellationToken cancellationToken = default);
    Task<Speaker> UpdateAsync(Speaker speaker, CancellationToken cancellationToken = default);
    Task<List<Speaker>> GetByMeetingIdAsync(string meetingId, CancellationToken cancellationToken = default);
    Task<Speaker?> GetBySpeakerLabelAsync(string meetingId, string speakerLabel, CancellationToken cancellationToken = default);
}
