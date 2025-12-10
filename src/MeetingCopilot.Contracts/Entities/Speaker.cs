namespace MeetingCopilot.Contracts.Entities;

public record Speaker
{
    // Identity
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string MeetingId { get; init; } = default!; // Partition key
    public string UserId { get; init; } = default!; // Owner user ID
    
    // Speaker identification
    public string SpeakerLabel { get; init; } = default!; // "Speaker 1", "Speaker 2", etc. (initial)
    public string? DisplayName { get; init; } // Inferred or set via /speaker command
    public string? Role { get; init; } // Optional role from /speaker {name} {role}
    public string? Company { get; init; } // Optional company name
    public bool IsSelf { get; init; } // True if this is the user
    
    // Priority for question answering
    public int PriorityRank { get; init; } = 99; // 1 = highest (user), others ranked by role
    
    // Timestamps
    public DateTimeOffset FirstSeenAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; init; } = DateTimeOffset.UtcNow;
}
