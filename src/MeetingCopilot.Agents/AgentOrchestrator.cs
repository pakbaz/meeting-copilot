using MeetingCopilot.Contracts.Interfaces;
using MeetingCopilot.Contracts.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Agents;

/// <summary>
/// Interface for broadcasting agent messages to external systems (e.g., SignalR).
/// </summary>
public interface IAgentMessageBroadcaster
{
    Task BroadcastAsync(AgentMessage message, CancellationToken cancellationToken = default);
}

public class AgentOrchestrator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AgentPriorityQueue _messageQueue;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly IAgentMessageBroadcaster? _broadcaster;

    public AgentOrchestrator(
        IServiceScopeFactory scopeFactory,
        AgentPriorityQueue messageQueue,
        ILogger<AgentOrchestrator> logger,
        IAgentMessageBroadcaster? broadcaster = null)
    {
        _scopeFactory = scopeFactory;
        _messageQueue = messageQueue;
        _logger = logger;
        _broadcaster = broadcaster;
    }

    public void EnqueueMessage(AgentMessage message)
    {
        _messageQueue.Enqueue(message);
        _logger.LogDebug("Message {MessageId} enqueued with priority {Priority}", message.Id, message.Priority);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentOrchestrator started");

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
        // Create a new scope for each message to properly resolve scoped dependencies
        using var scope = _scopeFactory.CreateScope();
        var agents = scope.ServiceProvider.GetServices<IAgent>();

        var targetAgents = message.TargetAgent != null
            ? agents.Where(a => a.Name == message.TargetAgent)
            : agents.Where(a => a.CanHandle(message));

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

        // Enqueue any new messages produced by agents and broadcast them
        foreach (var newMessages in results)
        {
            foreach (var newMessage in newMessages)
            {
                EnqueueMessage(newMessage);
                
                // Broadcast to external systems (e.g., SignalR)
                if (_broadcaster != null)
                {
                    try
                    {
                        await _broadcaster.BroadcastAsync(newMessage, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to broadcast message {MessageId}", newMessage.Id);
                    }
                }
            }
        }
    }

    public int QueueLength => _messageQueue.Count;
}
