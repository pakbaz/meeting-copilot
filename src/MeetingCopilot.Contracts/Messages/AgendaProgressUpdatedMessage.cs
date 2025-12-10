using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Messages;

/// <summary>
/// Message emitted when an agenda item's status is updated.
/// </summary>
public record AgendaProgressUpdatedMessage : AgentMessage
{
    public string AgendaItemId { get; init; } = default!;
    public AgendaItemStatus NewStatus { get; init; }
}
