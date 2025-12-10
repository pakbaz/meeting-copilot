using Azure.AI.OpenAI;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using MeetingCopilot.Contracts.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace MeetingCopilot.Agents.AnswerAgent;

/// <summary>
/// Agent responsible for answering detected questions using RAG retrieval.
/// Priority: Highest (1) - Questions get immediate attention.
/// </summary>
public class AnswerAgent : IAgent
{
    private readonly IInteractionRepository _interactionRepository;
    private readonly IMemoryProvider _memoryProvider;
    private readonly ChatClient _chatClient;
    private readonly ILogger<AnswerAgent> _logger;

    public string Name => "AnswerAgent";
    public int Priority => 1; // Highest priority

    public AnswerAgent(
        IInteractionRepository interactionRepository,
        IMemoryProvider memoryProvider,
        AzureOpenAIClient azureClient,
        IConfiguration configuration,
        ILogger<AnswerAgent> logger)
    {
        _interactionRepository = interactionRepository;
        _memoryProvider = memoryProvider;
        _logger = logger;

        var modelName = configuration["AzureAI:Model"] ?? "gpt-4o-mini";
        _chatClient = azureClient.GetChatClient(modelName);
    }

    public bool CanHandle(AgentMessage message)
    {
        return message is QuestionDetectedMessage;
    }

    public async Task<List<AgentMessage>> ProcessAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        var result = new List<AgentMessage>();

        if (message is not QuestionDetectedMessage questionMessage)
        {
            _logger.LogWarning("Invalid message type: {Type}", message.GetType().Name);
            return result;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Processing question from {Speaker}: {Question}",
                questionMessage.SpeakerId,
                questionMessage.QuestionText.Substring(0, Math.Min(50, questionMessage.QuestionText.Length)));

            // Retrieve relevant context using RAG
            var context = await RetrieveContextAsync(
                questionMessage.MeetingId,
                questionMessage.QuestionText,
                questionMessage.UserId,
                cancellationToken);

            // Generate answer using LLM
            var answer = await GenerateAnswerAsync(
                questionMessage.QuestionText,
                context,
                cancellationToken);

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Store the Q&A interaction
            var interaction = new Interaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = questionMessage.UserId,
                MeetingId = questionMessage.MeetingId,
                Type = InteractionType.Answer,
                Content = answer.Text,
                ContentVector = null, // Will be populated by embedding service
                SpeakerId = "system",
                SpeakerName = "Meeting Copilot",
                QuestionId = questionMessage.UtteranceId,
                Confidence = answer.Confidence,
                Sources = answer.Sources,
                AgentName = Name,
                ProcessingTimeMs = processingTime,
                Timestamp = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                Ttl = null
            };

            await _interactionRepository.CreateAsync(interaction, cancellationToken);

            _logger.LogInformation(
                "Generated answer for question {QuestionId} in {Time}ms",
                questionMessage.UtteranceId,
                processingTime);

            // Emit answer ready message
            result.Add(new AnswerReadyMessage
            {
                Id = Guid.NewGuid().ToString(),
                MeetingId = questionMessage.MeetingId,
                UserId = questionMessage.UserId,
                SourceAgent = Name,
                Priority = 1,
                Timestamp = DateTimeOffset.UtcNow,
                QuestionMessageId = questionMessage.Id,
                QuestionText = questionMessage.QuestionText,
                AnswerText = answer.Text,
                Confidence = answer.Confidence,
                Sources = answer.Sources ?? new(),
                QuestionerSpeakerId = questionMessage.SpeakerId,
                ProcessingTimeMs = processingTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question {QuestionId}", questionMessage.Id);
        }

        return result;
    }

    private async Task<List<string>> RetrieveContextAsync(
        string meetingId,
        string question,
        string userId,
        CancellationToken cancellationToken)
    {
        var contextItems = new List<string>();

        try
        {
            // Search for relevant meeting content using vector search
            var searchResults = await _memoryProvider.SearchSimilarAsync<Interaction>(
                userId,
                question,
                limit: 5,
                similarityThreshold: 0.6,
                cancellationToken);

            contextItems.AddRange(searchResults.Select(r => r.Content));

            // Also get recent transcript for context
            var recentInteractions = await _interactionRepository.GetByMeetingIdAsync(
                userId,
                meetingId,
                cancellationToken);

            var recentTranscript = recentInteractions
                .Where(i => i.Type == InteractionType.Utterance)
                .OrderByDescending(i => i.Timestamp)
                .Take(10)
                .Select(i => $"[{i.SpeakerName}]: {i.Content}");

            contextItems.AddRange(recentTranscript);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving context for question");
        }

        return contextItems;
    }

    private async Task<(string Text, double Confidence, List<string>? Sources)> GenerateAnswerAsync(
        string question,
        List<string> context,
        CancellationToken cancellationToken)
    {
        var systemPrompt = @"You are a helpful meeting assistant. Answer the question based on the meeting context provided.
Be concise and direct. If you don't have enough information to answer, say so briefly.
If the question is about something discussed in the meeting, reference what was said.

Guidelines:
- Keep answers under 3 sentences unless more detail is needed
- Reference specific speakers when relevant
- If uncertain, indicate the confidence level
- Don't make up information not in the context";

        var contextText = context.Count > 0
            ? $"Meeting Context:\n{string.Join("\n", context)}"
            : "No specific context available.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage($"{contextText}\n\nQuestion: {question}")
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.3f, // Lower temperature for factual answers
            MaxOutputTokenCount = 500
        };

        var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var answerText = response.Value.Content[0].Text;

        // Estimate confidence based on context availability
        var confidence = context.Count >= 3 ? 0.9 : context.Count >= 1 ? 0.7 : 0.5;

        return (answerText, confidence, context.Count > 0 ? new List<string> { "Meeting transcript" } : null);
    }
}
