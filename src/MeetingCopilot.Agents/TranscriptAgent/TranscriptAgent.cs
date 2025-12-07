using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Events;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

namespace MeetingCopilot.Agents.TranscriptAgent;

/// <summary>
/// Agent responsible for processing real-time transcription and speaker diarization
/// </summary>
public class TranscriptAgent : IAgent
{
    private readonly IInteractionRepository _interactionRepository;
    private readonly ISpeakerRepository _speakerRepository;
    private readonly ILogger<TranscriptAgent> _logger;

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

    public bool CanHandle(MeetingCopilot.Contracts.Messages.AgentMessage message)
    {
        // Handle transcription events
        return message is MeetingCopilot.Contracts.Messages.UtteranceTranscribedMessage;
    }

    public async Task<List<MeetingCopilot.Contracts.Messages.AgentMessage>> ProcessAsync(
        MeetingCopilot.Contracts.Messages.AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        var result = new List<MeetingCopilot.Contracts.Messages.AgentMessage>();
        
        if (message is not MeetingCopilot.Contracts.Messages.UtteranceTranscribedMessage transcription)
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

            // Emit UtteranceProcessed message for other agents
            result.Add(new MeetingCopilot.Contracts.Messages.UtteranceProcessedMessage
            {
                MeetingId = transcription.MeetingId,
                UserId = transcription.UserId,
                SourceAgent = Name,
                Interaction = interaction
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transcription");
        }

        return result;
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
