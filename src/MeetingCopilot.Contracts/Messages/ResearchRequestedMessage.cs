namespace MeetingCopilot.Contracts.Messages;

/// <summary>
/// Message requesting research on a specific topic.
/// </summary>
public record ResearchRequestedMessage : AgentMessage
{
    /// <summary>
    /// The topic or query to research.
    /// </summary>
    public string Query { get; init; } = default!;
    
    /// <summary>
    /// How the research was triggered.
    /// </summary>
    public ResearchTrigger Trigger { get; init; } = ResearchTrigger.Manual;
    
    /// <summary>
    /// ID of the source utterance that triggered research.
    /// </summary>
    public string? SourceUtteranceId { get; init; }
    
    /// <summary>
    /// Speaker ID who requested or triggered the research.
    /// </summary>
    public string? RequestingSpeakerId { get; init; }
    
    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int MaxResults { get; init; } = 5;
}

public enum ResearchTrigger
{
    /// <summary>Manual slash command /research</summary>
    Manual,
    
    /// <summary>Automatic detection of unfamiliar topic</summary>
    AutoDetect,
    
    /// <summary>Question that requires external context</summary>
    Question
}
