# Agent Event Contracts

**Feature**: 001-meeting-copilot-mvp  
**Date**: 2025-12-06

---

## Overview

This document defines the message contracts for inter-agent communication in the Meeting Copilot multi-agent system. Agents communicate through a message bus pattern following Microsoft Agent Framework conventions.

---

## Agent Definitions

| Agent | Responsibility | Priority | Input Sources | Output Targets |
|-------|---------------|----------|---------------|----------------|
| **TranscriptAgent** | Process speech, store utterances, detect questions | P1 | Azure Speech SDK | AnswerAgent, SummaryAgent, ResearchAgent, Cosmos |
| **AnswerAgent** | Generate answers to detected questions | P1 (Highest) | TranscriptAgent, User input | UI (SignalR), Cosmos |
| **SummaryAgent** | Track agenda, extract key points, action items | P2 | TranscriptAgent | UI (SignalR), Cosmos |
| **ResearchAgent** | Background research, web search | P3 | TranscriptAgent, User commands | UI (SignalR), Cosmos |

---

## Message Types

### Base Message Contract

All inter-agent messages inherit from this base:

```csharp
public abstract record AgentMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; init; }    // Links related messages
    public string MeetingId { get; init; }
    public string UserId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string SourceAgent { get; init; }
    public string? TargetAgent { get; init; }     // null = broadcast
}
```

---

## TranscriptAgent Events

### UtteranceProcessed

Emitted when a speech utterance is transcribed and stored.

```csharp
public record UtteranceProcessed : AgentMessage
{
    public string UtteranceId { get; init; }
    public string SpeakerId { get; init; }
    public string SpeakerName { get; init; }
    public string Content { get; init; }
    public float[] ContentVector { get; init; }   // 1536-dim embedding
    public bool ContainsQuestion { get; init; }   // Triggers AnswerAgent
    public double QuestionConfidence { get; init; }
}
```

**Routing**:
- If `ContainsQuestion = true` → AnswerAgent (priority)
- Always → SummaryAgent (for key point detection)
- Always → ResearchAgent (for topic detection)

---

### QuestionDetected

Emitted when a question is identified in the transcript.

```csharp
public record QuestionDetected : AgentMessage
{
    public string UtteranceId { get; init; }
    public string Question { get; init; }
    public string SpeakerId { get; init; }
    public string SpeakerName { get; init; }
    public int SpeakerPriority { get; init; }     // For answer prioritization
    public double Confidence { get; init; }
}
```

**Target**: AnswerAgent  
**Priority**: P1 - Must be processed immediately  
**SC-005**: Answer must be generated within 5 seconds

---

### SpeakerIdentified

Emitted when a speaker is newly identified or name is inferred.

```csharp
public record SpeakerIdentified : AgentMessage
{
    public string SpeakerId { get; init; }
    public string SpeakerLabel { get; init; }     // "Speaker 1", etc.
    public string? InferredName { get; init; }    // From transcript analysis
    public string? Role { get; init; }
    public bool IsNewSpeaker { get; init; }
}
```

---

## AnswerAgent Events

### AnswerGenerated

Emitted when an answer is ready for display.

```csharp
public record AnswerGenerated : AgentMessage
{
    public string QuestionId { get; init; }
    public string Question { get; init; }
    public string Answer { get; init; }           // Max 200 words (FR-013)
    public string DetailedAnswer { get; init; }   // Full answer for "More"
    public double Confidence { get; init; }
    public List<string> Sources { get; init; }    // RAG source references
    public int ProcessingTimeMs { get; init; }
}
```

**Target**: UI (via SignalR)  
**Storage**: Cosmos `interactions` container

---

### AnswerFailed

Emitted when answer generation fails.

```csharp
public record AnswerFailed : AgentMessage
{
    public string QuestionId { get; init; }
    public string Question { get; init; }
    public string ErrorCode { get; init; }
    public string ErrorMessage { get; init; }
    public bool WillRetry { get; init; }
}
```

---

## SummaryAgent Events

### KeyPointExtracted

Emitted when a significant point is identified.

```csharp
public record KeyPointExtracted : AgentMessage
{
    public string KeyPointId { get; init; }
    public string Title { get; init; }
    public string Content { get; init; }
    public double PriorityScore { get; init; }    // 0-100 for ranking
    public string SourceUtteranceId { get; init; }
    public string SpeakerName { get; init; }
}
```

**Target**: UI (via SignalR)  
**SC-006**: Must be emitted within 3 seconds of significant point

---

### ActionItemExtracted

Emitted when a todo/action item is detected.

```csharp
public record ActionItemExtracted : AgentMessage
{
    public string ActionItemId { get; init; }
    public string Title { get; init; }
    public string? Description { get; init; }
    public string? OwnerSpeakerId { get; init; }
    public string? OwnerName { get; init; }       // Auto-assigned (FR-038)
    public DateTimeOffset? DueDate { get; init; }
    public string SourceUtteranceId { get; init; }
}
```

---

### AgendaProgressUpdated

Emitted when agenda status changes.

```csharp
public record AgendaProgressUpdated : AgentMessage
{
    public string AgendaItemId { get; init; }
    public string Title { get; init; }
    public AgendaStatus Status { get; init; }
    public string? Summary { get; init; }
    public string? NextSuggestedTopic { get; init; }
}

public enum AgendaStatus { Pending, InProgress, Completed }
```

---

### MeetingSummaryGenerated

Emitted when end-of-meeting summary is complete.

```csharp
public record MeetingSummaryGenerated : AgentMessage
{
    public string SummaryId { get; init; }
    public string ExecutiveSummary { get; init; }
    public List<string> KeyTakeaways { get; init; }
    public List<TopicSummary> TopicsCovered { get; init; }
    public double AgendaCompletionRate { get; init; }
    public int ProcessingTimeMs { get; init; }
}

public record TopicSummary(
    string Topic,
    string Summary,
    int DurationSeconds
);
```

**SC-010**: Must be generated within 10 seconds of End Meeting

---

## ResearchAgent Events

### ResearchRequested

Triggers research on a topic (from transcript or /research command).

```csharp
public record ResearchRequested : AgentMessage
{
    public string RequestId { get; init; }
    public string Query { get; init; }
    public ResearchSource Source { get; init; }   // Automatic or Command
    public bool IncludeWebSearch { get; init; }   // FR-034
}

public enum ResearchSource { Automatic, Command }
```

---

### ResearchCompleted

Emitted when research results are ready.

```csharp
public record ResearchCompleted : AgentMessage
{
    public string RequestId { get; init; }
    public string Query { get; init; }
    public string Summary { get; init; }
    public string DetailedContent { get; init; }
    public List<WebSource> Sources { get; init; }
    public int ProcessingTimeMs { get; init; }
}

public record WebSource(
    string Title,
    string Url,
    string Snippet
);
```

**Target**: UI (via SignalR)  
**SC-007**: Must be emitted within 30 seconds

---

## Command Events

### SlashCommandReceived

Emitted when user enters a slash command.

```csharp
public record SlashCommandReceived : AgentMessage
{
    public string CommandType { get; init; }      // "research" | "speaker" | "suggest"
    public string Argument { get; init; }
}
```

**Routing**:
- `research` → ResearchAgent
- `speaker` → TranscriptAgent (updates speaker record)
- `suggest` → SummaryAgent

---

## Error Events

### AgentError

Standard error event for agent failures.

```csharp
public record AgentError : AgentMessage
{
    public string ErrorCode { get; init; }
    public string ErrorMessage { get; init; }
    public string? OriginalMessageId { get; init; }
    public bool IsRecoverable { get; init; }
    public int? RetryAfterMs { get; init; }
}
```

**Error Codes**:
- `AGENT_TIMEOUT` - Agent exceeded response time limit
- `LLM_ERROR` - LLM inference failed
- `EMBEDDING_ERROR` - Vector embedding generation failed
- `COSMOS_ERROR` - Database operation failed
- `SPEECH_ERROR` - Speech recognition error
- `WEB_SEARCH_ERROR` - Web search failed

---

## Message Flow Diagrams

### Question Detection Flow

```text
Speech Input
    │
    ▼
┌──────────────────┐
│ TranscriptAgent  │
│                  │
│ • Transcribe     │
│ • Store utterance│
│ • Detect question│
└────────┬─────────┘
         │ QuestionDetected (if question)
         ▼
┌──────────────────┐
│   AnswerAgent    │
│                  │
│ • RAG retrieval  │
│ • LLM generation │
│ • Format answer  │
└────────┬─────────┘
         │ AnswerGenerated
         ▼
    SignalR → UI
```

### Research Flow

```text
Transcript / /research command
    │
    ▼
┌──────────────────┐
│ TranscriptAgent  │ ──or── User Command
│                  │
│ • Topic detected │
└────────┬─────────┘
         │ ResearchRequested
         ▼
┌──────────────────┐
│  ResearchAgent   │
│                  │
│ • RAG retrieval  │
│ • Web search     │
│ • Synthesize     │
└────────┬─────────┘
         │ ResearchCompleted
         ▼
    SignalR → UI
```

### Summary Flow

```text
All Utterances
    │
    ▼
┌──────────────────┐
│  SummaryAgent    │
│                  │
│ • Key points     │──▶ KeyPointExtracted → UI
│ • Action items   │──▶ ActionItemExtracted → UI
│ • Agenda track   │──▶ AgendaProgressUpdated → UI
└────────┬─────────┘
         │
    End Meeting
         │
         ▼
┌──────────────────┐
│  SummaryAgent    │
│                  │
│ • Full summary   │
│ • Takeaways      │
│ • Statistics     │
└────────┬─────────┘
         │ MeetingSummaryGenerated
         ▼
    SignalR → UI
```

---

## Priority Queue Configuration

```csharp
public class AgentPriorityQueue
{
    // Priority levels (lower = higher priority)
    public const int ANSWER_PRIORITY = 1;      // P1 - Immediate
    public const int TRANSCRIPT_PRIORITY = 1;   // P1 - Real-time
    public const int SUMMARY_PRIORITY = 2;      // P2 - Background
    public const int RESEARCH_PRIORITY = 3;     // P3 - Low priority
    
    // Timeout configuration
    public static readonly Dictionary<int, TimeSpan> Timeouts = new()
    {
        [ANSWER_PRIORITY] = TimeSpan.FromSeconds(5),     // SC-005
        [TRANSCRIPT_PRIORITY] = TimeSpan.FromMilliseconds(500), // SC-002
        [SUMMARY_PRIORITY] = TimeSpan.FromSeconds(3),    // SC-006
        [RESEARCH_PRIORITY] = TimeSpan.FromSeconds(30)   // SC-007
    };
}
```
