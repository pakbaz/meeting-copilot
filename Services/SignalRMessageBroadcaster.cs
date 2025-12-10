using MeetingCopilot.Agents;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Events;
using MeetingCopilot.Contracts.Messages;
using meeting_copilot.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace meeting_copilot.Services;

/// <summary>
/// SignalR broadcaster that converts agent messages to SignalR events for real-time UI updates.
/// </summary>
public class SignalRMessageBroadcaster : IAgentMessageBroadcaster
{
    private readonly IHubContext<MeetingHub> _hubContext;
    private readonly ILogger<SignalRMessageBroadcaster> _logger;

    public SignalRMessageBroadcaster(
        IHubContext<MeetingHub> hubContext,
        ILogger<SignalRMessageBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastAsync(AgentMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (message)
            {
                case UtteranceProcessedMessage utterance:
                    await BroadcastUtteranceProcessedAsync(utterance, cancellationToken);
                    break;

                case AnswerReadyMessage answer:
                    await BroadcastAnswerReadyAsync(answer, cancellationToken);
                    break;

                case KeyPointExtractedMessage keyPoint:
                    await BroadcastKeyPointExtractedAsync(keyPoint, cancellationToken);
                    break;

                case ResearchReadyMessage research:
                    await BroadcastResearchReadyAsync(research, cancellationToken);
                    break;

                case QuestionDetectedMessage question:
                    await BroadcastQuestionDetectedAsync(question, cancellationToken);
                    break;

                case AgendaProgressUpdatedMessage agenda:
                    await BroadcastAgendaItemUpdatedAsync(agenda, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast message {MessageId}", message.Id);
        }
    }

    private async Task BroadcastUtteranceProcessedAsync(
        UtteranceProcessedMessage utterance,
        CancellationToken cancellationToken)
    {
        var transcriptionEvent = new TranscriptionEvent
        {
            MeetingId = utterance.MeetingId,
            UtteranceId = utterance.Interaction.Id,
            SpeakerId = utterance.Interaction.SpeakerId ?? "unknown",
            SpeakerName = utterance.Interaction.SpeakerName ?? "Unknown",
            Content = utterance.Interaction.Content,
            Confidence = utterance.Interaction.Confidence ?? 1.0,
            Timestamp = utterance.Interaction.Timestamp,
            IsFinal = true
        };

        await _hubContext.Clients.Group(utterance.MeetingId)
            .SendAsync("UtteranceProcessed", transcriptionEvent, cancellationToken);

        _logger.LogDebug("Broadcast UtteranceProcessed for meeting {MeetingId}", utterance.MeetingId);
    }

    private async Task BroadcastAnswerReadyAsync(
        AnswerReadyMessage answer,
        CancellationToken cancellationToken)
    {
        var answerEvent = new AnswerEvent
        {
            MeetingId = answer.MeetingId,
            QuestionId = answer.QuestionMessageId,
            AnswerId = Guid.NewGuid().ToString(),
            Question = answer.QuestionText,
            Answer = answer.AnswerText,
            Confidence = answer.Confidence,
            Sources = answer.Sources ?? new(),
            ProcessingTimeMs = (int)answer.ProcessingTimeMs,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.Group(answer.MeetingId)
            .SendAsync("AnswerReady", answerEvent, cancellationToken);

        _logger.LogDebug("Broadcast AnswerReady for question {QuestionId} in meeting {MeetingId}", 
            answer.QuestionMessageId, answer.MeetingId);
    }

    private async Task BroadcastKeyPointExtractedAsync(
        KeyPointExtractedMessage keyPoint,
        CancellationToken cancellationToken)
    {
        var keyPointsEvent = new KeyPointsEvent
        {
            MeetingId = keyPoint.MeetingId,
            KeyPoints = new List<KeyPointItem>
            {
                new KeyPointItem
                {
                    Id = keyPoint.KeyPointId,
                    Title = keyPoint.Title,
                    Content = keyPoint.Content,
                    PriorityScore = keyPoint.PriorityScore,
                    SourceUtteranceId = keyPoint.SourceUtteranceId
                }
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.Group(keyPoint.MeetingId)
            .SendAsync("KeyPointsUpdated", keyPointsEvent, cancellationToken);

        _logger.LogDebug("Broadcast KeyPointsUpdated for meeting {MeetingId}: {Title}", 
            keyPoint.MeetingId, keyPoint.Title);
    }

    private async Task BroadcastResearchReadyAsync(
        ResearchReadyMessage research,
        CancellationToken cancellationToken)
    {
        var researchEvent = new ResearchEvent
        {
            MeetingId = research.MeetingId,
            ResearchId = research.Id,
            Query = research.Query,
            Summary = research.Summary ?? string.Empty,
            Sources = research.Results.Select(r => new WebSourceItem
            {
                Title = r.Title,
                Url = r.Url ?? string.Empty,
                Snippet = r.Snippet
            }).ToList(),
            Timestamp = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.Group(research.MeetingId)
            .SendAsync("ResearchReady", researchEvent, cancellationToken);

        _logger.LogDebug("Broadcast ResearchReady for query '{Query}' in meeting {MeetingId}", 
            research.Query, research.MeetingId);
    }

    private async Task BroadcastQuestionDetectedAsync(
        QuestionDetectedMessage question,
        CancellationToken cancellationToken)
    {
        var questionEvent = new QuestionDetectedEvent
        {
            MeetingId = question.MeetingId,
            QuestionId = question.Id,
            QuestionText = question.QuestionText,
            SpeakerId = question.SpeakerId,
            SpeakerName = question.SpeakerName ?? "Unknown",
            QuestionType = question.QuestionType,
            DetectionConfidence = question.Confidence,
            AnswerPriority = CalculateAnswerPriority(question),
            Timestamp = question.Timestamp
        };

        await _hubContext.Clients.Group(question.MeetingId)
            .SendAsync("QuestionDetected", questionEvent, cancellationToken);

        _logger.LogDebug("Broadcast QuestionDetected in meeting {MeetingId}: {Question}", 
            question.MeetingId, question.QuestionText.Substring(0, Math.Min(50, question.QuestionText.Length)));
    }

    private async Task BroadcastAgendaItemUpdatedAsync(
        AgendaProgressUpdatedMessage agenda,
        CancellationToken cancellationToken)
    {
        var agendaEvent = new AgendaItemUpdatedEvent
        {
            MeetingId = agenda.MeetingId,
            AgendaItemId = agenda.AgendaItemId,
            NewStatus = agenda.NewStatus,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.Group(agenda.MeetingId)
            .SendAsync("AgendaItemUpdated", agendaEvent, cancellationToken);

        _logger.LogDebug("Broadcast AgendaItemUpdated for item {ItemId} in meeting {MeetingId}: {Status}", 
            agenda.AgendaItemId, agenda.MeetingId, agenda.NewStatus);
    }

    /// <summary>
    /// Broadcasts connection status changes for degraded mode indication.
    /// </summary>
    public async Task BroadcastConnectionStatusAsync(
        string meetingId,
        ConnectionStatusEvent statusEvent,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(meetingId)
            .SendAsync("ConnectionStatusChanged", statusEvent, cancellationToken);

        _logger.LogDebug("Broadcast ConnectionStatusChanged for meeting {MeetingId}: {Status}", 
            meetingId, statusEvent.Status);
    }

    /// <summary>
    /// Calculates answer priority based on speaker and question properties.
    /// </summary>
    private static int CalculateAnswerPriority(QuestionDetectedMessage question)
    {
        // Base priority
        var priority = 50;

        // Higher confidence = higher priority
        priority += (int)(question.Confidence * 20);

        // Question type modifiers
        if (question.QuestionType?.Equals("clarification", StringComparison.OrdinalIgnoreCase) == true)
        {
            priority += 10; // Clarifications are more urgent
        }

        return Math.Min(100, priority);
    }
}
