using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Events;

public record AgendaItemUpdatedEvent
{
    public string MeetingId { get; init; } = default!;
    public string AgendaItemId { get; init; } = default!;
    public AgendaItemStatus NewStatus { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
