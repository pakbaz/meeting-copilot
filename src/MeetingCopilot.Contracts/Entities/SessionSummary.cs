namespace MeetingCopilot.Contracts.Entities;

public record SessionSummary
{
    // Identity
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = default!; // Partition key
    public string MeetingId { get; init; } = default!;
    
    // Summary content
    public string ExecutiveSummary { get; init; } = default!; // High-level overview
    public List<string> KeyTakeaways { get; init; } = new(); // Bullet points
    public List<string> TopicsCovered { get; init; } = new(); // Discussion topics
    public List<string> DecisionsMade { get; init; } = new(); // Key decisions
    
    // Aggregated metrics
    public int TotalUtterances { get; init; }
    public int TotalSpeakers { get; init; }
    public int QuestionsAsked { get; init; }
    public int QuestionsAnswered { get; init; }
    public int KeyPointsExtracted { get; init; }
    public int ActionItemsCreated { get; init; }
    
    // References
    public List<string> ActionItemIds { get; init; } = new(); // References to insights
    public List<string> KeyPointIds { get; init; } = new(); // References to insights
    
    // Timestamps
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    
    // No TTL - session summaries retained indefinitely
}
