# SignalR Hub Specifications

**Feature**: 001-meeting-copilot-mvp  
**Date**: 2025-12-06

---

## Overview

Meeting Copilot uses SignalR for real-time bidirectional communication between the Blazor Interactive Server frontend and backend services. This document specifies the hub contracts.

---

## Hub: MeetingHub

**Path**: `/hubs/meeting`  
**Authentication**: Required (user context from circuit)

### Client → Server Methods

#### `JoinMeeting(meetingId: string): Task`

Join a meeting room to receive updates.

```csharp
// Client invocation
await hubConnection.InvokeAsync("JoinMeeting", meetingId);
```

**Behavior**:
- Adds connection to meeting group
- Returns current meeting state
- Starts receiving real-time updates

---

#### `LeaveMeeting(meetingId: string): Task`

Leave a meeting room.

```csharp
await hubConnection.InvokeAsync("LeaveMeeting", meetingId);
```

---

#### `StartTranscription(meetingId: string): Task`

Begin real-time transcription (FR-006).

```csharp
await hubConnection.InvokeAsync("StartTranscription", meetingId);
```

**Behavior**:
- Initiates Azure Speech SDK connection
- Begins streaming audio to Speech Service
- Starts emitting `TranscriptionUpdate` events

---

#### `StopTranscription(meetingId: string): Task`

Stop transcription (on End Meeting).

```csharp
await hubConnection.InvokeAsync("StopTranscription", meetingId);
```

---

#### `SendCommand(meetingId: string, command: SlashCommand): Task`

Process slash commands (FR-043).

```csharp
public record SlashCommand(
    string Type,      // "research" | "speaker" | "suggest"
    string Argument   // Command argument
);

await hubConnection.InvokeAsync("SendCommand", meetingId, new SlashCommand("research", "cloud migration"));
```

**Commands**:
- `/research {topic}` - Triggers Research Agent (FR-031)
- `/speaker {name} {role?}` - Updates current speaker (FR-019)
- `/suggest` - Triggers Summary Agent suggestion (FR-036)

---

#### `SubmitQuestion(meetingId: string, question: string): Task`

Manually submit a question for Answer Agent.

```csharp
await hubConnection.InvokeAsync("SubmitQuestion", meetingId, "What is our quarterly budget?");
```

---

### Server → Client Methods

#### `TranscriptionUpdate(update: TranscriptionEvent)`

Real-time transcription updates (FR-007).

```csharp
public record TranscriptionEvent(
    string Id,
    string MeetingId,
    string SpeakerId,
    string SpeakerName,
    string Content,
    bool IsFinal,           // true = final transcript, false = interim
    DateTimeOffset Timestamp
);
```

**Frequency**: Interim updates every ~300ms, final on utterance end  
**SC-002**: First update within 500ms of speech detection

---

#### `SpeakerChanged(speaker: SpeakerEvent)`

Speaker identification update (FR-008, FR-009).

```csharp
public record SpeakerEvent(
    string MeetingId,
    string SpeakerId,
    string SpeakerLabel,
    string? DisplayName,
    bool IsNewSpeaker,
    DateTimeOffset Timestamp
);
```

**SC-004**: Emitted within 1 second of speaker transition

---

#### `AnswerReady(answer: AnswerEvent)`

Answer Agent response (FR-012, FR-013).

```csharp
public record AnswerEvent(
    string Id,
    string MeetingId,
    string QuestionId,
    string Question,        // Original detected question
    string Answer,          // Concise answer (max 200 words) - FR-013
    string? DetailedAnswer, // Full answer for "More" button
    double Confidence,
    List<string> Sources,
    DateTimeOffset Timestamp
);
```

**SC-005**: Emitted within 5 seconds of question detection

---

#### `KeyPointsUpdated(keyPoints: KeyPointsEvent)`

Key points list update (FR-022, FR-023, FR-024).

```csharp
public record KeyPointsEvent(
    string MeetingId,
    List<KeyPointItem> KeyPoints,  // Max 5 items, sorted by priority
    DateTimeOffset Timestamp
);

public record KeyPointItem(
    string Id,
    string Title,
    string Content,
    double PriorityScore,
    string? SourceSpeakerName
);
```

**SC-006**: Emitted within 3 seconds of significant discussion point

---

#### `ResearchReady(research: ResearchEvent)`

Research Agent results (FR-032, FR-033).

```csharp
public record ResearchEvent(
    string Id,
    string MeetingId,
    string Query,
    string Summary,           // Concise summary
    string? DetailedContent,  // Full content for "More" button
    List<WebSource> Sources,
    DateTimeOffset Timestamp
);

public record WebSource(
    string Title,
    string Url,
    string Snippet
);
```

**SC-007**: Emitted within 30 seconds of topic identification

---

#### `SummaryUpdated(summary: SummaryEvent)`

Summary Agent update (FR-035, FR-036).

```csharp
public record SummaryEvent(
    string MeetingId,
    List<AgendaProgress> AgendaProgress,
    string? NextSuggestedTopic,
    DateTimeOffset Timestamp
);

public record AgendaProgress(
    string ItemId,
    string Title,
    AgendaStatus Status,
    string? Summary
);
```

---

#### `ActionItemDetected(actionItem: ActionItemEvent)`

Action item extraction (FR-037, FR-038).

```csharp
public record ActionItemEvent(
    string Id,
    string MeetingId,
    string Title,
    string? OwnerSpeakerId,
    string? OwnerName,
    DateTimeOffset Timestamp
);
```

---

#### `ConnectionStatusChanged(status: ConnectionStatus)`

Connection health updates (existing ReconnectModal integration).

```csharp
public record ConnectionStatus(
    string Status,           // "connected" | "reconnecting" | "disconnected"
    string? Reason,
    bool IsBuffering,        // True if buffering locally (FR-062)
    DateTimeOffset Timestamp
);
```

---

#### `MicrophoneLevelWarning(warning: MicrophoneWarning)`

Audio level warning (FR-011).

```csharp
public record MicrophoneWarning(
    string MeetingId,
    string Level,            // "low" | "silent"
    DateTimeOffset Timestamp
);
```

---

## Hub: AgentHub

**Path**: `/hubs/agents`  
**Purpose**: Internal agent-to-agent communication (not exposed to client)

### Methods

#### `BroadcastAgentMessage(message: AgentMessage): Task`

Inter-agent communication for orchestration.

```csharp
public record AgentMessage(
    string Id,
    string FromAgent,       // "TranscriptAgent", "AnswerAgent", etc.
    string ToAgent,         // Target agent or "*" for broadcast
    string Type,            // "utterance", "question_detected", "research_request"
    object Payload,
    DateTimeOffset Timestamp
);
```

---

## Connection Lifecycle

### Client Connection Flow

```text
1. Client connects to /hubs/meeting
2. Client calls JoinMeeting(meetingId)
3. Server adds to meeting group, returns current state
4. Client calls StartTranscription(meetingId) when ready
5. Server streams TranscriptionUpdate events
6. On disconnect: Server buffers locally, emits ConnectionStatusChanged
7. On reconnect: Server syncs buffered updates
8. Client calls StopTranscription(meetingId) on End Meeting
9. Client calls LeaveMeeting(meetingId)
```

### Reconnection Handling (FR-062)

```csharp
// Client configuration
var hubConnection = new HubConnectionBuilder()
    .WithUrl("/hubs/meeting")
    .WithAutomaticReconnect(new[] { 
        TimeSpan.Zero,      // Immediate retry
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    })
    .Build();
```

---

## Error Events

#### `Error(error: HubError)`

Error notification to client.

```csharp
public record HubError(
    string Code,
    string Message,
    string? MeetingId,
    DateTimeOffset Timestamp
);
```

**Error Codes**:
- `MEETING_NOT_FOUND` - Meeting ID does not exist
- `MEETING_NOT_ACTIVE` - Meeting not in active state
- `TRANSCRIPTION_FAILED` - Speech service error
- `AGENT_TIMEOUT` - Agent response timeout
- `UNAUTHORIZED` - User not authorized for meeting
