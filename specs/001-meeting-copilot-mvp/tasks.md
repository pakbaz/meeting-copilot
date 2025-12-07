# Tasks: Real-time Meeting AI Assistant (MVP)

**Input**: Design documents from `/specs/001-meeting-copilot-mvp/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Not explicitly requested - test tasks omitted (add via separate request if needed)

**Organization**: Tasks grouped by user story to enable independent implementation and testing

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project restructuring for Aspire orchestration and shared projects

- [X] T001 Create Aspire AppHost project at `MeetingCopilot.AppHost/MeetingCopilot.AppHost.csproj`
- [X] T002 Create ServiceDefaults project at `MeetingCopilot.ServiceDefaults/MeetingCopilot.ServiceDefaults.csproj`
- [X] T003 [P] Create Contracts project at `src/MeetingCopilot.Contracts/MeetingCopilot.Contracts.csproj`
- [X] T004 [P] Create Memory project at `src/MeetingCopilot.Memory/MeetingCopilot.Memory.csproj`
- [X] T005 [P] Create Agents project at `src/MeetingCopilot.Agents/MeetingCopilot.Agents.csproj`
- [X] T006 Update solution file `meeting-copilot.sln` to include all new projects
- [X] T007 Configure Aspire service topology in `MeetingCopilot.AppHost/Program.cs`
- [X] T008 [P] Add Microsoft.Agents.AI preview packages to `src/MeetingCopilot.Agents/MeetingCopilot.Agents.csproj`
- [X] T009 [P] Add Microsoft.Azure.Cosmos package to `src/MeetingCopilot.Memory/MeetingCopilot.Memory.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Cosmos DB Setup

- [ ] T010 Create C# record definitions for all entities in `src/MeetingCopilot.Contracts/Entities/`
- [ ] T011 [P] Create `Meeting.cs` record in `src/MeetingCopilot.Contracts/Entities/Meeting.cs`
- [ ] T012 [P] Create `Speaker.cs` record in `src/MeetingCopilot.Contracts/Entities/Speaker.cs`
- [ ] T013 [P] Create `Interaction.cs` record in `src/MeetingCopilot.Contracts/Entities/Interaction.cs`
- [ ] T014 [P] Create `Insight.cs` record in `src/MeetingCopilot.Contracts/Entities/Insight.cs`
- [ ] T015 [P] Create `SessionSummary.cs` record in `src/MeetingCopilot.Contracts/Entities/SessionSummary.cs`
- [ ] T016 Implement `CosmosDbService.cs` base class in `src/MeetingCopilot.Memory/CosmosDbService.cs`
- [ ] T017 Implement container initialization with vector policies in `src/MeetingCopilot.Memory/CosmosContainerInitializer.cs`

### Memory Provider (james-tn/agent-memory pattern)

- [ ] T018 Implement `IMemoryProvider` interface in `src/MeetingCopilot.Contracts/Interfaces/IMemoryProvider.cs`
- [ ] T019 Implement `CosmosMemoryProvider.cs` in `src/MeetingCopilot.Memory/CosmosMemoryProvider.cs`
- [ ] T020 Implement `VectorSearchService.cs` in `src/MeetingCopilot.Memory/VectorSearchService.cs`
- [ ] T021 Implement `EmbeddingService.cs` for OpenAI embeddings in `src/MeetingCopilot.Memory/EmbeddingService.cs`

### SignalR Hubs

- [ ] T022 Create `MeetingHub.cs` SignalR hub in `Hubs/MeetingHub.cs`
- [ ] T023 [P] Create SignalR event DTOs in `src/MeetingCopilot.Contracts/Events/`
- [ ] T024 [P] Create `TranscriptionEvent.cs` in `src/MeetingCopilot.Contracts/Events/TranscriptionEvent.cs`
- [ ] T025 [P] Create `AnswerEvent.cs` in `src/MeetingCopilot.Contracts/Events/AnswerEvent.cs`
- [ ] T026 [P] Create `KeyPointsEvent.cs` in `src/MeetingCopilot.Contracts/Events/KeyPointsEvent.cs`
- [ ] T027 [P] Create `ResearchEvent.cs` in `src/MeetingCopilot.Contracts/Events/ResearchEvent.cs`
- [ ] T028 Register SignalR hub in `Program.cs` with `/hubs/meeting` endpoint

### Agent Infrastructure

- [ ] T029 Create `IAgent` interface in `src/MeetingCopilot.Contracts/Interfaces/IAgent.cs`
- [ ] T030 Create `AgentMessage` base record in `src/MeetingCopilot.Contracts/Messages/AgentMessage.cs`
- [ ] T031 Implement `AgentOrchestrator.cs` with priority queue in `src/MeetingCopilot.Agents/AgentOrchestrator.cs`
- [ ] T032 Implement `AgentPriorityQueue.cs` in `src/MeetingCopilot.Agents/AgentPriorityQueue.cs`

### Repository Migration

- [ ] T033 Create `IMeetingRepository` interface in `src/MeetingCopilot.Contracts/Interfaces/IMeetingRepository.cs`
- [ ] T034 Implement `CosmosMeetingRepository.cs` in `src/MeetingCopilot.Memory/Repositories/CosmosMeetingRepository.cs`
- [ ] T035 [P] Create `ISpeakerRepository` interface in `src/MeetingCopilot.Contracts/Interfaces/ISpeakerRepository.cs`
- [ ] T036 [P] Implement `CosmosSpeakerRepository.cs` in `src/MeetingCopilot.Memory/Repositories/CosmosSpeakerRepository.cs`
- [ ] T037 [P] Create `IInteractionRepository` interface in `src/MeetingCopilot.Contracts/Interfaces/IInteractionRepository.cs`
- [ ] T038 [P] Implement `CosmosInteractionRepository.cs` in `src/MeetingCopilot.Memory/Repositories/CosmosInteractionRepository.cs`
- [ ] T039 [P] Create `IInsightRepository` interface in `src/MeetingCopilot.Contracts/Interfaces/IInsightRepository.cs`
- [ ] T040 [P] Implement `CosmosInsightRepository.cs` in `src/MeetingCopilot.Memory/Repositories/CosmosInsightRepository.cs`

### DI Registration

- [ ] T041 Register all services in `Program.cs` with scoped lifetime for Cosmos repositories
- [ ] T042 Configure `appsettings.json` with Cosmos DB connection settings
- [ ] T043 Verify Aspire orchestration starts all services with `dotnet run` in AppHost

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Pre-Meeting Setup (Priority: P1) 🎯 MVP

**Goal**: User can set up meeting context before it starts (title, microphone, agenda, attachments)

**Independent Test**: Open app → select microphone → enter meeting details → click Start Meeting → see in-meeting view

### Implementation for User Story 1

- [X] T044 [US1] Create `PreMeetingSetup.razor` component in `Components/Pages/PreMeetingSetup.razor`
- [X] T045 [US1] Implement microphone device enumeration in `Services/MicrophoneService.cs`
- [X] T046 [P] [US1] Create `MicrophoneSelector.razor` component in `Components/Shared/MicrophoneSelector.razor`
- [X] T047 [P] [US1] Create `AgendaEditor.razor` component in `Components/Shared/AgendaEditor.razor`
- [X] T048 [P] [US1] Create `AttachmentUploader.razor` component in `Components/Shared/AttachmentUploader.razor`
- [X] T049 [US1] Implement `MeetingService.cs` for meeting CRUD in `Services/MeetingService.cs`
- [X] T050 [US1] Add meeting creation endpoint `POST /api/meetings` in `Controllers/MeetingsController.cs`
- [X] T051 [US1] Implement "Start Meeting" button navigation to in-meeting view
- [X] T052 [US1] Store meeting to Cosmos DB via `CosmosMeetingRepository` on Start Meeting

**Checkpoint**: User Story 1 complete - pre-meeting setup functional

---

## Phase 4: User Story 2 - Real-time Speech Transcription (Priority: P1) 🎯 MVP

**Goal**: Continuous real-time transcription with speaker identification

**Independent Test**: Start meeting → speak → see transcription with speaker labels in real-time → verify stored in DB

### Implementation for User Story 2

- [ ] T053 [US2] Implement `TranscriptAgent.cs` in `src/MeetingCopilot.Agents/TranscriptAgent/TranscriptAgent.cs`
- [ ] T054 [US2] Enhance `SpeechRecognitionService.cs` with speaker diarization in `Services/SpeechRecognitionService.cs`
- [ ] T055 [US2] Implement `UtteranceProcessed` event emission in TranscriptAgent
- [ ] T056 [US2] Create `TranscriptPanel.razor` component in `Components/Meeting/TranscriptPanel.razor`
- [ ] T057 [P] [US2] Create `SpeakerLabel.razor` component in `Components/Shared/SpeakerLabel.razor`
- [ ] T058 [US2] Implement speaker name inference from transcript in `Services/SpeakerInferenceService.cs`
- [ ] T059 [US2] Add `/speaker {name}` command handler in `Services/SlashCommandService.cs`
- [ ] T060 [US2] Implement microphone level warning UI in `Components/Shared/MicrophoneLevelIndicator.razor`
- [ ] T061 [US2] Implement local audio buffering for connection recovery in `Services/AudioBufferService.cs`
- [ ] T062 [US2] Add `ConnectionStatusChanged` SignalR event for degraded mode indication
- [ ] T063 [US2] Store utterances to Cosmos `interactions` container via repository

**Checkpoint**: User Story 2 complete - real-time transcription with diarization working

---

## Phase 5: User Story 3 - Immediate Question Answering (Priority: P1) 🎯 MVP

**Goal**: AI answers detected questions with highest priority

**Independent Test**: During meeting, ask a question → see answer in Answer panel within 5 seconds

### Implementation for User Story 3

- [ ] T064 [US3] Implement `AnswerAgent.cs` in `src/MeetingCopilot.Agents/AnswerAgent/AnswerAgent.cs`
- [ ] T065 [US3] Implement question detection logic in `Services/QuestionDetectionService.cs`
- [ ] T066 [US3] Create `QuestionDetected` message type in `src/MeetingCopilot.Contracts/Messages/QuestionDetected.cs`
- [ ] T067 [US3] Implement RAG retrieval for answer context in AnswerAgent
- [ ] T068 [US3] Create `AnswerPanel.razor` component in `Components/Meeting/AnswerPanel.razor`
- [ ] T069 [P] [US3] Create `DetailedAnswerView.razor` component in `Components/Meeting/DetailedAnswerView.razor`
- [ ] T070 [US3] Implement speaker priority ranking for question prioritization
- [ ] T071 [US3] Add `AnswerReady` SignalR event broadcast from AnswerAgent
- [ ] T072 [US3] Store Q&A interactions to Cosmos `interactions` container

**Checkpoint**: User Story 3 complete - question answering functional

---

## Phase 6: User Story 4 - Key Points Tracking (Priority: P1) 🎯 MVP

**Goal**: Real-time top 5 key points sorted by importance

**Independent Test**: During meeting, discuss important topics → see key points appear and reorder

### Implementation for User Story 4

- [ ] T073 [US4] Implement key point extraction in `SummaryAgent.cs` (partial) in `src/MeetingCopilot.Agents/SummaryAgent/SummaryAgent.cs`
- [ ] T074 [US4] Create `KeyPointExtracted` message type in `src/MeetingCopilot.Contracts/Messages/KeyPointExtracted.cs`
- [ ] T075 [US4] Create `KeyPointsPanel.razor` component in `Components/Meeting/KeyPointsPanel.razor`
- [ ] T076 [US4] Implement key point priority scoring algorithm in SummaryAgent
- [ ] T077 [US4] Add "Follow Up" button to add key point to parking lot
- [ ] T078 [US4] Add `KeyPointsUpdated` SignalR event broadcast
- [ ] T079 [US4] Store key points to Cosmos `insights` container with type `key_point`

**Checkpoint**: User Story 4 complete - key points tracking functional

---

## Phase 7: User Story 5 - Background Research Agent (Priority: P2)

**Goal**: Automatic and on-demand background research on topics

**Independent Test**: Type `/research {topic}` → see research results in Research panel

### Implementation for User Story 5

- [ ] T080 [US5] Implement `ResearchAgent.cs` in `src/MeetingCopilot.Agents/ResearchAgent/ResearchAgent.cs`
- [ ] T081 [US5] Integrate web search capability via Microsoft Foundry in ResearchAgent
- [ ] T082 [US5] Create `ResearchRequested` message type in `src/MeetingCopilot.Contracts/Messages/ResearchRequested.cs`
- [ ] T083 [US5] Create `ResearchPanel.razor` component in `Components/Meeting/ResearchPanel.razor`
- [ ] T084 [P] [US5] Create `ResearchDetailView.razor` component in `Components/Meeting/ResearchDetailView.razor`
- [ ] T085 [US5] Add `/research {topic}` command handler in `Services/SlashCommandService.cs`
- [ ] T086 [US5] Implement automatic topic detection for background research
- [ ] T087 [US5] Add `ResearchReady` SignalR event broadcast
- [ ] T088 [US5] Store research results to Cosmos `insights` container with type `research_result`

**Checkpoint**: User Story 5 complete - research agent functional

---

## Phase 8: User Story 6 - Meeting Summary & Agenda Tracking (Priority: P2)

**Goal**: Real-time summary of covered topics and next discussion items

**Independent Test**: Enter agenda in pre-meeting → discuss items → see summary panel update

### Implementation for User Story 6

- [ ] T089 [US6] Enhance `SummaryAgent.cs` with agenda tracking in `src/MeetingCopilot.Agents/SummaryAgent/SummaryAgent.cs`
- [ ] T090 [US6] Create `AgendaProgressUpdated` message type in `src/MeetingCopilot.Contracts/Messages/AgendaProgressUpdated.cs`
- [ ] T091 [US6] Create `SummaryPanel.razor` component in `Components/Meeting/SummaryPanel.razor`
- [ ] T092 [US6] Implement agenda item status tracking (pending/in_progress/completed)
- [ ] T093 [US6] Add `/suggest` command handler for next topic suggestion
- [ ] T094 [US6] Add `SummaryUpdated` SignalR event broadcast

**Checkpoint**: User Story 6 complete - summary and agenda tracking functional

---

## Phase 9: User Story 7 - Action Items & Parking Lot (Priority: P2)

**Goal**: Automatic extraction of action items with owner assignment

**Independent Test**: Mention action items in meeting → see them appear with assigned owners

### Implementation for User Story 7

- [ ] T095 [US7] Implement action item extraction in `SummaryAgent.cs`
- [ ] T096 [US7] Create `ActionItemExtracted` message type in `src/MeetingCopilot.Contracts/Messages/ActionItemExtracted.cs`
- [ ] T097 [US7] Create `ActionItemsPanel.razor` component in `Components/Meeting/ActionItemsPanel.razor`
- [ ] T098 [P] [US7] Create `ParkingLotView.razor` component in `Components/Meeting/ParkingLotView.razor`
- [ ] T099 [US7] Implement automatic owner assignment based on speaker context
- [ ] T100 [US7] Add `ActionItemDetected` SignalR event broadcast
- [ ] T101 [US7] Store action items to Cosmos `insights` container with type `action_item` (no TTL)

**Checkpoint**: User Story 7 complete - action items extraction functional

---

## Phase 10: User Story 8 - End Meeting & Final Summary (Priority: P2)

**Goal**: Comprehensive meeting summary when meeting ends

**Independent Test**: Click "End Meeting" → see summary view with takeaways, action items, transcript access

### Implementation for User Story 8

- [ ] T102 [US8] Create `MeetingSummaryView.razor` component in `Components/Pages/MeetingSummaryView.razor`
- [ ] T103 [US8] Implement end-meeting summary generation in SummaryAgent
- [ ] T104 [US8] Create `MeetingSummaryGenerated` message type in `src/MeetingCopilot.Contracts/Messages/MeetingSummaryGenerated.cs`
- [ ] T105 [P] [US8] Create `FullTranscriptView.razor` component in `Components/Meeting/FullTranscriptView.razor`
- [ ] T106 [P] [US8] Create `KeyPointsTranscriptView.razor` component in `Components/Meeting/KeyPointsTranscriptView.razor`
- [ ] T107 [US8] Store session summary to Cosmos `session_summaries` container
- [ ] T108 [US8] Implement meeting status transition to "ended" with TTL setting

**Checkpoint**: User Story 8 complete - end meeting flow functional

---

## Phase 11: User Story 9 - Input Area & Slash Commands (Priority: P2)

**Goal**: Collapsed input area with slash command support

**Independent Test**: During meeting, type slash commands → verify they are processed correctly

### Implementation for User Story 9

- [ ] T109 [US9] Create `CollapsedInputArea.razor` component in `Components/Meeting/CollapsedInputArea.razor`
- [ ] T110 [US9] Implement slash command parser in `Services/SlashCommandService.cs`
- [ ] T111 [US9] Add `/speaker {name} {role}` extended command support
- [ ] T112 [US9] Implement rich text/markdown input handling
- [ ] T113 [US9] Process all inputs as high priority via AgentOrchestrator

**Checkpoint**: User Story 9 complete - input area and commands functional

---

## Phase 12: User Story 10 - Top Bar Controls (Priority: P3)

**Goal**: Clean top bar with meeting status and controls

**Independent Test**: During meeting, verify top bar shows title, speaker, elapsed time, End Meeting button

### Implementation for User Story 10

- [ ] T114 [US10] Create `MeetingTopBar.razor` component in `Components/Meeting/MeetingTopBar.razor`
- [ ] T115 [US10] Implement real-time elapsed time display
- [ ] T116 [US10] Display current speaker name from SignalR events
- [ ] T117 [US10] Add End Meeting button with confirmation

**Checkpoint**: User Story 10 complete - all user stories implemented

---

## Phase 13: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T118 [P] Create Bicep infrastructure files in `infra/main.bicep`
- [ ] T119 [P] Create Cosmos DB Bicep module in `infra/modules/cosmos.bicep`
- [ ] T120 [P] Create Container Apps Bicep module in `infra/modules/container-apps.bicep`
- [ ] T121 Implement 90-day TTL policy enforcement for transcript data
- [ ] T122 [P] Add structured logging with correlation IDs across all agents
- [ ] T123 [P] Configure Application Insights integration in ServiceDefaults
- [ ] T124 Validate quickstart.md setup instructions
- [ ] T125 Performance optimization: pre-cache detailed views (FR-046)
- [ ] T126 Implement connection recovery with buffered audio sync
- [ ] T127 Final integration testing: 4-hour meeting stability (SC-003)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ──────────────────────────────┐
                                             │
Phase 2: Foundational ◄──────────────────────┘
    │
    ├──► Phase 3: US1 (Pre-Meeting) ──────► P1 MVP
    │        │
    ├──► Phase 4: US2 (Transcription) ────► P1 MVP (depends on US1 for meeting context)
    │        │
    ├──► Phase 5: US3 (Q&A) ──────────────► P1 MVP (depends on US2 for transcript)
    │        │
    ├──► Phase 6: US4 (Key Points) ───────► P1 MVP (depends on US2 for transcript)
    │        │
    ├──► Phase 7: US5 (Research) ─────────► P2 (depends on US2 for topics)
    │        │
    ├──► Phase 8: US6 (Summary) ──────────► P2 (depends on US1 agenda, US2 transcript)
    │        │
    ├──► Phase 9: US7 (Action Items) ─────► P2 (depends on US2 for transcript)
    │        │
    ├──► Phase 10: US8 (End Meeting) ─────► P2 (depends on US6, US7)
    │        │
    ├──► Phase 11: US9 (Commands) ────────► P2 (integrates all agents)
    │        │
    └──► Phase 12: US10 (Top Bar) ────────► P3 (UI only)
                                             │
Phase 13: Polish ◄───────────────────────────┘
```

### MVP Scope (Recommended)

**Minimum Viable Product** = Phases 1-6 (User Stories 1-4):
- Pre-meeting setup
- Real-time transcription with diarization
- Question answering
- Key points tracking

**Extended MVP** = Add Phases 7-10 (User Stories 5-8):
- Research agent
- Summary and agenda
- Action items
- End meeting summary

### Parallel Opportunities

**Phase 1 (Setup)**:
- T003, T004, T005 can run in parallel (different projects)
- T008, T009 can run in parallel (different csproj files)

**Phase 2 (Foundational)**:
- T011-T015 can run in parallel (different entity files)
- T023-T027 can run in parallel (different event DTOs)
- T035-T040 can run in parallel (different repository interfaces/implementations)

**User Stories (After Foundational)**:
- US1, US2 must be sequential (US2 needs meeting from US1)
- US3, US4 can start after US2 completes (both consume transcript)
- US5, US6, US7 can run in parallel after US2
- US8 depends on US6, US7 completion
- US9, US10 can run in parallel with any story after US2

---

## Task Summary

| Phase | Tasks | Parallel | Description |
|-------|-------|----------|-------------|
| 1. Setup | T001-T009 | 5 | Aspire project structure |
| 2. Foundational | T010-T043 | 18 | Core infrastructure |
| 3. US1 Pre-Meeting | T044-T052 | 3 | Meeting setup UI |
| 4. US2 Transcription | T053-T063 | 2 | Speech-to-text engine |
| 5. US3 Q&A | T064-T072 | 2 | Answer Agent |
| 6. US4 Key Points | T073-T079 | 0 | Key points tracking |
| 7. US5 Research | T080-T088 | 2 | Research Agent |
| 8. US6 Summary | T089-T094 | 0 | Agenda tracking |
| 9. US7 Actions | T095-T101 | 2 | Action items |
| 10. US8 End Meeting | T102-T108 | 3 | Summary generation |
| 11. US9 Commands | T109-T113 | 0 | Slash commands |
| 12. US10 Top Bar | T114-T117 | 0 | UI controls |
| 13. Polish | T118-T127 | 5 | Infrastructure & quality |

**Total Tasks**: 127  
**Parallelizable**: 42 (33%)  
**MVP Tasks (US1-4)**: 67  
**Full Implementation**: 127
