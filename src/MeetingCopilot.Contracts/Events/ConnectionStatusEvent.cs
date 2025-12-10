namespace MeetingCopilot.Contracts.Events;

/// <summary>
/// Event indicating connection status changes for degraded mode indication.
/// </summary>
public record ConnectionStatusEvent
{
    /// <summary>
    /// The meeting ID this status applies to.
    /// </summary>
    public string MeetingId { get; init; } = default!;

    /// <summary>
    /// Current connection status.
    /// </summary>
    public ConnectionStatus Status { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string Message { get; init; } = default!;

    /// <summary>
    /// Whether transcription is affected.
    /// </summary>
    public bool TranscriptionAffected { get; init; }

    /// <summary>
    /// Whether AI features are affected.
    /// </summary>
    public bool AiFeaturesAffected { get; init; }

    /// <summary>
    /// Estimated time to recovery in seconds, if known.
    /// </summary>
    public int? EstimatedRecoverySeconds { get; init; }

    /// <summary>
    /// Timestamp of the status change.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Connection status values.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// All services connected and operational.
    /// </summary>
    Connected,

    /// <summary>
    /// Reconnecting after a connection drop.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// Operating in degraded mode - some features unavailable.
    /// </summary>
    Degraded,

    /// <summary>
    /// Disconnected - buffering audio locally.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connection error - manual intervention may be needed.
    /// </summary>
    Error
}
