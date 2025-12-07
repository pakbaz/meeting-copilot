namespace MeetingCopilot.Contracts.Events;

public record KeyPointsEvent
{
    public string MeetingId { get; init; } = default!;
    public List<KeyPointItem> KeyPoints { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record KeyPointItem
{
    public string Id { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Content { get; init; } = default!;
    public double PriorityScore { get; init; }
    public string? SourceUtteranceId { get; init; }
}
