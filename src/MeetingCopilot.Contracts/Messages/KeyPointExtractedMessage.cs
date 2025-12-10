namespace MeetingCopilot.Contracts.Messages;

/// <summary>
/// Message indicating a key point has been extracted from the meeting.
/// </summary>
public record KeyPointExtractedMessage : AgentMessage
{
    /// <summary>
    /// The unique ID for this key point (maps to Insight Id).
    /// </summary>
    public string KeyPointId { get; init; } = default!;
    
    /// <summary>
    /// Title/summary of the key point.
    /// </summary>
    public string Title { get; init; } = default!;
    
    /// <summary>
    /// Detailed content of the key point.
    /// </summary>
    public string Content { get; init; } = default!;
    
    /// <summary>
    /// The speaker ID who made this key point.
    /// </summary>
    public string SpeakerId { get; init; } = default!;
    
    /// <summary>
    /// The speaker's display name if known.
    /// </summary>
    public string? SpeakerName { get; init; }
    
    /// <summary>
    /// Priority score for ranking (0-100, higher = more important).
    /// </summary>
    public double PriorityScore { get; init; }
    
    /// <summary>
    /// ID of the source utterance.
    /// </summary>
    public string SourceUtteranceId { get; init; } = default!;
    
    /// <summary>
    /// Categories/tags for the key point.
    /// </summary>
    public List<string> Categories { get; init; } = new();
}
