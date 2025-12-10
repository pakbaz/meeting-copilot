using Azure.AI.OpenAI;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using MeetingCopilot.Contracts.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace MeetingCopilot.Agents.ResearchAgent;

/// <summary>
/// Agent responsible for conducting background research on topics.
/// Priority: Low (8) - Background research runs when other agents are idle.
/// </summary>
public class ResearchAgent : IAgent
{
    private readonly IInsightRepository _insightRepository;
    private readonly IMemoryProvider _memoryProvider;
    private readonly ChatClient _chatClient;
    private readonly ILogger<ResearchAgent> _logger;

    public string Name => "ResearchAgent";
    public int Priority => 8; // Lower priority - background task

    public ResearchAgent(
        IInsightRepository insightRepository,
        IMemoryProvider memoryProvider,
        AzureOpenAIClient azureClient,
        IConfiguration configuration,
        ILogger<ResearchAgent> logger)
    {
        _insightRepository = insightRepository;
        _memoryProvider = memoryProvider;
        _logger = logger;

        var modelName = configuration["AzureAI:Model"] ?? "gpt-4o-mini";
        _chatClient = azureClient.GetChatClient(modelName);
    }

    public bool CanHandle(AgentMessage message)
    {
        return message is ResearchRequestedMessage;
    }

    public async Task<List<AgentMessage>> ProcessAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        var result = new List<AgentMessage>();

        if (message is not ResearchRequestedMessage researchMessage)
        {
            _logger.LogWarning("Invalid message type: {Type}", message.GetType().Name);
            return result;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Processing research request: {Query} for meeting {MeetingId}",
                researchMessage.Query,
                researchMessage.MeetingId);

            // Search for existing knowledge in memory
            var existingKnowledge = await RetrieveExistingKnowledgeAsync(
                researchMessage.MeetingId,
                researchMessage.UserId,
                researchMessage.Query,
                cancellationToken);

            // Generate research results using LLM
            var researchResults = await GenerateResearchAsync(
                researchMessage.Query,
                existingKnowledge,
                researchMessage.MaxResults,
                cancellationToken);

            // Store research results as insight
            var insight = new Insight
            {
                Id = Guid.NewGuid().ToString(),
                UserId = researchMessage.UserId,
                MeetingId = researchMessage.MeetingId,
                Type = InsightType.ResearchResult,
                Title = $"Research: {TruncateText(researchMessage.Query, 50)}",
                Content = researchResults.Summary ?? string.Join("\n\n", researchResults.Results.Select(r => $"**{r.Title}**\n{r.Snippet}")),
                PriorityScore = 50, // Medium priority
                SourceUtteranceId = researchMessage.SourceUtteranceId
            };

            await _insightRepository.CreateAsync(insight, cancellationToken);

            var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Emit research ready message
            var responseMessage = new ResearchReadyMessage
            {
                MeetingId = researchMessage.MeetingId,
                UserId = researchMessage.UserId,
                SourceAgent = Name,
                Priority = Priority,
                CorrelationId = researchMessage.CorrelationId,
                RequestMessageId = researchMessage.Id,
                Query = researchMessage.Query,
                Results = researchResults.Results,
                Summary = researchResults.Summary,
                ProcessingTimeMs = processingTime,
                Success = true
            };

            result.Add(responseMessage);

            _logger.LogInformation(
                "Research completed for query '{Query}' with {ResultCount} results in {Duration}ms",
                researchMessage.Query,
                researchResults.Results.Count,
                processingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing research request: {Query}", researchMessage.Query);

            var errorResponse = new ResearchReadyMessage
            {
                MeetingId = researchMessage.MeetingId,
                UserId = researchMessage.UserId,
                SourceAgent = Name,
                Priority = Priority,
                CorrelationId = researchMessage.CorrelationId,
                RequestMessageId = researchMessage.Id,
                Query = researchMessage.Query,
                Success = false,
                ErrorMessage = ex.Message
            };

            result.Add(errorResponse);
        }

        return result;
    }

    private async Task<List<string>> RetrieveExistingKnowledgeAsync(
        string meetingId,
        string userId,
        string query,
        CancellationToken cancellationToken)
    {
        var contextDocs = new List<string>();

        try
        {
            // Search for related interactions in memory
            var searchResults = await _memoryProvider.SearchSimilarAsync<Interaction>(
                userId,
                query,
                limit: 5,
                cancellationToken: cancellationToken);

            foreach (var result in searchResults)
            {
                contextDocs.Add($"[{result.SpeakerName ?? "Unknown"}] {result.Content}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving existing knowledge");
        }

        return contextDocs;
    }

    private async Task<ResearchResponse> GenerateResearchAsync(
        string query,
        List<string> existingKnowledge,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var systemPrompt = """
            You are a research assistant helping during a meeting. Your task is to provide relevant, 
            accurate information on the requested topic.

            Guidelines:
            - Provide concise, factual information
            - Structure your response as distinct research findings
            - Include key facts, definitions, and relevant context
            - Be objective and balanced
            - Acknowledge if information might be outdated or uncertain
            - Keep each finding brief and actionable

            Format your response as JSON with this structure:
            {
                "summary": "A brief 1-2 sentence summary of findings",
                "results": [
                    {
                        "title": "Finding title",
                        "snippet": "Key information about this finding",
                        "relevance": 0.9,
                        "sourceType": "knowledge"
                    }
                ]
            }
            """;

        var contextSection = existingKnowledge.Count > 0
            ? $"\n\nRelevant context from the meeting:\n{string.Join("\n", existingKnowledge)}"
            : "";

        var userPrompt = $"""
            Research the following topic and provide {maxResults} relevant findings:

            Topic: {query}
            {contextSection}
            """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.3f,
            MaxOutputTokenCount = 1500
        };

        var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var responseText = completion.Value.Content[0].Text;

        // Parse the JSON response
        try
        {
            // Clean the response (remove markdown code blocks if present)
            responseText = CleanJsonResponse(responseText);
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<ResearchJsonResponse>(responseText, jsonOptions);

            if (response != null)
            {
                return new ResearchResponse
                {
                    Summary = response.Summary,
                    Results = response.Results?.Select(r => new ResearchResult
                    {
                        Title = r.Title ?? "Research Finding",
                        Snippet = r.Snippet ?? "",
                        Relevance = r.Relevance,
                        SourceType = r.SourceType ?? "knowledge"
                    }).ToList() ?? new List<ResearchResult>()
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse research response as JSON, using fallback");
        }

        // Fallback: treat the entire response as a single result
        return new ResearchResponse
        {
            Summary = TruncateText(responseText, 200),
            Results = new List<ResearchResult>
            {
                new ResearchResult
                {
                    Title = $"Research: {TruncateText(query, 50)}",
                    Snippet = responseText,
                    Relevance = 0.8,
                    SourceType = "knowledge"
                }
            }
        };
    }

    private static string CleanJsonResponse(string response)
    {
        // Remove markdown code blocks
        if (response.StartsWith("```json"))
        {
            response = response[7..];
        }
        else if (response.StartsWith("```"))
        {
            response = response[3..];
        }

        if (response.EndsWith("```"))
        {
            response = response[..^3];
        }

        return response.Trim();
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text[..(maxLength - 3)] + "...";
    }

    private class ResearchResponse
    {
        public string? Summary { get; set; }
        public List<ResearchResult> Results { get; set; } = new();
    }

    private class ResearchJsonResponse
    {
        public string? Summary { get; set; }
        public List<ResearchResultJson>? Results { get; set; }
    }

    private class ResearchResultJson
    {
        public string? Title { get; set; }
        public string? Snippet { get; set; }
        public double Relevance { get; set; }
        public string? SourceType { get; set; }
    }
}
