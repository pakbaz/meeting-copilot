using MeetingCopilot.Contracts.Messages;

namespace MeetingCopilot.Contracts.Interfaces;

public interface IAgent
{
    string Name { get; }
    int Priority { get; } // 1 = highest priority (AnswerAgent), higher numbers = lower priority
    
    /// <summary>
    /// Determines if this agent can handle the given message
    /// </summary>
    bool CanHandle(AgentMessage message);
    
    /// <summary>
    /// Process the message and optionally emit new messages
    /// </summary>
    Task<List<AgentMessage>> ProcessAsync(AgentMessage message, CancellationToken cancellationToken = default);
}
