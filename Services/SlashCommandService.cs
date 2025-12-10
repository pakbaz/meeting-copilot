using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace meeting_copilot.Services;

/// <summary>
/// Service for parsing and executing slash commands in the meeting input area.
/// </summary>
public class SlashCommandService
{
    private readonly ILogger<SlashCommandService> _logger;
    private readonly ISpeakerRepository _speakerRepository;

    public SlashCommandService(
        ISpeakerRepository speakerRepository,
        ILogger<SlashCommandService> logger)
    {
        _speakerRepository = speakerRepository;
        _logger = logger;
    }

    public record SlashCommandResult(
        bool Success,
        string? Message,
        SlashCommandType Type,
        object? Data = null
    );

    public enum SlashCommandType
    {
        Unknown,
        Speaker,
        Research,
        Suggest,
        Help
    }

    /// <summary>
    /// Parses and executes a slash command from user input.
    /// </summary>
    public async Task<SlashCommandResult> ExecuteAsync(
        string meetingId,
        string userId,
        string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
        {
            return new SlashCommandResult(false, "Not a slash command", SlashCommandType.Unknown);
        }

        var parts = input.TrimStart('/').Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1] : string.Empty;

        _logger.LogInformation("Processing slash command: /{Command} {Argument}", command, argument);

        return command switch
        {
            "speaker" => await HandleSpeakerCommandAsync(meetingId, userId, argument, cancellationToken),
            "research" => HandleResearchCommand(meetingId, argument),
            "suggest" => HandleSuggestCommand(meetingId),
            "help" => HandleHelpCommand(),
            _ => new SlashCommandResult(false, $"Unknown command: /{command}", SlashCommandType.Unknown)
        };
    }

    /// <summary>
    /// Handles /speaker {label} {name} [{role}] command to assign name/role to a speaker.
    /// </summary>
    private async Task<SlashCommandResult> HandleSpeakerCommandAsync(
        string meetingId,
        string userId,
        string argument,
        CancellationToken cancellationToken)
    {
        // Parse: /speaker Guest1 John Smith [Product Manager]
        var match = Regex.Match(argument, @"^(\S+)\s+(.+?)(?:\s+\[(.+?)\])?$");
        if (!match.Success)
        {
            return new SlashCommandResult(
                false,
                "Usage: /speaker {label} {name} [role]\nExample: /speaker Guest1 John Smith [Product Manager]",
                SlashCommandType.Speaker);
        }

        var speakerLabel = match.Groups[1].Value;
        var displayName = match.Groups[2].Value.Trim();
        var role = match.Groups[3].Success ? match.Groups[3].Value : null;

        try
        {
            // Find speaker by label in this meeting
            var speakers = await _speakerRepository.GetByMeetingIdAsync(meetingId, cancellationToken);
            var speaker = speakers.FirstOrDefault(s => 
                s.SpeakerLabel.Equals(speakerLabel, StringComparison.OrdinalIgnoreCase));

            if (speaker == null)
            {
                // Create new speaker
                speaker = new Speaker
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    MeetingId = meetingId,
                    SpeakerLabel = speakerLabel,
                    DisplayName = displayName,
                    Role = role,
                    IsSelf = false,
                    FirstSeenAt = DateTimeOffset.UtcNow
                };
                await _speakerRepository.CreateAsync(speaker, cancellationToken);
                _logger.LogInformation("Created speaker {Label} as {Name}", speakerLabel, displayName);
            }
            else
            {
                // Update existing speaker
                speaker = speaker with
                {
                    DisplayName = displayName,
                    Role = role ?? speaker.Role
                };
                await _speakerRepository.UpdateAsync(speaker, cancellationToken);
                _logger.LogInformation("Updated speaker {Label} to {Name}", speakerLabel, displayName);
            }

            return new SlashCommandResult(
                true,
                $"Speaker {speakerLabel} set to {displayName}" + (role != null ? $" ({role})" : ""),
                SlashCommandType.Speaker,
                speaker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating speaker {Label}", speakerLabel);
            return new SlashCommandResult(false, $"Error updating speaker: {ex.Message}", SlashCommandType.Speaker);
        }
    }

    /// <summary>
    /// Handles /research {topic} command to trigger background research.
    /// </summary>
    private SlashCommandResult HandleResearchCommand(string meetingId, string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return new SlashCommandResult(
                false,
                "Usage: /research {topic}\nExample: /research Azure Cosmos DB pricing",
                SlashCommandType.Research);
        }

        _logger.LogInformation("Research requested for topic: {Topic}", topic);
        
        // Return result that will be handled by the orchestrator/ResearchAgent
        return new SlashCommandResult(
            true,
            $"Researching: {topic}",
            SlashCommandType.Research,
            new { Topic = topic, MeetingId = meetingId });
    }

    /// <summary>
    /// Handles /suggest command to get next topic suggestion.
    /// </summary>
    private SlashCommandResult HandleSuggestCommand(string meetingId)
    {
        _logger.LogInformation("Topic suggestion requested for meeting {MeetingId}", meetingId);
        
        return new SlashCommandResult(
            true,
            "Analyzing agenda for next topic...",
            SlashCommandType.Suggest,
            new { MeetingId = meetingId });
    }

    /// <summary>
    /// Handles /help command to show available commands.
    /// </summary>
    private SlashCommandResult HandleHelpCommand()
    {
        var helpText = @"**Available Commands:**
- `/speaker {label} {name} [role]` - Assign name to a speaker
  Example: `/speaker Guest1 John Smith [Product Manager]`
- `/research {topic}` - Search for information on a topic
  Example: `/research Azure Cosmos DB pricing`
- `/suggest` - Get suggestion for next agenda topic
- `/help` - Show this help message";

        return new SlashCommandResult(true, helpText, SlashCommandType.Help);
    }

    /// <summary>
    /// Checks if the input is a slash command.
    /// </summary>
    public bool IsSlashCommand(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.TrimStart().StartsWith("/");
    }
}
