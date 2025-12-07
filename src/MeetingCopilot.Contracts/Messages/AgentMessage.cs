namespace MeetingCopilot.Contracts.Messages;

public abstract record AgentMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
    public string MeetingId { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string SourceAgent { get; init; } = default!;
    public string? TargetAgent { get; init; } // null = broadcast
    public int Priority { get; init; } = 5; // 1 = highest, 10 = lowest
}
