using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;

namespace meeting_copilot.Services;

public class MeetingService
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(
        IMeetingRepository meetingRepository,
        ILogger<MeetingService> logger)
    {
        _meetingRepository = meetingRepository;
        _logger = logger;
    }

    public async Task<Meeting> CreateMeetingAsync(
        string userId,
        string title,
        string? description = null,
        List<AgendaItem>? agendaItems = null,
        List<AttachmentRef>? attachments = null,
        string? microphoneDeviceId = null,
        string language = "en-US")
    {
        var meetingId = Guid.NewGuid().ToString();
        var meeting = new Meeting
        {
            Id = meetingId,
            MeetingId = meetingId, // Partition key same as ID
            UserId = userId,
            Title = title,
            Description = description,
            Status = MeetingStatus.Setup,
            CreatedAt = DateTimeOffset.UtcNow,
            AgendaItems = agendaItems ?? new List<AgendaItem>(),
            Attachments = attachments ?? new List<AttachmentRef>(),
            MicrophoneDeviceId = microphoneDeviceId,
            Language = language
        };

        await _meetingRepository.CreateAsync(meeting);
        
        _logger.LogInformation("Created meeting {MeetingId} for user {UserId}", meeting.Id, userId);
        
        return meeting;
    }

    public async Task<Meeting?> GetMeetingAsync(string meetingId)
    {
        return await _meetingRepository.GetByIdAsync(meetingId);
    }

    public async Task<Meeting> StartMeetingAsync(string meetingId)
    {
        var meeting = await _meetingRepository.GetByIdAsync(meetingId);
        if (meeting == null)
        {
            throw new InvalidOperationException($"Meeting {meetingId} not found");
        }

        if (meeting.Status != MeetingStatus.Setup)
        {
            throw new InvalidOperationException($"Meeting {meetingId} cannot be started from status {meeting.Status}");
        }

        var updatedMeeting = meeting with
        {
            Status = MeetingStatus.Active,
            StartedAt = DateTimeOffset.UtcNow
        };

        await _meetingRepository.UpdateAsync(updatedMeeting);
        
        _logger.LogInformation("Started meeting {MeetingId}", meetingId);
        
        return updatedMeeting;
    }

    public async Task<Meeting> EndMeetingAsync(string meetingId)
    {
        var meeting = await _meetingRepository.GetByIdAsync(meetingId);
        if (meeting == null)
        {
            throw new InvalidOperationException($"Meeting {meetingId} not found");
        }

        if (meeting.Status != MeetingStatus.Active)
        {
            throw new InvalidOperationException($"Meeting {meetingId} cannot be ended from status {meeting.Status}");
        }

        var endedAt = DateTimeOffset.UtcNow;
        var duration = meeting.StartedAt.HasValue 
            ? (int)(endedAt - meeting.StartedAt.Value).TotalSeconds 
            : 0;

        var updatedMeeting = meeting with
        {
            Status = MeetingStatus.Ended,
            EndedAt = endedAt,
            DurationSeconds = duration
        };

        await _meetingRepository.UpdateAsync(updatedMeeting);
        
        _logger.LogInformation("Ended meeting {MeetingId} with duration {Duration}s", meetingId, duration);
        
        return updatedMeeting;
    }

    public async Task<Meeting> UpdateMeetingAsync(Meeting meeting)
    {
        await _meetingRepository.UpdateAsync(meeting);
        _logger.LogInformation("Updated meeting {MeetingId}", meeting.Id);
        return meeting;
    }

    public async Task<List<Meeting>> GetUserMeetingsAsync(string userId, int limit = 50)
    {
        // This would need a query implementation in the repository
        // For now, we'll throw NotImplementedException
        throw new NotImplementedException("User meeting queries not yet implemented");
    }
}
