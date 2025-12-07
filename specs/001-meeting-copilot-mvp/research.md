# Research: Real-time Meeting AI Assistant

**Feature**: 001-meeting-copilot-mvp  
**Date**: 2025-12-06  
**Status**: Complete

---

## 1. Microsoft Agent Framework

### Decision

Use `Microsoft.Agents.AI` and related packages (preview) for multi-agent orchestration.

### Rationale

- Official Microsoft framework for building AI agents in .NET
- Supports durable workflows, multi-agent orchestration, and Azure AI integration
- Aligns with Constitution IV mandate for Microsoft Agent Framework
- Preview packages provide latest patterns for agent development

### Alternatives Considered

| Option | Rejected Because |
|--------|------------------|
| Semantic Kernel alone | Lacks orchestration primitives, Agent Framework builds on top |
| AutoGen .NET | Less mature, Microsoft recommending Agent Framework for production |
| Custom orchestration | Unnecessary complexity; framework handles common patterns |

### Implementation Patterns

**Package Installation**:

```bash
dotnet add package Microsoft.Agents.AI.AzureAI --prerelease
dotnet add package Microsoft.Agents.AI.Workflows --prerelease
dotnet add package Microsoft.Extensions.AI --prerelease
```

**Multi-Agent Workflow**:

```csharp
// Define agents
var transcriptAgent = new TranscriptAgent(cosmos, speech);
var answerAgent = new AnswerAgent(cosmos, llm);
var researchAgent = new ResearchAgent(cosmos, webSearch);
var summaryAgent = new SummaryAgent(cosmos, llm);

// Build workflow with edges
WorkflowBuilder builder = new(transcriptAgent);
builder.AddEdge(transcriptAgent, answerAgent);
builder.AddEdge(transcriptAgent, summaryAgent);
builder.AddEdge(transcriptAgent, researchAgent);

Workflow workflow = await builder.BuildAsync();
```

**Durable Agent (Azure Functions)**:

```csharp
[Function("agent_orchestration_workflow")]
public static async Task<Dictionary<string, string>> AgentOrchestrationWorkflow(
    [OrchestrationTrigger] TaskOrchestrationContext context)
{
    DurableAIAgent mainAgent = context.GetAgent("TranscriptAgent");
    AgentRunResponse<TextResponse> response = await mainAgent.RunAsync<TextResponse>(input);
    
    // Fan-out to parallel agents
    var tasks = new List<Task<AgentRunResponse<TextResponse>>>
    {
        context.GetAgent("AnswerAgent").RunAsync<TextResponse>(response.Content),
        context.GetAgent("SummaryAgent").RunAsync<TextResponse>(response.Content)
    };
    
    await Task.WhenAll(tasks);
}
```

### Key Sources

- Microsoft Learn: Multi-agent orchestration patterns
- NuGet: Microsoft.Agents.AI.* packages (v1.0.0-preview*)

---

## 2. Cosmos DB Memory Provider

### Decision

Implement `CosmosMemoryProvider` following james-tn/agent-memory patterns for agent state and RAG.

### Rationale

- Vector search capability for semantic retrieval (FR-054)
- Partition key design supports multi-tenant isolation (FR-060)
- Proven patterns from Microsoft reference implementation
- Cosmos DB SDK supports .NET 10 and async patterns

### Alternatives Considered

| Option | Rejected Because |
|--------|------------------|
| SQLite + pgvector | Constitution III mandates Azure-native services |
| Azure AI Search | Additional service complexity; Cosmos handles both data + vectors |
| In-memory only | No persistence across sessions; can't support 90-day retention |

### Implementation Patterns

**Container Schema** (from james-tn/agent-memory):

```json
{
  "containers": [
    {
      "id": "meetings",
      "partitionKey": "/meeting_id",
      "indexingPolicy": { "automatic": true }
    },
    {
      "id": "interactions",
      "partitionKey": "/user_id",
      "vectorEmbeddingPolicy": {
        "vectorEmbeddings": [{
          "path": "/content_vector",
          "dataType": "float32",
          "distanceFunction": "cosine",
          "dimensions": 1536
        }]
      }
    },
    {
      "id": "insights",
      "partitionKey": "/user_id",
      "vectorEmbeddingPolicy": {
        "vectorEmbeddings": [{
          "path": "/embedding",
          "dataType": "float32",
          "distanceFunction": "cosine",
          "dimensions": 1536
        }]
      }
    },
    {
      "id": "session_summaries",
      "partitionKey": "/user_id"
    }
  ]
}
```

**Memory Provider Pattern**:

```csharp
public class CosmosMemoryProvider : IContextProvider
{
    private readonly CosmosClient _client;
    private readonly string _databaseId;
    private readonly EmbeddingClient _embeddingClient;
    
    public async Task<ContextResult> GetContextAsync(string userId, string query)
    {
        // Generate embedding for query
        var embedding = await _embeddingClient.GenerateEmbeddingAsync(query);
        
        // Vector search in interactions container
        var container = _client.GetContainer(_databaseId, "interactions");
        var queryDef = new QueryDefinition(
            "SELECT TOP @k c.id, c.content, c.timestamp, VectorDistance(c.content_vector, @embedding) AS score " +
            "FROM c WHERE c.user_id = @userId ORDER BY VectorDistance(c.content_vector, @embedding)")
            .WithParameter("@k", 10)
            .WithParameter("@userId", userId)
            .WithParameter("@embedding", embedding.Vector.ToArray());
        
        // Return ranked results as context
    }
    
    public async Task StoreInteractionAsync(Interaction interaction)
    {
        var embedding = await _embeddingClient.GenerateEmbeddingAsync(interaction.Content);
        interaction.ContentVector = embedding.Vector.ToArray();
        
        var container = _client.GetContainer(_databaseId, "interactions");
        await container.CreateItemAsync(interaction, new PartitionKey(interaction.UserId));
    }
}
```

**Agent Integration**:

```csharp
// Inject memory provider as context provider
var memoryProvider = new CosmosMemoryProvider(cosmosClient, embeddingClient, config);

var agent = new ChatAgent(
    chatClient: openAIClient.GetChatClient("gpt-4o"),
    instructions: "You are a meeting assistant...",
    contextProviders: [memoryProvider]  // Automatic RAG injection
);
```

### Key Sources

- GitHub: james-tn/agent-memory (CosmosMemoryProvider, CosmosAgentMemory)
- Microsoft Learn: Cosmos DB vector search documentation

---

## 3. .NET Aspire Orchestration

### Decision

Use .NET Aspire for local development orchestration and Azure Container Apps deployment.

### Rationale

- Constitution V mandates Aspire for local orchestration
- Single topology definition for local dev and cloud deployment
- Built-in service discovery, health checks, and observability
- `aspire deploy` command handles Azure Container Apps provisioning

### Alternatives Considered

| Option | Rejected Because |
|--------|------------------|
| Docker Compose only | No Azure deployment path; manual service discovery |
| Kubernetes locally | Excessive complexity for MVP; Aspire abstracts this |
| Single monolith | Can't independently scale agents; poor separation |

### Implementation Patterns

**AppHost Topology**:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Azure resources
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .AddDatabase("meetingcopilot");

var speech = builder.AddConnectionString("speech");  // Azure Speech endpoint

// Service projects
var agents = builder.AddProject<Projects.MeetingCopilot_Agents>("agents")
    .WithReference(cosmos);

var web = builder.AddProject<Projects.MeetingCopilot>("web")
    .WithExternalHttpEndpoints()
    .WithReference(agents)
    .WithReference(cosmos)
    .WithReference(speech);

builder.Build().Run();
```

**Azure Deployment**:

```bash
# Install Aspire CLI
dotnet tool install -g aspire-cli

# Deploy to Azure Container Apps
aspire deploy --project ./MeetingCopilot.AppHost/MeetingCopilot.AppHost.csproj
```

**Container Apps Environment** (explicit creation required in Aspire 9.4+):

```csharp
var containerAppEnvironment = builder.AddAzureContainerAppEnvironment("cae");

var web = builder.AddProject<Projects.MeetingCopilot>("web")
    .PublishAsAzureContainerApp((infrastructure, containerApp) =>
    {
        containerApp.Template.Scale.MinReplicas = 1;
        containerApp.Template.Scale.MaxReplicas = 3;
    });
```

### Key Sources

- Microsoft Learn: .NET Aspire deployment to Azure Container Apps
- Microsoft Learn: Aspire 9.4 breaking changes (hybrid mode removal)

---

## 4. Azure Speech Services Integration

### Decision

Use Azure Cognitive Services Speech SDK for real-time transcription with speaker diarization.

### Rationale

- Already implemented in existing codebase (`SpeechRecognitionService.cs`)
- Provides real-time streaming transcription
- Speaker diarization identifies different speakers
- Configuration already added to project

### Implementation Notes

**Existing Service** (already in codebase):

- `Services/SpeechRecognitionService.cs` - Speech SDK integration
- SignalR connection for real-time UI updates
- Microphone permission handling via JS interop

**Enhancement for MVP**:

- Add local audio buffer for connection recovery (FR-062)
- Implement retry logic with exponential backoff
- Add speaker name inference from transcript content

### Key Sources

- Existing codebase: `Services/SpeechRecognitionService.cs`
- Azure Speech SDK documentation

---

## 5. Microsoft Foundry AI (Azure AI Foundry)

### Decision

Use Microsoft Foundry for LLM inference and embeddings.

### Rationale

- Configuration already added to project
- Provides managed access to GPT-4o and embedding models
- Supports web search capability for Research Agent
- Enterprise-grade security with managed identity

### Implementation Notes

**Configuration** (user confirmed already configured):

- Connection string in `appsettings.json` or Key Vault
- DefaultAzureCredential for authentication
- Model endpoints for chat completion and embeddings

**Agent Integration**:

```csharp
// Chat completion for Answer/Summary agents
var chatClient = new AzureOpenAIClient(
    new Uri(config["Foundry:Endpoint"]),
    new DefaultAzureCredential()
).GetChatClient("gpt-4o");

// Embeddings for vector search
var embeddingClient = new AzureOpenAIClient(
    new Uri(config["Foundry:Endpoint"]),
    new DefaultAzureCredential()
).GetEmbeddingClient("text-embedding-3-small");
```

### Key Sources

- Microsoft Learn: Azure AI Foundry documentation
- Existing project configuration

---

## Research Summary

| Area | Decision | Confidence |
|------|----------|------------|
| Agent Framework | Microsoft.Agents.AI (preview) | High - Official MS recommendation |
| Memory Store | Cosmos DB with vector search | High - Proven patterns from james-tn |
| Orchestration | .NET Aspire | High - Constitution mandate |
| Speech | Azure Speech SDK (existing) | High - Already implemented |
| LLM | Microsoft Foundry AI (existing) | High - Already configured |

**All NEEDS CLARIFICATION items resolved.** Proceed to Phase 1 design.
