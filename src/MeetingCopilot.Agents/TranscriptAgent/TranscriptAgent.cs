using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Events;
using MeetingCopilot.Contracts.Interfaces;
using MeetingCopilot.Contracts.Messages;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MeetingCopilot.Agents.TranscriptAgent;

/// <summary>
/// Agent responsible for processing real-time transcription and speaker diarization
/// </summary>
public class TranscriptAgent : IAgent
{
    private readonly IInteractionRepository _interactionRepository;
    private readonly ISpeakerRepository _speakerRepository;
    private readonly ILogger<TranscriptAgent> _logger;

    // Question detection patterns
    private static readonly (string Pattern, double Confidence)[] QuestionPatterns = new[]
    {
        (@"^(?:what|who|where|when|why|how|which|whose|whom)\b", 0.95),
        (@"^(?:can|could|would|should|will|do|does|did|is|are|was|were|have|has|had)\b.*\?", 0.9),
        (@"\?$", 0.85),
        (@"(?:do you know|can you tell|could you explain|would you mind)", 0.8),
    };

    public string Name => "TranscriptAgent";
    public int Priority => 10; // High priority for real-time processing

    public TranscriptAgent(
        IInteractionRepository interactionRepository,
        ISpeakerRepository speakerRepository,
        ILogger<TranscriptAgent> logger)
    {
        _interactionRepository = interactionRepository;
        _speakerRepository = speakerRepository;
        _logger = logger;
    }

    public bool CanHandle(AgentMessage message)
    {
        // Handle transcription events
        return message is UtteranceTranscribedMessage;
    }

    public async Task<List<AgentMessage>> ProcessAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        var result = new List<AgentMessage>();
        
        if (message is not UtteranceTranscribedMessage transcription)
        {
            _logger.LogWarning("Invalid message type: {Type}", message.GetType().Name);
            return result;
        }

        try
        {
            _logger.LogInformation(
                "Processing transcription for speaker {SpeakerId}: {Text}",
                transcription.SpeakerId,
                transcription.Text.Substring(0, Math.Min(50, transcription.Text.Length)));

            // Ensure speaker exists
            var speaker = await EnsureSpeakerExistsAsync(
                transcription.MeetingId,
                transcription.SpeakerId,
                transcription.UserId,
                cancellationToken);

            // Store interaction (without ContentVector for now, will be added by embedding service)
            var interaction = new Interaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = transcription.UserId,
                MeetingId = transcription.MeetingId,
                Type = InteractionType.Utterance,
                Content = transcription.Text,
                ContentVector = null, // Will be populated by embedding service
                SpeakerId = speaker.Id,
                SpeakerName = speaker.DisplayName ?? speaker.SpeakerLabel,
                QuestionId = null,
                Confidence = transcription.Confidence,
                Sources = null,
                AgentName = Name,
                ProcessingTimeMs = null,
                Timestamp = transcription.Timestamp,
                CreatedAt = DateTimeOffset.UtcNow,
                Ttl = null
            };

            await _interactionRepository.CreateAsync(interaction, cancellationToken);

            _logger.LogInformation(
                "Stored utterance {InteractionId} for speaker {SpeakerId}",
                interaction.Id,
                speaker.Id);

            // Emit UtteranceProcessed message for other agents (SummaryAgent, etc.)
            result.Add(new UtteranceProcessedMessage
            {
                MeetingId = transcription.MeetingId,
                UserId = transcription.UserId,
                SourceAgent = Name,
                Interaction = interaction
            });

            // Check if the utterance is a question and emit QuestionDetectedMessage
            var questionDetection = DetectQuestion(transcription.Text);
            if (questionDetection.IsQuestion)
            {
                _logger.LogInformation(
                    "Detected question with confidence {Confidence}: {Text}",
                    questionDetection.Confidence,
                    transcription.Text.Substring(0, Math.Min(50, transcription.Text.Length)));

                result.Add(new QuestionDetectedMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    MeetingId = transcription.MeetingId,
                    UserId = transcription.UserId,
                    SourceAgent = Name,
                    Priority = 1, // Highest priority for questions
                    Timestamp = DateTimeOffset.UtcNow,
                    UtteranceId = interaction.Id,
                    QuestionText = transcription.Text,
                    SpeakerId = speaker.Id,
                    SpeakerName = speaker.DisplayName ?? speaker.SpeakerLabel,
                    Confidence = questionDetection.Confidence,
                    QuestionType = questionDetection.QuestionType ?? "unknown",
                    SpeakerPriority = speaker.PriorityRank
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription");
        }

        return result;
    }

    private (bool IsQuestion, double Confidence, string? QuestionType) DetectQuestion(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (false, 0, null);
        }

        var normalizedText = text.Trim().ToLowerInvariant();
        
        foreach (var (pattern, confidence) in QuestionPatterns)
        {
            if (Regex.IsMatch(normalizedText, pattern, RegexOptions.IgnoreCase))
            {
                var questionType = DetermineQuestionType(normalizedText);
                return (true, confidence, questionType);
            }
        }

        return (false, 0, null);
    }

    private string DetermineQuestionType(string text)
    {
        if (Regex.IsMatch(text, @"^what\b")) return "Factual";
        if (Regex.IsMatch(text, @"^who\b")) return "Person";
        if (Regex.IsMatch(text, @"^where\b")) return "Location";
        if (Regex.IsMatch(text, @"^when\b")) return "Time";
        if (Regex.IsMatch(text, @"^why\b")) return "Reason";
        if (Regex.IsMatch(text, @"^how\b")) return "Procedural";
        if (Regex.IsMatch(text, @"^which\b")) return "Choice";
        if (Regex.IsMatch(text, @"\?$")) return "YesNo";
        return "General";
    }

    private async Task<Speaker> EnsureSpeakerExistsAsync(
        string meetingId, 
        string speakerId, 
        string userId,
        CancellationToken cancellationToken)
    {
        // Try to find existing speaker
        var speakers = await _speakerRepository.GetByMeetingIdAsync(meetingId, cancellationToken);
        var speaker = speakers.FirstOrDefault(s => s.SpeakerLabel == speakerId);

        if (speaker != null)
        {
            return speaker;
        }

        // Create new speaker (using object initializer)
        speaker = new Speaker
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = meetingId,
            UserId = userId,
            SpeakerLabel = speakerId,
            DisplayName = null, // Will be inferred or set by user
            Role = null,
            Company = null,
            IsSelf = false, // Will be determined later
            PriorityRank = 100, // Default low priority, will be updated if identified
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };

        await _speakerRepository.CreateAsync(speaker, cancellationToken);

        _logger.LogInformation(
            "Created new speaker {SpeakerId} with label {SpeakerLabel} for meeting {MeetingId}",
            speaker.Id,
            speaker.SpeakerLabel,
            meetingId);

        return speaker;
    }
}

/// <summary>
/// Data structure for transcription events
/// </summary>
public record TranscriptionEventData
{
    public required string MeetingId { get; init; }
    public required string UserId { get; init; }
    public required string SpeakerId { get; init; }
    public required string Text { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public double Confidence { get; init; } = 1.0;
}
