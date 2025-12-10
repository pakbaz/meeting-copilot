namespace MeetingCopilot.Contracts.Messages;

/// <summary>
/// Message indicating an answer has been generated for a detected question.
/// </summary>
public record AnswerReadyMessage : AgentMessage
{
    /// <summary>
    /// Reference to the original question message ID.
    /// </summary>
    public string QuestionMessageId { get; init; } = default!;
    
    /// <summary>
    /// The original question text.
    /// </summary>
    public string QuestionText { get; init; } = default!;
    
    /// <summary>
    /// The generated answer text.
    /// </summary>
    public string AnswerText { get; init; } = default!;
    
    /// <summary>
    /// Confidence score for the answer (0-1).
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Sources used to generate the answer.
    /// </summary>
    public List<string> Sources { get; init; } = new();
    
    /// <summary>
    /// The speaker ID who asked the question.
    /// </summary>
    public string QuestionerSpeakerId { get; init; } = default!;
    
    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; init; }
}
