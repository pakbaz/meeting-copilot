using MeetingCopilot.Contracts.Interfaces;
using MeetingCopilot.Contracts.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Agents;

public class AgentOrchestrator : BackgroundService
{
    private readonly IEnumerable<IAgent> _agents;
    private readonly AgentPriorityQueue _messageQueue;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IEnumerable<IAgent> agents,
        AgentPriorityQueue messageQueue,
        ILogger<AgentOrchestrator> logger)
    {
        _agents = agents;
        _messageQueue = messageQueue;
        _logger = logger;
    }

    public void EnqueueMessage(AgentMessage message)
    {
        _messageQueue.Enqueue(message);
        _logger.LogDebug("Message {MessageId} enqueued with priority {Priority}", message.Id, message.Priority);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentOrchestrator started with {Count} agents", _agents.Count());

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _messageQueue.DequeueAsync(stoppingToken);
                if (message == null) continue;

                _logger.LogInformation(
                    "Processing message {MessageId} from {Source} (Priority: {Priority})",
                    message.Id,
                    message.SourceAgent,
                    message.Priority);

                await ProcessMessageAsync(message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message in orchestrator");
            }
        }

        _logger.LogInformation("AgentOrchestrator stopped");
    }

    private async Task ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        var targetAgents = message.TargetAgent != null
            ? _agents.Where(a => a.Name == message.TargetAgent)
            : _agents.Where(a => a.CanHandle(message));

        var tasks = new List<Task<List<AgentMessage>>>();

        foreach (var agent in targetAgents)
        {
            _logger.LogDebug("Agent {AgentName} handling message {MessageId}", agent.Name, message.Id);
            tasks.Add(agent.ProcessAsync(message, cancellationToken));
        }

        if (!tasks.Any())
        {
            _logger.LogWarning("No agents found to handle message {MessageId}", message.Id);
            return;
        }

        // Wait for all agents to complete
        var results = await Task.WhenAll(tasks);

        // Enqueue any new messages produced by agents
        foreach (var newMessages in results)
        {
            foreach (var newMessage in newMessages)
            {
                EnqueueMessage(newMessage);
            }
        }
    }

    public int QueueLength => _messageQueue.Count;
}
