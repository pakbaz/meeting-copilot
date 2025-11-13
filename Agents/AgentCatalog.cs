using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace meeting_copilot.Agents;

public sealed class AgentCatalog
{
    public AgentCatalog(IConfiguration configuration)
    {
        var apiKey = configuration["AzureAI:APIKey"]
            ?? throw new InvalidOperationException("AzureAI:APIKey configuration value is required.");
        var openAiEndpoint = configuration["AzureAI:OpenAIEndpoint"]
            ?? throw new InvalidOperationException("AzureAI:OpenAIEndpoint configuration value is required.");
        var model = configuration["AzureAI:Model"]
            ?? throw new InvalidOperationException("AzureAI:Model configuration value is required.");

        var credential = new AzureKeyCredential(apiKey);
        var client = new AzureOpenAIClient(new Uri(openAiEndpoint), credential);
        var chatClient = client.GetChatClient(model);
        ChatClient = chatClient.AsIChatClient();

        KeypointAgent = ChatClient.CreateAIAgent(
            name: "keypoint-agent",
            instructions: "You analyze live meeting transcripts to extract concise key points and actionable items. Produce compact JSON with keypoints, todos, suggestedBy, needsFollowUp flags, and guestId associations."
        );

        SpeakerAgent = ChatClient.CreateAIAgent(
            name: "speaker-identification-agent",
            instructions: "You infer the likely speaker identity, real name, and role given conversation context and optional hints. Respond with JSON containing guestId, guestName, and jobTitle only when confident."
        );
    }

    public IChatClient ChatClient { get; }

    public AIAgent KeypointAgent { get; }

    public AIAgent SpeakerAgent { get; }
}
