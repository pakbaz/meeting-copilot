namespace MeetingCopilot.Contracts.Events;

public record AnswerEvent
{
    public string MeetingId { get; init; } = default!;
    public string QuestionId { get; init; } = default!;
    public string AnswerId { get; init; } = default!;
    public string Question { get; init; } = default!;
    public string Answer { get; init; } = default!;
    public double Confidence { get; init; }
    public List<string> Sources { get; init; } = new();
    public int ProcessingTimeMs { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
