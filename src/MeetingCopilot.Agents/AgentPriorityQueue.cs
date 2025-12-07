using MeetingCopilot.Contracts.Messages;
using System.Collections.Concurrent;

namespace MeetingCopilot.Agents;

public class AgentPriorityQueue
{
    private readonly SortedSet<QueuedMessage> _queue;
    private readonly SemaphoreSlim _semaphore;
    private readonly object _lock = new();

    public AgentPriorityQueue()
    {
        _queue = new SortedSet<QueuedMessage>(new MessagePriorityComparer());
        _semaphore = new SemaphoreSlim(0);
    }

    public void Enqueue(AgentMessage message)
    {
        lock (_lock)
        {
            _queue.Add(new QueuedMessage(message, DateTimeOffset.UtcNow));
        }
        _semaphore.Release();
    }

    public async Task<AgentMessage?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                var item = _queue.First();
                _queue.Remove(item);
                return item.Message;
            }
        }

        return null;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    private record QueuedMessage(AgentMessage Message, DateTimeOffset EnqueuedAt);

    private class MessagePriorityComparer : IComparer<QueuedMessage>
    {
        public int Compare(QueuedMessage? x, QueuedMessage? y)
        {
            if (x == null || y == null) return 0;

            // First compare by priority (lower number = higher priority)
            var priorityComparison = x.Message.Priority.CompareTo(y.Message.Priority);
            if (priorityComparison != 0) return priorityComparison;

            // If same priority, FIFO by enqueue time
            var timeComparison = x.EnqueuedAt.CompareTo(y.EnqueuedAt);
            if (timeComparison != 0) return timeComparison;

            // Fallback to ID to ensure uniqueness in SortedSet
            return string.Compare(x.Message.Id, y.Message.Id, StringComparison.Ordinal);
        }
    }
}
