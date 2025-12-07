namespace MeetingCopilot.Contracts.Events;

public record ResearchEvent
{
    public string MeetingId { get; init; } = default!;
    public string ResearchId { get; init; } = default!;
    public string Query { get; init; } = default!;
    public string Summary { get; init; } = default!;
    public List<WebSourceItem> Sources { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record WebSourceItem
{
    public string Title { get; init; } = default!;
    public string Url { get; init; } = default!;
    public string Snippet { get; init; } = default!;
}
