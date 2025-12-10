using System.Text.Json.Serialization;

namespace MeetingCopilot.Contracts.Entities;

public record Interaction
{
    // Identity
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = default!; // Partition key (multi-tenant)
    public string MeetingId { get; init; } = default!; // Reference to meeting
    
    // Type discriminator
    public InteractionType Type { get; init; }
    
    // Content
    public string Content { get; init; } = default!; // Transcribed text or generated content
    public float[]? ContentVector { get; init; } // 1536-dim embedding for RAG
    
    // Speaker reference (for utterances)
    public string? SpeakerId { get; init; } // Reference to speakers container
    public string? SpeakerName { get; init; } // Denormalized for display
    
    // Question/Answer specific
    public string? QuestionId { get; init; } // Links answer to question
    public double? Confidence { get; init; } // AI confidence score
    public List<string>? Sources { get; init; } // RAG source references
    
    // Agent metadata
    public string? AgentName { get; init; } // "TranscriptAgent", "AnswerAgent", etc.
    public int? ProcessingTimeMs { get; init; }
    
    // Timestamps
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow; // When spoken/generated
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow; // When stored
    
    // TTL (omit from JSON when null to avoid Cosmos DB BadRequest)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Ttl { get; init; } // Set to 7776000 on meeting archive
}

public enum InteractionType
{
    Utterance,
    Question,
    Answer,
    AgentMessage
}
