namespace MeetingCopilot.Contracts.Events;

/// <summary>
/// Event indicating a question was detected in the transcript.
/// </summary>
public record QuestionDetectedEvent
{
    /// <summary>
    /// The meeting ID where the question was detected.
    /// </summary>
    public string MeetingId { get; init; } = default!;

    /// <summary>
    /// Unique identifier for this question.
    /// </summary>
    public string QuestionId { get; init; } = default!;

    /// <summary>
    /// The detected question text.
    /// </summary>
    public string QuestionText { get; init; } = default!;

    /// <summary>
    /// Speaker ID who asked the question.
    /// </summary>
    public string SpeakerId { get; init; } = default!;

    /// <summary>
    /// Display name of the speaker.
    /// </summary>
    public string SpeakerName { get; init; } = default!;

    /// <summary>
    /// Type of question detected (direct, rhetorical, clarification, etc.).
    /// </summary>
    public string QuestionType { get; init; } = "direct";

    /// <summary>
    /// Confidence score for question detection (0.0 to 1.0).
    /// </summary>
    public double DetectionConfidence { get; init; }

    /// <summary>
    /// Priority ranking for answering (higher = more important).
    /// </summary>
    public int AnswerPriority { get; init; }

    /// <summary>
    /// Timestamp when the question was detected.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
