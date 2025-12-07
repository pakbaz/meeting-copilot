using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Messages;

public record UtteranceProcessedMessage : AgentMessage
{
    public Interaction Interaction { get; init; } = default!;
}
