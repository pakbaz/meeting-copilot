using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using meeting_copilot.Data.Repositories;
using meeting_copilot.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace meeting_copilot.Agents;

public class AgentOrchestrator
{
    private readonly AgentCatalog _agents;
    private readonly IChatClient _chatClient;
    private readonly KeypointRepository _keypoints;
    private readonly GuestInfoRepository _guestInfo;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly SemaphoreSlim _keypointLock = new(1, 1);
    private readonly SemaphoreSlim _speakerLock = new(1, 1);

    public AgentOrchestrator(
        AgentCatalog agents,
        KeypointRepository keypoints,
        GuestInfoRepository guestInfo,
        ILogger<AgentOrchestrator> logger)
    {
        _agents = agents;
        _chatClient = agents.ChatClient;
        _keypoints = keypoints;
        _guestInfo = guestInfo;
        _logger = logger;
    }

    public AIAgent KeypointAgent => _agents.KeypointAgent;

    public AIAgent SpeakerAgent => _agents.SpeakerAgent;

    public async Task ProcessTranscriptAsync(TranscriptionResult result, CancellationToken cancellationToken = default)
    {
        if (!result.IsFinal || string.IsNullOrWhiteSpace(result.Text))
        {
            return;
        }

        try
        {
            await Task.WhenAll(
                ProcessKeypointsAsync(result, cancellationToken),
                ProcessSpeakerIdentificationAsync(result, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Agent processing failed for transcript chunk.");
        }
    }

    public Task UpdateSpeakerAsync(string guestId, string guestName, string jobTitle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(guestId))
        {
            return Task.CompletedTask;
        }

        return _guestInfo.UpsertAsync(guestId, guestName, jobTitle, cancellationToken);
    }

    private async Task ProcessKeypointsAsync(TranscriptionResult result, CancellationToken cancellationToken)
    {
        await _keypointLock.WaitAsync(cancellationToken);
        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "Always respond with JSON matching schema: {\"items\":[{\"guestId\":string,\"point\":string,\"todo\":bool,\"suggestedBy\":string,\"needsFollowUp\":bool}]}. Return empty items when nothing relevant."),
                new(ChatRole.User, JsonSerializer.Serialize(new KeypointPrompt(result)))
            };

            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            await HandleKeypointResponseAsync(ExtractTextContent(response), cancellationToken);
        }
        finally
        {
            _keypointLock.Release();
        }
    }

    private async Task ProcessSpeakerIdentificationAsync(TranscriptionResult result, CancellationToken cancellationToken)
    {
        await _speakerLock.WaitAsync(cancellationToken);
        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "Return JSON {\"guestId\":string,\"guestName\":string,\"jobTitle\":string,\"confidence\":0-1}. Use guestId from transcript, infer name/title if possible; otherwise omit fields by returning empty strings."),
                new(ChatRole.User, JsonSerializer.Serialize(new SpeakerPrompt(result)))
            };

            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
            await HandleSpeakerResponseAsync(ExtractTextContent(response), cancellationToken);
        }
        finally
        {
            _speakerLock.Release();
        }
    }

    private static string ExtractTextContent(ChatResponse response)
    {
        if (response?.Messages is null || response.Messages.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var lastMessage = response.Messages[response.Messages.Count - 1];
        foreach (var content in lastMessage.Contents)
        {
            if (content is TextContent textContent && !string.IsNullOrWhiteSpace(textContent.Text))
            {
                builder.Append(textContent.Text);
            }
        }

        return builder.ToString();
    }

    private async Task HandleKeypointResponseAsync(string response, CancellationToken cancellationToken)
    {
        var payloadJson = response?.Trim();
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<KeypointAgentResponse>(payloadJson);
            if (payload?.Items is null)
            {
                return;
            }

            foreach (var item in payload.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Point))
                {
                    continue;
                }

                var keypoint = new meeting_copilot.Data.Entities.Keypoint
                {
                    Timestamp = item.Timestamp ?? DateTime.UtcNow,
                    GuestId = item.GuestId ?? "Unknown",
                    Todo = item.Todo,
                    Point = item.Point,
                    SuggestedBy = item.SuggestedBy ?? item.GuestId ?? "Unknown",
                    NeedsFollowUp = item.NeedsFollowUp
                };

                await _keypoints.AddAsync(keypoint, cancellationToken);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse keypoint agent response: {Response}", payloadJson);
        }
    }

    private async Task HandleSpeakerResponseAsync(string response, CancellationToken cancellationToken)
    {
        var payloadJson = response?.Trim();
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SpeakerAgentResponse>(payloadJson);
            if (payload is null || string.IsNullOrWhiteSpace(payload.GuestId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(payload.GuestName) && string.IsNullOrWhiteSpace(payload.JobTitle))
            {
                return;
            }

            await _guestInfo.UpsertAsync(
                payload.GuestId,
                payload.GuestName ?? string.Empty,
                payload.JobTitle ?? string.Empty,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse speaker agent response: {Response}", payloadJson);
        }
    }

    private record KeypointPrompt
    {
        public KeypointPrompt(TranscriptionResult result)
        {
            Transcript = result.Text;
            GuestId = result.SpeakerId;
            Timestamp = result.Timestamp;
        }

        [JsonPropertyName("transcript")]
        public string Transcript { get; }

        [JsonPropertyName("guestId")]
        public string GuestId { get; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; }
    }

    private record SpeakerPrompt
    {
        public SpeakerPrompt(TranscriptionResult result)
        {
            Transcript = result.Text;
            GuestId = result.SpeakerId;
            Timestamp = result.Timestamp;
        }

        [JsonPropertyName("transcript")]
        public string Transcript { get; }

        [JsonPropertyName("guestId")]
        public string GuestId { get; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; }
    }

    private sealed class KeypointAgentResponse
    {
        [JsonPropertyName("items")]
        public List<KeypointAgentItem>? Items { get; set; }
    }

    private sealed class KeypointAgentItem
    {
        [JsonPropertyName("guestId")]
        public string? GuestId { get; set; }

        [JsonPropertyName("point")]
        public string? Point { get; set; }

        [JsonPropertyName("todo")]
        public bool Todo { get; set; }

        [JsonPropertyName("suggestedBy")]
        public string? SuggestedBy { get; set; }

        [JsonPropertyName("needsFollowUp")]
        public bool NeedsFollowUp { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
    }

    private sealed class SpeakerAgentResponse
    {
        [JsonPropertyName("guestId")]
        public string? GuestId { get; set; }

        [JsonPropertyName("guestName")]
        public string? GuestName { get; set; }

        [JsonPropertyName("jobTitle")]
        public string? JobTitle { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
}
