using Azure.AI.OpenAI;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using MeetingCopilot.Contracts.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace MeetingCopilot.Agents.SummaryAgent;

/// <summary>
/// Agent responsible for extracting key points and tracking agenda progress.
/// Priority: Medium (5) - Runs after transcript processing.
/// </summary>
public class SummaryAgent : IAgent
{
    private readonly IInsightRepository _insightRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly ChatClient _chatClient;
    private readonly ILogger<SummaryAgent> _logger;

    public string Name => "SummaryAgent";
    public int Priority => 5; // Medium priority

    public SummaryAgent(
        IInsightRepository insightRepository,
        IMeetingRepository meetingRepository,
        AzureOpenAIClient azureClient,
        IConfiguration configuration,
        ILogger<SummaryAgent> logger)
    {
        _insightRepository = insightRepository;
        _meetingRepository = meetingRepository;
        _logger = logger;

        var modelName = configuration["AzureAI:Model"] ?? "gpt-4o-mini";
        _chatClient = azureClient.GetChatClient(modelName);
    }

    public bool CanHandle(AgentMessage message)
    {
        return message is UtteranceProcessedMessage or ExtractKeyPointsMessage or UpdateAgendaMessage;
    }

    public async Task<List<AgentMessage>> ProcessAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        var result = new List<AgentMessage>();

        try
        {
            if (message is UtteranceProcessedMessage utterance)
            {
                await ProcessUtteranceAsync(utterance, result, cancellationToken);
            }
            else if (message is ExtractKeyPointsMessage extractRequest)
            {
                await ExtractKeyPointsAsync(extractRequest, result, cancellationToken);
            }
            else if (message is UpdateAgendaMessage agendaUpdate)
            {
                await UpdateAgendaProgressAsync(agendaUpdate, result, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in SummaryAgent");
        }

        return result;
    }

    private async Task ProcessUtteranceAsync(
        UtteranceProcessedMessage utterance,
        List<AgentMessage> result,
        CancellationToken cancellationToken)
    {
        // Analyze utterance for key points every 5 utterances or significant content
        if (ShouldExtractKeyPoints(utterance.Interaction.Content))
        {
            var keyPoints = await ExtractKeyPointsFromTextAsync(
                utterance.MeetingId,
                utterance.UserId,
                utterance.Interaction.Content,
                cancellationToken);

            foreach (var keyPoint in keyPoints)
            {
                result.Add(new MeetingCopilot.Contracts.Messages.KeyPointExtractedMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    MeetingId = utterance.MeetingId,
                    UserId = utterance.UserId,
                    SourceAgent = Name,
                    Priority = 5, // Medium priority for key points
                    Timestamp = DateTimeOffset.UtcNow,
                    KeyPointId = keyPoint.Id,
                    Title = keyPoint.Title,
                    Content = keyPoint.Content ?? string.Empty,
                    SpeakerId = keyPoint.SourceUtteranceId ?? "unknown",
                    SpeakerName = null,
                    PriorityScore = keyPoint.PriorityScore ?? 50,
                    SourceUtteranceId = keyPoint.SourceUtteranceId ?? string.Empty,
                    Categories = new List<string>()
                });
            }
        }

        // Check for agenda item updates
        var meeting = await _meetingRepository.GetByIdAsync(utterance.MeetingId, cancellationToken);
        if (meeting?.AgendaItems?.Count > 0)
        {
            await CheckAgendaProgressAsync(meeting, utterance.Interaction.Content, result, cancellationToken);
        }
    }

    private bool ShouldExtractKeyPoints(string content)
    {
        // Extract key points for substantial content or decision-related phrases
        var decisionPhrases = new[]
        {
            "we decided", "let's go with", "agreed", "the plan is",
            "action item", "next step", "deadline", "important",
            "key point", "to summarize", "in conclusion"
        };

        return content.Length > 100 ||
               decisionPhrases.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<Insight>> ExtractKeyPointsFromTextAsync(
        string meetingId,
        string userId,
        string text,
        CancellationToken cancellationToken)
    {
        var keyPoints = new List<Insight>();

        var systemPrompt = @"You are a meeting analyst. Extract key points from the text.
Return a JSON array of key points, each with:
- title: Short summary (5-10 words)
- content: Detailed description
- priority: 1-10 (10 = most important)
- type: 'decision', 'action', 'insight', or 'question'

Only extract genuinely important points. Return empty array if nothing significant.
Format: [{""title"": ""..."", ""content"": ""..."", ""priority"": N, ""type"": ""...""}]";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage($"Extract key points from:\n\n{text}")
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.2f,
            MaxOutputTokenCount = 500
        };

        try
        {
            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var responseText = response.Value.Content[0].Text;

            // Parse JSON response
            var extracted = ParseKeyPointsJson(responseText);

            foreach (var kp in extracted)
            {
                var insight = new Insight
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    MeetingId = meetingId,
                    Type = MapToInsightType(kp.Type),
                    Title = kp.Title,
                    Content = kp.Content,
                    Embedding = null, // Will be populated by embedding service
                    PriorityScore = kp.Priority * 10, // Convert 1-10 to 0-100
                    SourceUtteranceId = null,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Ttl = null
                };

                await _insightRepository.CreateAsync(insight, cancellationToken);
                keyPoints.Add(insight);

                _logger.LogInformation(
                    "Extracted key point: {Title} (Priority: {Priority})",
                    insight.Title,
                    insight.PriorityScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting key points from text");
        }

        return keyPoints;
    }

    private List<(string Title, string Content, int Priority, string Type)> ParseKeyPointsJson(string json)
    {
        var result = new List<(string, string, int, string)>();

        try
        {
            // Clean up potential markdown code blocks
            json = json.Trim();
            if (json.StartsWith("```"))
            {
                json = json.Substring(json.IndexOf('['));
                json = json.Substring(0, json.LastIndexOf(']') + 1);
            }

            using var doc = JsonDocument.Parse(json);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var title = element.GetProperty("title").GetString() ?? "";
                var content = element.GetProperty("content").GetString() ?? "";
                var priority = element.TryGetProperty("priority", out var p) ? p.GetInt32() : 5;
                var type = element.TryGetProperty("type", out var t) ? t.GetString() ?? "insight" : "insight";

                result.Add((title, content, priority, type));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing key points JSON: {Json}", json);
        }

        return result;
    }

    private InsightType MapToInsightType(string type) => type.ToLowerInvariant() switch
    {
        "decision" => InsightType.KeyPoint,
        "action" => InsightType.ActionItem,
        "question" => InsightType.ParkingLot, // Questions go to parking lot for follow-up
        _ => InsightType.KeyPoint
    };

    private async Task ExtractKeyPointsAsync(
        ExtractKeyPointsMessage request,
        List<AgentMessage> result,
        CancellationToken cancellationToken)
    {
        var keyPoints = await ExtractKeyPointsFromTextAsync(
            request.MeetingId,
            request.UserId,
            request.Text,
            cancellationToken);

        foreach (var keyPoint in keyPoints)
        {
            result.Add(new MeetingCopilot.Contracts.Messages.KeyPointExtractedMessage
            {
                Id = Guid.NewGuid().ToString(),
                MeetingId = request.MeetingId,
                UserId = request.UserId,
                SourceAgent = Name,
                Priority = 5,
                Timestamp = DateTimeOffset.UtcNow,
                KeyPointId = keyPoint.Id,
                Title = keyPoint.Title,
                Content = keyPoint.Content ?? string.Empty,
                SpeakerId = keyPoint.SourceUtteranceId ?? "unknown",
                SpeakerName = null,
                PriorityScore = keyPoint.PriorityScore ?? 50,
                SourceUtteranceId = keyPoint.SourceUtteranceId ?? string.Empty,
                Categories = new List<string>()
            });
        }
    }

    private async Task CheckAgendaProgressAsync(
        Meeting meeting,
        string content,
        List<AgentMessage> result,
        CancellationToken cancellationToken)
    {
        // Simple heuristic: check if content mentions agenda items
        foreach (var item in meeting.AgendaItems.Where(a => a.Status != AgendaItemStatus.Completed))
        {
            var titleWords = item.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchCount = titleWords.Count(w =>
                content.Contains(w, StringComparison.OrdinalIgnoreCase));

            var matchRatio = titleWords.Length > 0 ? (double)matchCount / titleWords.Length : 0;

            if (matchRatio >= 0.5 && item.Status == AgendaItemStatus.Pending)
            {
                // Mark as in progress
                result.Add(new AgendaProgressUpdatedMessage
                {
                    MeetingId = meeting.Id,
                    UserId = meeting.UserId,
                    SourceAgent = Name,
                    AgendaItemId = item.Id,
                    NewStatus = AgendaItemStatus.InProgress
                });

                _logger.LogInformation("Detected agenda item in progress: {Title}", item.Title);
            }
        }
    }

    private async Task UpdateAgendaProgressAsync(
        UpdateAgendaMessage request,
        List<AgentMessage> result,
        CancellationToken cancellationToken)
    {
        var meeting = await _meetingRepository.GetByIdAsync(request.MeetingId, cancellationToken);
        if (meeting == null) return;

        var itemIndex = meeting.AgendaItems.FindIndex(a => a.Id == request.AgendaItemId);
        if (itemIndex < 0) return;

        var updatedItems = meeting.AgendaItems.ToList();
        updatedItems[itemIndex] = updatedItems[itemIndex] with
        {
            Status = request.NewStatus,
            CompletedAt = request.NewStatus == AgendaItemStatus.Completed
                ? DateTimeOffset.UtcNow
                : updatedItems[itemIndex].CompletedAt
        };

        var updatedMeeting = meeting with { AgendaItems = updatedItems };
        await _meetingRepository.UpdateAsync(updatedMeeting, cancellationToken);

        result.Add(new AgendaProgressUpdatedMessage
        {
            MeetingId = meeting.Id,
            UserId = meeting.UserId,
            SourceAgent = Name,
            AgendaItemId = request.AgendaItemId,
            NewStatus = request.NewStatus
        });
    }
}

/// <summary>
/// Request to extract key points from text.
/// </summary>
public record ExtractKeyPointsMessage : AgentMessage
{
    public string Text { get; init; } = default!;
}

/// <summary>
/// Request to update agenda item status.
/// </summary>
public record UpdateAgendaMessage : AgentMessage
{
    public string AgendaItemId { get; init; } = default!;
    public AgendaItemStatus NewStatus { get; init; }
}
