namespace MeetingCopilot.Contracts.Messages;

/// <summary>
/// Message indicating a question has been detected in the transcript.
/// </summary>
public record QuestionDetectedMessage : AgentMessage
{
    /// <summary>
    /// The ID of the utterance containing the question.
    /// </summary>
    public string UtteranceId { get; init; } = default!;
    
    /// <summary>
    /// The text of the detected question.
    /// </summary>
    public string QuestionText { get; init; } = default!;
    
    /// <summary>
    /// The speaker ID who asked the question.
    /// </summary>
    public string SpeakerId { get; init; } = default!;
    
    /// <summary>
    /// The speaker's display name if known.
    /// </summary>
    public string? SpeakerName { get; init; }
    
    /// <summary>
    /// Confidence score for question detection (0-1).
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Type of question (factual, opinion, clarification, action, etc.)
    /// </summary>
    public string QuestionType { get; init; } = "unknown";
    
    /// <summary>
    /// Priority rank of the speaker (1 = highest).
    /// Used for prioritizing question answering.
    /// </summary>
    public int SpeakerPriority { get; init; } = 10;
}
