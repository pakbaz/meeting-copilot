using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Contracts.Services;

/// <summary>
/// Utility service for parsing meeting context text to extract meeting details.
/// </summary>
public static class MeetingContextParser
{
    /// <summary>
    /// Parses context text to extract a title.
    /// Takes the first non-empty line of the context as the title.
    /// </summary>
    public static string ParseTitle(string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
            return string.Empty;

        var lines = context.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && trimmed.Length <= 100)
            {
                // Remove common title prefixes
                if (trimmed.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                    return trimmed.Substring(6).Trim();
                if (trimmed.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
                    return trimmed.Substring(8).Trim();
                if (trimmed.StartsWith("Meeting:", StringComparison.OrdinalIgnoreCase))
                    return trimmed.Substring(8).Trim();
                return trimmed;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Parses context text to extract agenda items.
    /// Looks for numbered lists (1., 2.) or bullet points (-, *).
    /// </summary>
    public static List<AgendaItem> ParseAgendaItems(string? context)
    {
        var items = new List<AgendaItem>();
        if (string.IsNullOrWhiteSpace(context))
            return items;

        var lines = context.Split('\n');
        int order = 0;
        bool inAgendaSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Check for agenda section header
            if (trimmed.StartsWith("Agenda:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Topics:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Discussion Items:", StringComparison.OrdinalIgnoreCase))
            {
                inAgendaSection = true;
                continue;
            }

            // Check for numbered list items (1. Item, 2. Item, etc.)
            var numberedMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(\d+)\.\s*(.+)$");
            if (numberedMatch.Success)
            {
                order++;
                items.Add(new AgendaItem
                {
                    Title = numberedMatch.Groups[2].Value.Trim(),
                    Order = order,
                    Status = AgendaItemStatus.Pending
                });
                inAgendaSection = true;
                continue;
            }

            // Check for bullet points (- Item or * Item)
            if ((trimmed.StartsWith("- ") || trimmed.StartsWith("* ")) && inAgendaSection)
            {
                order++;
                items.Add(new AgendaItem
                {
                    Title = trimmed.Substring(2).Trim(),
                    Order = order,
                    Status = AgendaItemStatus.Pending
                });
            }

            // End agenda section on empty line after items have been collected
            if (string.IsNullOrEmpty(trimmed) && items.Count > 0 && !inAgendaSection)
            {
                break;
            }
        }

        return items;
    }

    /// <summary>
    /// Parses context text to extract participant names.
    /// Looks for keywords like "Attendees:", "Participants:", "People:", "Invitees:".
    /// </summary>
    public static List<Participant> ParseParticipants(string? context)
    {
        var participants = new List<Participant>();
        if (string.IsNullOrWhiteSpace(context))
            return participants;

        var lines = context.Split('\n');
        bool inParticipantSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Check for participant section header
            if (trimmed.StartsWith("Attendees:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Participants:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("People:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Invitees:", StringComparison.OrdinalIgnoreCase))
            {
                inParticipantSection = true;
                
                // Check if names are on the same line (comma-separated)
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0 && colonIndex < trimmed.Length - 1)
                {
                    var namesOnLine = trimmed.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(namesOnLine))
                    {
                        var names = namesOnLine.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var name in names)
                        {
                            var cleanName = name.Trim();
                            if (!string.IsNullOrEmpty(cleanName))
                            {
                                participants.Add(new Participant
                                {
                                    DisplayName = cleanName,
                                    Role = participants.Count == 0 ? ParticipantRole.Organizer : ParticipantRole.Attendee
                                });
                            }
                        }
                    }
                }
                continue;
            }

            // Parse list items within participant section
            if (inParticipantSection)
            {
                string? name = null;
                
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    name = trimmed.Substring(2).Trim();
                }
                else if (!string.IsNullOrEmpty(trimmed) && !trimmed.Contains(':'))
                {
                    name = trimmed;
                }

                if (!string.IsNullOrEmpty(name))
                {
                    participants.Add(new Participant
                    {
                        DisplayName = name,
                        Role = participants.Count == 0 ? ParticipantRole.Organizer : ParticipantRole.Attendee
                    });
                }
                
                // Stop at empty line
                if (string.IsNullOrEmpty(trimmed))
                {
                    inParticipantSection = false;
                }
            }
        }

        return participants;
    }

    /// <summary>
    /// Parses complete meeting details from context text.
    /// </summary>
    public static (string Title, List<AgendaItem> AgendaItems, List<Participant> Participants) ParseMeetingDetails(string? context)
    {
        return (
            ParseTitle(context),
            ParseAgendaItems(context),
            ParseParticipants(context)
        );
    }
}
