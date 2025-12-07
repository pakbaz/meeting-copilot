# Data Model: Real-time Meeting AI Assistant

**Feature**: 001-meeting-copilot-mvp  
**Date**: 2025-12-06  
**Storage**: Azure Cosmos DB NoSQL with Vector Search

---

## Overview

This document defines the Cosmos DB schema for Meeting Copilot MVP. The design follows:

- **james-tn/agent-memory patterns** for agent state and RAG
- **Multi-tenant ready** schema with `user_id` isolation (FR-060)
- **Vector search** enabled for semantic retrieval (FR-054)
- **90-day retention** for transcripts, indefinite for summaries (FR-065, FR-066)

---

## Cosmos DB Containers

### Container: `meetings`

**Purpose**: Meeting metadata, configuration, and relationships  
**Partition Key**: `/meeting_id`  
**Vector Index**: None

```typescript
interface Meeting {
  // Identity
  id: string;                    // UUID
  meeting_id: string;            // Same as id (partition key)
  user_id: string;               // Owner user ID (multi-tenant isolation)
  
  // Core fields
  title: string;                 // FR-002
  description?: string;          // Rich text/markdown (FR-004)
  status: "setup" | "active" | "ended" | "archived";
  
  // Timing
  created_at: string;            // ISO 8601
  started_at?: string;           // When "Start Meeting" clicked
  ended_at?: string;             // When "End Meeting" clicked
  duration_seconds?: number;     // Calculated on end
  
  // Pre-meeting context
  agenda_items: AgendaItem[];    // Embedded array
  attachments: AttachmentRef[];  // References to blob storage
  initial_context?: string;      // Pasted markdown content
  
  // Configuration
  microphone_device_id?: string; // Selected microphone (FR-003)
  language: string;              // Default: "en-US" (FR-070)
  
  // TTL for 90-day retention
  _ts: number;                   // Cosmos timestamp
  ttl?: number;                  // Set to 7776000 (90 days) on archive
}

interface AgendaItem {
  id: string;                    // UUID
  title: string;
  order: number;
  status: "pending" | "in_progress" | "completed";
  summary?: string;              // AI-generated summary when completed
  started_at?: string;
  completed_at?: string;
}

interface AttachmentRef {
  id: string;
  filename: string;
  content_type: string;
  blob_uri: string;              // Azure Blob Storage URI
  uploaded_at: string;
}
```

---

### Container: `speakers`

**Purpose**: Meeting participants with speaker identification  
**Partition Key**: `/meeting_id`  
**Vector Index**: None

```typescript
interface Speaker {
  // Identity
  id: string;                    // UUID
  meeting_id: string;            // Partition key
  user_id: string;               // Owner user ID
  
  // Speaker identification
  speaker_label: string;         // "Speaker 1", "Speaker 2", etc. (initial)
  display_name?: string;         // Inferred or set via /speaker command (FR-019)
  role?: string;                 // Optional role from /speaker {name} {role}
  company?: string;              // Optional company name
  is_self: boolean;              // True if this is the user (FR-068)
  
  // Priority for question answering
  priority_rank: number;         // 1 = highest (user), others ranked by role
  
  // Timestamps
  first_seen_at: string;         // First utterance timestamp
  last_seen_at: string;          // Last utterance timestamp
}
```

---

### Container: `interactions`

**Purpose**: All transcribed utterances and agent interactions  
**Partition Key**: `/user_id`  
**Vector Index**: `content_vector` (1536 dimensions, cosine)

```typescript
interface Interaction {
  // Identity
  id: string;                    // UUID
  user_id: string;               // Partition key (multi-tenant)
  meeting_id: string;            // Reference to meeting
  
  // Type discriminator
  type: "utterance" | "question" | "answer" | "agent_message";
  
  // Content
  content: string;               // Transcribed text or generated content
  content_vector: number[];      // 1536-dim embedding for RAG
  
  // Speaker reference (for utterances)
  speaker_id?: string;           // Reference to speakers container
  speaker_name?: string;         // Denormalized for display
  
  // Question/Answer specific
  question_id?: string;          // Links answer to question
  confidence?: number;           // AI confidence score
  sources?: string[];            // RAG source references
  
  // Agent metadata
  agent_name?: string;           // "TranscriptAgent", "AnswerAgent", etc.
  processing_time_ms?: number;
  
  // Timestamps
  timestamp: string;             // ISO 8601 - when spoken/generated
  created_at: string;            // When stored
  
  // TTL
  ttl?: number;                  // Set to 7776000 on meeting archive
}
```

**Indexing Policy**:

```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    { "path": "/user_id/?" },
    { "path": "/meeting_id/?" },
    { "path": "/type/?" },
    { "path": "/timestamp/?" },
    { "path": "/speaker_id/?" }
  ],
  "excludedPaths": [
    { "path": "/content_vector/*" }
  ],
  "vectorIndexes": [
    {
      "path": "/content_vector",
      "type": "quantizedFlat"
    }
  ]
}
```

**Vector Embedding Policy**:

```json
{
  "vectorEmbeddings": [
    {
      "path": "/content_vector",
      "dataType": "float32",
      "distanceFunction": "cosine",
      "dimensions": 1536
    }
  ]
}
```

---

### Container: `insights`

**Purpose**: Key points, action items, and extracted insights  
**Partition Key**: `/user_id`  
**Vector Index**: `embedding` (1536 dimensions, cosine)

```typescript
interface Insight {
  // Identity
  id: string;                    // UUID
  user_id: string;               // Partition key
  meeting_id: string;
  
  // Type discriminator
  type: "key_point" | "action_item" | "parking_lot" | "research_result";
  
  // Content
  title: string;                 // Short display title
  content: string;               // Full content
  embedding: number[];           // 1536-dim for semantic search
  
  // Key point specific
  priority_score?: number;       // 0-100, for ranking top 5 (FR-024)
  source_utterance_id?: string;  // Reference to source interaction
  
  // Action item specific (FR-037, FR-038)
  owner_speaker_id?: string;     // Assigned owner
  owner_name?: string;           // Denormalized
  due_date?: string;             // If mentioned
  status?: "pending" | "in_progress" | "completed";
  
  // Research result specific (FR-030, FR-034)
  research_query?: string;       // Original query
  web_sources?: WebSource[];     // Citations
  
  // Timestamps
  created_at: string;
  updated_at: string;
  
  // TTL (action items exempt from TTL)
  ttl?: number;
}

interface WebSource {
  title: string;
  url: string;
  snippet: string;
}
```

---

### Container: `session_summaries`

**Purpose**: Aggregated meeting summaries (retained indefinitely)  
**Partition Key**: `/user_id`  
**Vector Index**: None

```typescript
interface SessionSummary {
  // Identity
  id: string;                    // UUID
  user_id: string;               // Partition key
  meeting_id: string;            // 1:1 with meeting
  
  // Summary content (FR-051)
  executive_summary: string;     // AI-generated summary
  key_takeaways: string[];       // Bullet points
  topics_covered: TopicSummary[];
  
  // Agenda completion
  agenda_completion_rate: number; // 0-100%
  
  // Aggregated stats
  total_utterances: number;
  total_duration_seconds: number;
  speaker_stats: SpeakerStats[];
  
  // Action items snapshot (denormalized for retention)
  action_items: ActionItemSnapshot[];
  
  // Timestamps
  meeting_started_at: string;
  meeting_ended_at: string;
  generated_at: string;
  
  // No TTL - retained indefinitely
}

interface TopicSummary {
  topic: string;
  summary: string;
  duration_seconds: number;
}

interface SpeakerStats {
  speaker_id: string;
  speaker_name: string;
  utterance_count: number;
  total_speaking_seconds: number;
}

interface ActionItemSnapshot {
  id: string;
  title: string;
  owner_name?: string;
  status: string;
}
```

---

## Entity Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                         user_id (tenant)                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌──────────┐         ┌───────────┐         ┌──────────────┐   │
│   │ meetings │ ──1:N── │  speakers │         │session_summ. │   │
│   │          │         │           │         │              │   │
│   │ meeting_id│         │meeting_id │         │  meeting_id  │   │
│   └────┬─────┘         └─────┬─────┘         └──────────────┘   │
│        │                     │                                   │
│        │                     │                                   │
│        │      ┌──────────────┴──────────────┐                   │
│        │      │                             │                   │
│        ▼      ▼                             ▼                   │
│   ┌────────────────┐               ┌────────────────┐           │
│   │  interactions  │               │    insights    │           │
│   │                │               │                │           │
│   │ • utterances   │──references──▶│ • key_points   │           │
│   │ • questions    │               │ • action_items │           │
│   │ • answers      │               │ • research     │           │
│   │ • agent_msgs   │               │ • parking_lot  │           │
│   │                │               │                │           │
│   │ [vector index] │               │ [vector index] │           │
│   └────────────────┘               └────────────────┘           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Retention Rules

| Container | TTL Strategy | Retention |
|-----------|--------------|-----------|
| `meetings` | Set `ttl=7776000` on archive | 90 days after archive |
| `speakers` | Cascade with meeting | 90 days after meeting archive |
| `interactions` | Set `ttl=7776000` on meeting archive | 90 days after meeting archive |
| `insights` (key_points, research) | Set `ttl=7776000` on meeting archive | 90 days |
| `insights` (action_items) | No TTL | Indefinite |
| `session_summaries` | No TTL | Indefinite |

---

## C# Record Definitions

```csharp
// Cosmos DB entities using C# records (Constitution I compliance)

public record Meeting(
    string Id,
    string MeetingId,
    string UserId,
    string Title,
    string? Description,
    MeetingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    int? DurationSeconds,
    List<AgendaItem> AgendaItems,
    List<AttachmentRef> Attachments,
    string? InitialContext,
    string? MicrophoneDeviceId,
    string Language,
    int? Ttl = null
);

public record AgendaItem(
    string Id,
    string Title,
    int Order,
    AgendaStatus Status,
    string? Summary,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt
);

public record Speaker(
    string Id,
    string MeetingId,
    string UserId,
    string SpeakerLabel,
    string? DisplayName,
    string? Role,
    string? Company,
    bool IsSelf,
    int PriorityRank,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt
);

public record Interaction(
    string Id,
    string UserId,
    string MeetingId,
    InteractionType Type,
    string Content,
    float[] ContentVector,
    string? SpeakerId,
    string? SpeakerName,
    string? QuestionId,
    double? Confidence,
    List<string>? Sources,
    string? AgentName,
    int? ProcessingTimeMs,
    DateTimeOffset Timestamp,
    DateTimeOffset CreatedAt,
    int? Ttl = null
);

public record Insight(
    string Id,
    string UserId,
    string MeetingId,
    InsightType Type,
    string Title,
    string Content,
    float[] Embedding,
    double? PriorityScore,
    string? SourceUtteranceId,
    string? OwnerSpeakerId,
    string? OwnerName,
    DateTimeOffset? DueDate,
    InsightStatus? Status,
    string? ResearchQuery,
    List<WebSource>? WebSources,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int? Ttl = null
);

public record SessionSummary(
    string Id,
    string UserId,
    string MeetingId,
    string ExecutiveSummary,
    List<string> KeyTakeaways,
    List<TopicSummary> TopicsCovered,
    double AgendaCompletionRate,
    int TotalUtterances,
    int TotalDurationSeconds,
    List<SpeakerStats> SpeakerStats,
    List<ActionItemSnapshot> ActionItems,
    DateTimeOffset MeetingStartedAt,
    DateTimeOffset MeetingEndedAt,
    DateTimeOffset GeneratedAt
);

// Enums
public enum MeetingStatus { Setup, Active, Ended, Archived }
public enum AgendaStatus { Pending, InProgress, Completed }
public enum InteractionType { Utterance, Question, Answer, AgentMessage }
public enum InsightType { KeyPoint, ActionItem, ParkingLot, ResearchResult }
public enum InsightStatus { Pending, InProgress, Completed }
```

---

## Validation Rules

| Entity | Field | Rule | FR Reference |
|--------|-------|------|--------------|
| Meeting | title | Required, max 200 chars | FR-002 |
| Meeting | language | Default "en-US" | FR-070 |
| Speaker | is_self | Exactly one per meeting | FR-068 |
| Speaker | priority_rank | 1 for is_self=true | FR-068 |
| Interaction | content_vector | Required, 1536 dimensions | FR-054 |
| Insight (key_point) | priority_score | 0-100 | FR-024 |
| Insight (action_item) | ttl | Must be null (indefinite) | FR-066 |

---

## Migration Notes

The existing codebase has EF Core entities in `Data/Entities/`:

- `GuestInfo.cs` → Migrate to `Speaker` record
- `Keypoint.cs` → Migrate to `Insight` record with type `KeyPoint`

Migration steps:
1. Create Cosmos DB containers with above schema
2. Implement `CosmosMemoryProvider` following james-tn patterns
3. Create repository adapters that implement existing repository interfaces
4. Gradually migrate from SQLite to Cosmos during MVP development
