namespace MeetingCopilot.Contracts.Events;

public record TranscriptionEvent
{
    public string MeetingId { get; init; } = default!;
    public string UtteranceId { get; init; } = default!;
    public string SpeakerId { get; init; } = default!;
    public string SpeakerName { get; init; } = default!;
    public string Content { get; init; } = default!;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public bool IsFinal { get; init; } // true for final, false for interim results
}
