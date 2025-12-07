namespace MeetingCopilot.Contracts.Entities;

public record Insight
{
    // Identity
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = default!; // Partition key
    public string MeetingId { get; init; } = default!;
    
    // Type discriminator
    public InsightType Type { get; init; }
    
    // Content
    public string Title { get; init; } = default!; // Short display title
    public string Content { get; init; } = default!; // Full content
    public float[]? Embedding { get; init; } // 1536-dim for semantic search
    
    // Key point specific
    public double? PriorityScore { get; init; } // 0-100, for ranking top 5
    public string? SourceUtteranceId { get; init; } // Reference to source interaction
    
    // Action item specific
    public string? OwnerSpeakerId { get; init; } // Assigned owner
    public string? OwnerName { get; init; } // Denormalized
    public DateTimeOffset? DueDate { get; init; } // If mentioned
    public ActionItemStatus? Status { get; init; }
    
    // Research result specific
    public string? ResearchQuery { get; init; } // Original query
    public List<WebSource>? WebSources { get; init; } // Citations
    
    // Timestamps
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    // TTL (action items exempt from TTL)
    public int? Ttl { get; init; }
}

public enum InsightType
{
    KeyPoint,
    ActionItem,
    ParkingLot,
    ResearchResult
}

public enum ActionItemStatus
{
    Pending,
    InProgress,
    Completed
}

public record WebSource
{
    public string Title { get; init; } = default!;
    public string Url { get; init; } = default!;
    public string Snippet { get; init; } = default!;
}
