namespace MeetingCopilot.Contracts.Entities;

public record Meeting
{
    // Identity
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string MeetingId { get; init; } = default!; // Same as Id (partition key)
    public string UserId { get; init; } = default!; // Owner user ID (multi-tenant isolation)
    
    // Core fields
    public string Title { get; init; } = default!;
    public string? Description { get; init; } // Rich text/markdown
    public MeetingStatus Status { get; init; } = MeetingStatus.Setup;
    
    // Timing
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public int? DurationSeconds { get; init; }
    
    // Pre-meeting context
    public List<AgendaItem> AgendaItems { get; init; } = new();
    public List<AttachmentRef> Attachments { get; init; } = new();
    public string? InitialContext { get; init; }
    
    // Configuration
    public string? MicrophoneDeviceId { get; init; }
    public string Language { get; init; } = "en-US";
    
    // TTL for 90-day retention
    public int? Ttl { get; init; } // Set to 7776000 (90 days) on archive
}

public enum MeetingStatus
{
    Setup,
    Active,
    Ended,
    Archived
}

public record AgendaItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Title { get; init; } = default!;
    public int Order { get; init; }
    public AgendaItemStatus Status { get; init; } = AgendaItemStatus.Pending;
    public string? Summary { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}

public enum AgendaItemStatus
{
    Pending,
    InProgress,
    Completed
}

public record AttachmentRef
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Filename { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public string BlobUri { get; init; } = default!;
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;
}
