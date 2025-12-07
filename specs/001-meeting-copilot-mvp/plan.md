# Implementation Plan: Real-time Meeting AI Assistant (MVP)

**Branch**: `001-meeting-copilot-mvp` | **Date**: 2025-12-06 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-meeting-copilot-mvp/spec.md`

## Summary

Build a real-time meeting assistant web application using Blazor Interactive Server with multi-agent AI architecture for transcription, question answering, research, and action item extraction. The system uses Azure Speech Services for real-time transcription with speaker diarization (already implemented), Microsoft Agent Framework for multi-agent orchestration, Azure Cosmos DB with vector search for agent memory and RAG, Microsoft Foundry AI for LLM inference (already configured), and .NET Aspire for local development orchestration and container deployment.

## Technical Context

**Language/Version**: .NET 10.0 with C# 14 (nullable reference types, ImplicitUsings enabled)  
**Primary Dependencies**: Microsoft.Agents.AI (preview), Azure.AI.OpenAI, Microsoft.CognitiveServices.Speech, Microsoft.Azure.Cosmos, Aspire.Hosting  
**Storage**: Azure Cosmos DB NoSQL with vector search (containers: meetings, interactions, insights)  
**Testing**: xUnit with integration tests for agent orchestration and speech processing  
**Target Platform**: Blazor Interactive Server → Azure Container Apps via Aspire deployment  
**Project Type**: Web application with multi-service Aspire orchestration  
**Performance Goals**: <500ms transcription latency, <5s answer generation, 4-hour meeting support  
**Constraints**: Real-time SignalR updates, local audio buffer for connection recovery, 90-day transcript retention  
**Scale/Scope**: Single-user MVP (schema designed for multi-tenant expansion)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Modern .NET First | ✅ PASS | .NET 10, C# 14, top-level statements, records for DTOs, nullable enabled |
| II. Blazor Interactive Server | ✅ PASS | Existing Blazor app with `ReconnectModal` for SignalR resilience |
| III. Azure-Native Security | ✅ PASS | DefaultAzureCredential for managed identity, Key Vault for production |
| IV. AI Agent Framework | ✅ PASS | Microsoft.Agents.AI packages, will invoke aitk-* tools before codegen |
| V. Infrastructure as Code | ✅ PASS | .NET Aspire for local dev, azd + Bicep for Azure deployment |
| VI. Observability & Tracing | ✅ PASS | Application Insights, structured logging, correlation IDs |
| VII. Test-Driven Quality | ✅ PASS | xUnit tests for agents, speech processing, and API contracts |

**Pre-Design Gate**: ✅ ALL PASSED - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/001-meeting-copilot-mvp/
├── plan.md              # This file
├── research.md          # Phase 0: Technology research findings
├── data-model.md        # Phase 1: Cosmos DB entity definitions
├── quickstart.md        # Phase 1: Local development setup guide
├── contracts/           # Phase 1: API and SignalR hub contracts
│   ├── meeting-api.yaml # REST API for meeting management
│   ├── signalr-hubs.md  # SignalR hub specifications
│   └── agent-events.md  # Agent message/event contracts
└── tasks.md             # Phase 2: Implementation tasks (by /speckit.tasks)
```

### Source Code (repository root)

```text
# Existing Blazor app structure (enhanced for Aspire)
meeting-copilot/
├── meeting-copilot.sln
├── meeting-copilot.csproj           # Main Blazor app → becomes service project
├── Program.cs
├── appsettings.json
├── Agents/                          # Multi-agent orchestration (EXISTING)
│   ├── AgentCatalog.cs             # Agent registration/factory
│   └── AgentOrchestrator.cs        # Orchestration logic
├── Components/                      # Blazor UI (EXISTING)
│   ├── Layout/
│   └── Pages/
├── Data/                           # EF Core context (migrate to Cosmos)
│   ├── MeetingCopilotDbContext.cs
│   ├── Entities/
│   └── Repositories/
├── Services/
│   └── SpeechRecognitionService.cs # Azure Speech SDK (EXISTING)
└── wwwroot/

# NEW: Aspire orchestration projects
MeetingCopilot.AppHost/             # Aspire AppHost (orchestrator)
├── MeetingCopilot.AppHost.csproj
└── Program.cs                      # Service topology definition

MeetingCopilot.ServiceDefaults/     # Shared service configuration
├── MeetingCopilot.ServiceDefaults.csproj
└── Extensions.cs                   # OpenTelemetry, health checks

# NEW: Agent services (microservices)
src/
├── MeetingCopilot.Agents/          # Agent implementations
│   ├── TranscriptAgent/            # Real-time transcript processing
│   ├── AnswerAgent/                # Question answering (highest priority)
│   ├── ResearchAgent/              # Background research with web search
│   └── SummaryAgent/               # Agenda tracking, action items
├── MeetingCopilot.Memory/          # Cosmos DB memory provider
│   ├── CosmosMemoryProvider.cs
│   └── VectorSearchService.cs
└── MeetingCopilot.Contracts/       # Shared DTOs and interfaces

# Infrastructure
infra/
├── main.bicep                      # Azure resource definitions
├── modules/
│   ├── cosmos.bicep               # Cosmos DB with vector search
│   ├── speech.bicep               # Azure Speech Services
│   ├── ai.bicep                   # Azure AI Foundry
│   └── container-apps.bicep       # Container Apps environment
└── parameters/
    ├── dev.bicepparam
    └── prod.bicepparam

tests/
├── MeetingCopilot.Tests.Unit/
├── MeetingCopilot.Tests.Integration/
└── MeetingCopilot.Tests.Contract/
```

**Structure Decision**: Multi-project Aspire solution with service separation. The existing Blazor app becomes the frontend service. Agent logic moves to dedicated service projects for independent scaling. Cosmos DB memory provider follows james-tn/agent-memory patterns.

## Complexity Tracking

> **No violations detected - architecture aligns with Constitution principles**

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| Multi-project Aspire | Required | Constitution V mandates Aspire for local orchestration; multi-service enables agent isolation and independent scaling |
| Repository pattern | Keep existing | Already implemented in `Data/Repositories/`; aligns with Constitution data access abstraction |
| Cosmos DB over SQLite | Production path | Constitution III requires Azure-native; vector search needed for RAG (FR-054); schema designed for multi-tenant |

---

## Architecture Overview

### Multi-Agent System Design

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Blazor Interactive Server (Frontend)                   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐          │
│  │ Pre-Meet │ │ Answer   │ │ Key Pts  │ │ Research │ │ Summary  │          │
│  │ Setup    │ │ Panel    │ │ Panel    │ │ Panel    │ │ Panel    │          │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘          │
│                              SignalR Hub                                      │
└──────────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
        ┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
        │ Speech Service  │ │   Agent     │ │  Cosmos DB      │
        │ (Azure Speech)  │ │ Orchestrator│ │  Memory Store   │
        │                 │ │             │ │                 │
        │ • Real-time STT │ │ • Priority  │ │ • Transcripts   │
        │ • Diarization   │ │   routing   │ │ • Vector search │
        │ • Local buffer  │ │ • Fan-out   │ │ • Agent memory  │
        └─────────────────┘ └─────────────┘ └─────────────────┘
                                    │
                    ┌───────────────┼───────────────────────┐
                    ▼               ▼               ▼       ▼
            ┌───────────┐   ┌───────────┐   ┌───────────┐   ┌───────────┐
            │ Transcript│   │  Answer   │   │ Research  │   │ Summary   │
            │   Agent   │   │   Agent   │   │   Agent   │   │   Agent   │
            │           │   │           │   │           │   │           │
            │ • Store   │   │ • Q detect│   │ • Web srch│   │ • Agenda  │
            │ • Index   │   │ • RAG     │   │ • Topics  │   │ • Actions │
            │ • Speaker │   │ • Answer  │   │ • Cite    │   │ • Summary │
            └───────────┘   └───────────┘   └───────────┘   └───────────┘
```

### Agent Priority & Orchestration

| Agent | Priority | Trigger | Response Time | Output |
|-------|----------|---------|---------------|--------|
| **Answer Agent** | P1 (Highest) | Question detected in transcript | <5 seconds | Answer panel update |
| **Transcript Agent** | P1 | Continuous speech input | <500ms | Stored utterances |
| **Summary Agent** | P2 | Topic change / agenda progress | <3 seconds | Summary panel, key points |
| **Research Agent** | P3 | Background / explicit `/research` | <30 seconds | Research panel |

### Cosmos DB Schema (High-Level)

Following james-tn/agent-memory patterns:

| Container | Partition Key | Purpose | Vector Index |
|-----------|--------------|---------|---------------|
| `meetings` | `/meeting_id` | Meeting metadata, speakers, agenda | No |
| `speakers` | `/meeting_id` | Meeting participants with diarization IDs | No |
| `interactions` | `/user_id` | Utterances, Q&A pairs, agent messages | Yes (content_vector) |
| `insights` | `/user_id` | Key points, action items, research results | Yes (embedding) |
| `session_summaries` | `/user_id` | Meeting-level aggregated summaries (no TTL) | No |

### Data Flow

1. **Speech → Transcript**: Azure Speech SDK → SignalR → Transcript Agent → Cosmos `interactions`
2. **Question Detection**: Transcript Agent detects question → Answer Agent (priority queue)
3. **RAG Retrieval**: Agent queries Cosmos vector search → context injection → LLM
4. **Memory Persistence**: Agent state persisted to Cosmos via CosmosMemoryProvider
5. **UI Updates**: Agent outputs → SignalR broadcast → Blazor component state

---

## Key Technology Decisions

### 1. Microsoft Agent Framework Integration

```csharp
// Pattern from Microsoft docs - Multi-agent orchestration
WorkflowBuilder builder = new(transcriptAgent);
builder.AddEdge(transcriptAgent, answerAgent);    // Transcript → Q&A detection
builder.AddEdge(transcriptAgent, summaryAgent);   // Transcript → Summary update
builder.AddEdge(transcriptAgent, researchAgent);  // Transcript → Background research

Workflow workflow = await builder.BuildAsync();
```

### 2. Cosmos DB Memory Provider

Following james-tn/agent-memory patterns:
- `CosmosMemoryProvider` implements context provider interface
- Automatic recall tool injection for agents
- Vector search with cosine similarity (1536 dimensions for OpenAI embeddings)
- Hierarchical partitioning: user_id → meeting_id → interaction_id

### 3. Aspire Service Topology

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .AddDatabase("meetingcopilot");

var speech = builder.AddAzureAIServices("speech");

var agents = builder.AddProject<Projects.MeetingCopilot_Agents>("agents")
    .WithReference(cosmos)
    .WithReference(speech);

var web = builder.AddProject<Projects.MeetingCopilot>("web")
    .WithExternalHttpEndpoints()
    .WithReference(agents)
    .WithReference(cosmos);

builder.Build().Run();
```

---

## Phase 0 Deliverables

- [ ] `research.md` - Technology research consolidation
  - Microsoft Agent Framework patterns and packages
  - Cosmos DB vector search configuration
  - james-tn/agent-memory implementation patterns
  - Aspire multi-service orchestration patterns

## Phase 1 Deliverables

- [ ] `data-model.md` - Entity definitions with Cosmos schema
- [ ] `contracts/meeting-api.yaml` - REST API (OpenAPI 3.0)
- [ ] `contracts/signalr-hubs.md` - SignalR hub specifications
- [ ] `contracts/agent-events.md` - Inter-agent message contracts
- [ ] `quickstart.md` - Local development setup with Aspire

## Phase 2 (by /speckit.tasks - NOT part of this plan)

- Task breakdown by user story
- Implementation order and dependencies
- Acceptance criteria verification
