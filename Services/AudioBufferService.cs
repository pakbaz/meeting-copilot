using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace meeting_copilot.Services;

/// <summary>
/// Service for buffering audio data during connection issues to enable recovery.
/// Maintains a circular buffer of recent audio samples that can be replayed
/// when connection is restored.
/// </summary>
public class AudioBufferService : IDisposable
{
    private readonly ILogger<AudioBufferService> _logger;
    private readonly ConcurrentDictionary<string, AudioBuffer> _buffers = new();
    private readonly int _maxBufferDurationSeconds;
    private bool _disposed;

    public AudioBufferService(ILogger<AudioBufferService> logger, int maxBufferDurationSeconds = 30)
    {
        _logger = logger;
        _maxBufferDurationSeconds = maxBufferDurationSeconds;
    }

    /// <summary>
    /// Creates a new buffer for a meeting session.
    /// </summary>
    public void CreateBuffer(string meetingId)
    {
        var buffer = new AudioBuffer(_maxBufferDurationSeconds);
        _buffers.TryAdd(meetingId, buffer);
        _logger.LogDebug("Created audio buffer for meeting {MeetingId}", meetingId);
    }

    /// <summary>
    /// Adds audio data to the buffer for a specific meeting.
    /// </summary>
    public void AddAudioChunk(string meetingId, byte[] audioData, DateTimeOffset timestamp)
    {
        if (_buffers.TryGetValue(meetingId, out var buffer))
        {
            buffer.Add(audioData, timestamp);
        }
    }

    /// <summary>
    /// Gets buffered audio data since a specific timestamp.
    /// Used for recovery after connection drops.
    /// </summary>
    public IEnumerable<BufferedAudioChunk> GetBufferedAudioSince(string meetingId, DateTimeOffset since)
    {
        if (_buffers.TryGetValue(meetingId, out var buffer))
        {
            return buffer.GetChunksSince(since);
        }
        return Enumerable.Empty<BufferedAudioChunk>();
    }

    /// <summary>
    /// Gets all buffered audio data for a meeting.
    /// </summary>
    public IEnumerable<BufferedAudioChunk> GetAllBufferedAudio(string meetingId)
    {
        if (_buffers.TryGetValue(meetingId, out var buffer))
        {
            return buffer.GetAllChunks();
        }
        return Enumerable.Empty<BufferedAudioChunk>();
    }

    /// <summary>
    /// Clears the buffer for a specific meeting.
    /// </summary>
    public void ClearBuffer(string meetingId)
    {
        if (_buffers.TryRemove(meetingId, out var buffer))
        {
            buffer.Clear();
            _logger.LogDebug("Cleared audio buffer for meeting {MeetingId}", meetingId);
        }
    }

    /// <summary>
    /// Gets the current buffer status for monitoring.
    /// </summary>
    public BufferStatus GetBufferStatus(string meetingId)
    {
        if (_buffers.TryGetValue(meetingId, out var buffer))
        {
            return buffer.GetStatus();
        }
        return new BufferStatus(false, 0, 0, null, null);
    }

    /// <summary>
    /// Marks a timestamp as the last confirmed processed point.
    /// Audio before this point can be safely discarded.
    /// </summary>
    public void ConfirmProcessed(string meetingId, DateTimeOffset processedUpTo)
    {
        if (_buffers.TryGetValue(meetingId, out var buffer))
        {
            buffer.ConfirmProcessed(processedUpTo);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var buffer in _buffers.Values)
            {
                buffer.Clear();
            }
            _buffers.Clear();
            _disposed = true;
        }
    }
}

public record BufferedAudioChunk(byte[] Data, DateTimeOffset Timestamp, int SequenceNumber);

public record BufferStatus(
    bool IsActive,
    int ChunkCount,
    long TotalBytes,
    DateTimeOffset? OldestChunk,
    DateTimeOffset? NewestChunk
);

internal class AudioBuffer
{
    private readonly LinkedList<BufferedAudioChunk> _chunks = new();
    private readonly object _lock = new();
    private readonly TimeSpan _maxDuration;
    private int _sequenceNumber;
    private DateTimeOffset? _lastConfirmedTimestamp;

    public AudioBuffer(int maxDurationSeconds)
    {
        _maxDuration = TimeSpan.FromSeconds(maxDurationSeconds);
    }

    public void Add(byte[] audioData, DateTimeOffset timestamp)
    {
        lock (_lock)
        {
            var chunk = new BufferedAudioChunk(audioData, timestamp, ++_sequenceNumber);
            _chunks.AddLast(chunk);
            
            // Remove old chunks beyond the buffer duration
            var cutoff = timestamp - _maxDuration;
            while (_chunks.First != null && _chunks.First.Value.Timestamp < cutoff)
            {
                _chunks.RemoveFirst();
            }
        }
    }

    public IEnumerable<BufferedAudioChunk> GetChunksSince(DateTimeOffset since)
    {
        lock (_lock)
        {
            return _chunks.Where(c => c.Timestamp >= since).ToList();
        }
    }

    public IEnumerable<BufferedAudioChunk> GetAllChunks()
    {
        lock (_lock)
        {
            return _chunks.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _chunks.Clear();
            _sequenceNumber = 0;
        }
    }

    public void ConfirmProcessed(DateTimeOffset processedUpTo)
    {
        lock (_lock)
        {
            _lastConfirmedTimestamp = processedUpTo;
            // Optionally trim confirmed chunks to save memory
            while (_chunks.First != null && _chunks.First.Value.Timestamp < processedUpTo)
            {
                _chunks.RemoveFirst();
            }
        }
    }

    public BufferStatus GetStatus()
    {
        lock (_lock)
        {
            if (_chunks.Count == 0)
            {
                return new BufferStatus(true, 0, 0, null, null);
            }

            var totalBytes = _chunks.Sum(c => (long)c.Data.Length);
            return new BufferStatus(
                true,
                _chunks.Count,
                totalBytes,
                _chunks.First?.Value.Timestamp,
                _chunks.Last?.Value.Timestamp
            );
        }
    }
}
