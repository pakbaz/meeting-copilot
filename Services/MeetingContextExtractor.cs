using Azure.AI.OpenAI;
using MeetingCopilot.Contracts.Entities;
using OpenAI.Chat;
using System.Text.Json;

namespace meeting_copilot.Services;

/// <summary>
/// Service to extract meeting details (title, agenda, participants) from free-form context text using LLM.
/// </summary>
public class MeetingContextExtractor
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<MeetingContextExtractor> _logger;

    public MeetingContextExtractor(AzureOpenAIClient azureClient, IConfiguration configuration, ILogger<MeetingContextExtractor> logger)
    {
        var modelName = configuration["AzureAI:Model"] ?? "gpt-4o-mini";
        _chatClient = azureClient.GetChatClient(modelName);
        _logger = logger;
    }

    public record ExtractedMeetingDetails(
        string Title,
        string? Description,
        List<string> AgendaItems,
        List<ExtractedParticipant> Participants
    );

    public record ExtractedParticipant(
        string Name,
        string? Role,
        string? Organization
    );

    public async Task<ExtractedMeetingDetails> ExtractMeetingDetailsAsync(string context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return new ExtractedMeetingDetails("Untitled Meeting", null, new List<string>(), new List<ExtractedParticipant>());
        }

        try
        {
            var systemPrompt = @"You are a meeting assistant that extracts structured information from meeting context text.
Extract the following from the provided text:
1. Meeting Title - The main topic or name of the meeting (look for headers, the first line, or the most prominent topic)
2. Description - A brief summary of what the meeting is about (optional)
3. Agenda Items - List of topics to be discussed (look for numbered lists, bullet points, or items under 'Agenda' heading)
4. Participants - List of people attending (look for names under 'Participants', 'Attendees', or mentioned in the text)

Respond ONLY with valid JSON in this exact format:
{
  ""title"": ""string"",
  ""description"": ""string or null"",
  ""agendaItems"": [""string"", ""string""],
  ""participants"": [
    {""name"": ""string"", ""role"": ""string or null"", ""organization"": ""string or null""}
  ]
}

Important:
- The title should be concise but descriptive
- For markdown headers like '# Title', extract just the title text without the #
- If no clear title is found, derive one from the main topic
- Extract all agenda items as simple strings
- For participants, extract name, role (e.g., 'Product Manager'), and organization if mentioned";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Extract meeting details from this text:\n\n{context}")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f, // Low temperature for more deterministic extraction
                MaxOutputTokenCount = 1000
            };

            _logger.LogDebug("Extracting meeting details using LLM...");
            var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            
            var responseText = response.Value.Content[0].Text;
            _logger.LogDebug("LLM response: {Response}", responseText);

            // Parse the JSON response
            var extracted = ParseLlmResponse(responseText);
            
            _logger.LogInformation("Extracted meeting: Title='{Title}', AgendaItems={AgendaCount}, Participants={ParticipantCount}",
                extracted.Title, extracted.AgendaItems.Count, extracted.Participants.Count);

            return extracted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting meeting details with LLM, falling back to simple parsing");
            return FallbackParsing(context);
        }
    }

    private ExtractedMeetingDetails ParseLlmResponse(string responseText)
    {
        try
        {
            // Clean up the response - remove markdown code blocks if present
            var jsonText = responseText.Trim();
            if (jsonText.StartsWith("```json"))
            {
                jsonText = jsonText.Substring(7);
            }
            else if (jsonText.StartsWith("```"))
            {
                jsonText = jsonText.Substring(3);
            }
            if (jsonText.EndsWith("```"))
            {
                jsonText = jsonText.Substring(0, jsonText.Length - 3);
            }
            jsonText = jsonText.Trim();

            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var title = root.GetProperty("title").GetString() ?? "Untitled Meeting";
            var description = root.TryGetProperty("description", out var descProp) && descProp.ValueKind != JsonValueKind.Null
                ? descProp.GetString()
                : null;

            var agendaItems = new List<string>();
            if (root.TryGetProperty("agendaItems", out var agendaProp) && agendaProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in agendaProp.EnumerateArray())
                {
                    var itemText = item.GetString();
                    if (!string.IsNullOrWhiteSpace(itemText))
                    {
                        agendaItems.Add(itemText);
                    }
                }
            }

            var participants = new List<ExtractedParticipant>();
            if (root.TryGetProperty("participants", out var participantsProp) && participantsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in participantsProp.EnumerateArray())
                {
                    var name = p.GetProperty("name").GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        var role = p.TryGetProperty("role", out var roleProp) && roleProp.ValueKind != JsonValueKind.Null
                            ? roleProp.GetString()
                            : null;
                        var org = p.TryGetProperty("organization", out var orgProp) && orgProp.ValueKind != JsonValueKind.Null
                            ? orgProp.GetString()
                            : null;
                        participants.Add(new ExtractedParticipant(name, role, org));
                    }
                }
            }

            return new ExtractedMeetingDetails(title, description, agendaItems, participants);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM JSON response: {Response}", responseText);
            throw;
        }
    }

    private ExtractedMeetingDetails FallbackParsing(string context)
    {
        // Simple fallback parsing when LLM fails
        var lines = context.Split('\n');
        var title = "Untitled Meeting";
        var agendaItems = new List<string>();
        var inAgendaSection = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Look for markdown header as title
            if (title == "Untitled Meeting" && line.StartsWith("# ") && !line.ToLower().Contains("agenda") && !line.ToLower().Contains("participant"))
            {
                title = line.TrimStart('#').Trim();
                continue;
            }

            // First non-empty, non-header line as title
            if (title == "Untitled Meeting" && !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            {
                title = line;
                continue;
            }

            // Check for agenda section
            if (line.ToLower().Contains("agenda"))
            {
                inAgendaSection = true;
                continue;
            }

            if (inAgendaSection && (line.StartsWith("## ") || line.StartsWith("# ")))
            {
                if (!line.ToLower().Contains("agenda"))
                {
                    inAgendaSection = false;
                }
            }

            // Parse agenda items
            if (inAgendaSection && !string.IsNullOrWhiteSpace(line))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"^(?:\d+[\.\)]\s*|[-*]\s*)(.+)$");
                if (match.Success)
                {
                    agendaItems.Add(match.Groups[1].Value.Trim());
                }
            }
        }

        return new ExtractedMeetingDetails(title, null, agendaItems, new List<ExtractedParticipant>());
    }
}
