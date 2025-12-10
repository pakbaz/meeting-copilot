namespace MeetingCopilot.Contracts.Messages;

public record UtteranceTranscribedMessage : AgentMessage
{
    public string SpeakerId { get; init; } = default!;
    public string Text { get; init; } = default!;
    public double Confidence { get; init; } = 1.0;
}
