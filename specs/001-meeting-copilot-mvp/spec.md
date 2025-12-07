# Feature Specification: Real-time Meeting AI Assistant

**Feature Branch**: `001-meeting-copilot-mvp`  
**Created**: 2025-12-06  
**Status**: Draft  
**Input**: User description: "Meeting Copilot SaaS platform with real-time transcription, multi-agent AI assistance, and meeting intelligence"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pre-Meeting Setup (Priority: P1)

As a meeting participant, I want to set up my meeting context before it starts so that the AI assistant understands the meeting purpose and can provide relevant assistance.

**Why this priority**: This is the entry point for all meetings - without proper setup, the AI cannot provide contextual assistance. This establishes the foundation for all other features.

**Independent Test**: User can open the app, select a microphone, enter meeting details, and see the pre-meeting view ready for the meeting to start.

**Acceptance Scenarios**:

1. **Given** the app is loaded for the first time, **When** the page loads, **Then** the system prompts for microphone access permission
2. **Given** microphone permission is granted, **When** the user views the pre-meeting screen, **Then** a dropdown shows all available system microphones including the default
3. **Given** the pre-meeting screen is displayed, **When** the user enters a meeting title, **Then** the title is captured and displayed
4. **Given** the pre-meeting screen has a rich text area, **When** the user pastes markdown or rich text content (agenda, attendees, goals), **Then** the content is properly formatted and preserved
5. **Given** the user has clicked the attach button, **When** they select a file (image, document, or text), **Then** the attachment is uploaded and displayed in the pre-meeting context
6. **Given** meeting details are entered, **When** the user clicks "Start Meeting", **Then** the view transitions to the in-meeting screen

---

### User Story 2 - Real-time Speech Transcription with Speaker Identification (Priority: P1)

As a meeting participant, I want continuous real-time transcription with speaker identification so that I have an accurate record of who said what during the meeting.

**Why this priority**: This is the core engine that powers all other features. Without accurate transcription and speaker identification, no AI agent can function properly.

**Independent Test**: Start a meeting with multiple speakers, verify transcription appears in real-time with speaker labels, and verify the transcript is stored persistently.

**Acceptance Scenarios**:

1. **Given** the meeting has started, **When** someone speaks into the selected microphone, **Then** transcription begins within 500ms and displays in real-time
2. **Given** real-time transcription is active, **When** different speakers talk, **Then** the system identifies and labels each speaker with a unique identifier (Speaker 1, Speaker 2, etc.)
3. **Given** a speaker is identified as "Speaker 1", **When** another participant says "Thanks, John" and Speaker 1 responds, **Then** the system infers that Speaker 1 is "John" and updates all references
4. **Given** the user types `/speaker John` in the input area, **When** the current speaker is talking, **Then** the current speaker is labeled as "John" and all their utterances are updated
5. **Given** transcription is running for up to 4 hours, **When** the meeting continues, **Then** the system maintains stable transcription without degradation or disconnection
6. **Given** the microphone input level is too low, **When** speech is detected below threshold, **Then** a visual warning is displayed to the user
7. **Given** all transcribed content, **When** speakers are identified, **Then** each utterance is stored in a database with speaker ID, timestamp, and meeting ID

---

### User Story 3 - Immediate Question Answering (Priority: P1)

As a meeting participant, I want the AI to prioritize and answer immediate questions from the conversation so that I can respond quickly to stakeholders during the meeting.

**Why this priority**: The primary value proposition is providing real-time answers. This must be the highest priority processing task as it directly impacts the user's meeting performance.

**Independent Test**: During a meeting when a direct question is asked, verify the answer appears in the dedicated answer panel within seconds.

**Acceptance Scenarios**:

1. **Given** the meeting is active and transcription is running, **When** a direct question is detected in the conversation, **Then** the Answer Agent processes it with highest priority
2. **Given** a question has been identified, **When** the answer is generated, **Then** it appears in the Answer panel (top-left section) as a concise paragraph (200 words max)
3. **Given** an answer is displayed, **When** the user clicks "Detailed Answer", **Then** a new tab opens with the full detailed response
4. **Given** multiple questions are asked rapidly, **When** prioritizing responses, **Then** questions from higher-ranking participants (e.g., Director vs Engineer) are answered first
5. **Given** a question requires research, **When** processing begins, **Then** a placeholder or loading indicator shows while the research agent fetches additional context

---

### User Story 4 - Key Points and Highlights Tracking (Priority: P1)

As a meeting participant, I want to see the top 5 most important points from the conversation in real-time so that I never miss critical information.

**Why this priority**: Key points provide immediate situational awareness and help users track the most important discussion items without scrolling through transcripts.

**Independent Test**: During a meeting, verify that key points appear and reorder based on importance, capped at 5 items.

**Acceptance Scenarios**:

1. **Given** the meeting is in progress, **When** significant points are discussed, **Then** they appear in the Key Points panel (bottom-left section) as bullet points
2. **Given** key points are displayed, **When** new more important points emerge, **Then** the list reorders with most important at top
3. **Given** there are more than 5 key points, **When** the list updates, **Then** only the top 5 by importance are shown (lower priority items are removed from view)
4. **Given** a key point is displayed, **When** the user clicks "Follow Up", **Then** that point is added to the parking lot/action items
5. **Given** the key points section, **When** the user clicks "Go to Parking Lot", **Then** the view navigates to show all parking lot items

---

### User Story 5 - Background Research Agent (Priority: P2)

As a meeting participant, I want automatic and on-demand background research on discussion topics so that I have supporting information available when needed.

**Why this priority**: Research enhances meeting quality but is not blocking for core functionality. It operates in the background without interrupting the main flow.

**Independent Test**: Type `/research cloud migration strategies` during a meeting and verify research results appear in the Research panel.

**Acceptance Scenarios**:

1. **Given** the meeting is in progress, **When** a topic comes up in discussion, **Then** the Research Agent automatically begins researching relevant background information
2. **Given** the user types `/research {topic}` in the input area, **When** submitted, **Then** the Research Agent prioritizes that specific research request
3. **Given** research is being conducted, **When** results are ready, **Then** a subtle non-audible notification indicates availability and results appear in the Research panel (right pane)
4. **Given** research is complete, **When** displayed in the panel, **Then** results are concise with a "More" button for full details
5. **Given** web search capability is available, **When** researching a topic, **Then** the agent can access online sources for current information
6. **Given** research is running, **When** it operates, **Then** it runs in a separate thread/process to avoid blocking the main transcription agent

---

### User Story 6 - Meeting Summary and Agenda Tracking (Priority: P2)

As a meeting participant, I want a real-time summary of covered topics and next discussion items so that I can stay on track with the agenda.

**Why this priority**: Summary helps maintain meeting flow and ensures all agenda items are addressed. Critical for productive meetings but secondary to immediate Q&A.

**Independent Test**: Enter an agenda in pre-meeting, start the meeting, discuss items, and verify the summary panel updates showing covered topics and next items.

**Acceptance Scenarios**:

1. **Given** an agenda was provided in pre-meeting setup, **When** the meeting progresses, **Then** the Summary Agent tracks which items have been covered
2. **Given** the summary is displayed in the right panel, **When** a topic is discussed, **Then** it shows as covered with a concise summary
3. **Given** agenda items remain, **When** the current topic concludes, **Then** the next suggested topic is highlighted
4. **Given** the user types `/suggest`, **When** submitted, **Then** the Summary Agent immediately updates with the recommended next discussion point
5. **Given** there are remaining questions or unaddressed points, **When** viewing the summary, **Then** these are clearly indicated as pending

---

### User Story 7 - Action Items and Parking Lot (Priority: P2)

As a meeting participant, I want automatic extraction of action items and parking lot topics so that follow-ups are captured without manual note-taking.

**Why this priority**: Capturing action items ensures meeting outcomes are tracked. Important for post-meeting value but not critical during real-time assistance.

**Independent Test**: Discuss action items during a meeting (e.g., "John, can you follow up on the budget?") and verify they appear in the Action Items section with owner assigned.

**Acceptance Scenarios**:

1. **Given** the meeting is in progress, **When** someone mentions a todo, follow-up, or action item, **Then** the Action Agent extracts and records it
2. **Given** an action item is mentioned with a name, **When** extracted, **Then** the owner is automatically assigned based on context
3. **Given** items are added to parking lot from key points, **When** viewed, **Then** all parking lot items are accessible in a dedicated section
4. **Given** the meeting ends, **When** the summary is generated, **Then** all action items are listed with owners

---

### User Story 8 - End Meeting and Final Summary (Priority: P2)

As a meeting participant, I want a comprehensive meeting summary when the meeting ends so that I have all takeaways, action items, and transcripts in one place.

**Why this priority**: The end-of-meeting deliverable consolidates all value created during the meeting. Essential for post-meeting follow-up.

**Independent Test**: Click "End Meeting" and verify the summary view displays with all key takeaways, action items with owners, and access to full transcript.

**Acceptance Scenarios**:

1. **Given** the meeting is in progress, **When** the user clicks "End Meeting", **Then** the view transitions to the Meeting Summary screen
2. **Given** the summary screen is displayed, **When** viewing, **Then** it shows: Key Takeaways, Action Items with owners, and AI-generated summary
3. **Given** the summary screen, **When** the user clicks "Full Transcript", **Then** the complete labeled transcript with all speakers is accessible
4. **Given** the meeting has ended, **When** viewing action items, **Then** each item shows the owner and context from the discussion
5. **Given** the summary is displayed, **When** viewing key points transcript, **Then** only the significant moments are shown with speaker labels

---

### User Story 9 - In-Meeting Input and Slash Commands (Priority: P2)

As a meeting participant, I want a collapsed input area during meetings for quick commands and context additions so that I can interact with the system efficiently.

**Why this priority**: Input area enables user control during meetings but the system should function autonomously for most tasks.

**Independent Test**: During a meeting, verify the input area is collapsed at the bottom, accepts rich text, and processes slash commands correctly.

**Acceptance Scenarios**:

1. **Given** the meeting has started, **When** viewing the in-meeting screen, **Then** the input area is collapsed at the bottom of the screen
2. **Given** the collapsed input area, **When** the user pastes rich text or markdown, **Then** it is accepted and processed as high priority context
3. **Given** the user types `/research {topic}`, **When** submitted, **Then** the Research Agent is explicitly invoked for that topic
4. **Given** the user types `/speaker {name}`, **When** submitted, **Then** the current speaker is labeled with that name
5. **Given** the user types `/speaker` (blank), **When** submitted, **Then** the current speaker is labeled as "Myself"
6. **Given** the user types `/suggest`, **When** submitted, **Then** the Summary Agent prioritizes updating the next discussion item
7. **Given** any input is submitted, **When** processed, **Then** it is treated as high priority and acted upon immediately

---

### User Story 10 - Top Bar Meeting Controls (Priority: P3)

As a meeting participant, I want a clean top bar with meeting controls so that I can see meeting status and manage the meeting easily.

**Why this priority**: UI controls are supporting features that enhance usability but are not core to the AI functionality.

**Independent Test**: Verify the top bar displays meeting title, elapsed time, and the appropriate button (Start/End Meeting) based on meeting state.

**Acceptance Scenarios**:

1. **Given** the in-meeting screen is displayed, **When** viewing the top bar, **Then** it shows: Meeting Title, Current Speaker Name, Elapsed Time, and End Meeting button
2. **Given** the meeting is in progress, **When** time passes, **Then** the elapsed time updates in real-time (MM:SS or HH:MM:SS format)
3. **Given** a speaker is identified, **When** they are speaking, **Then** "Speaking: [Speaker Name]" appears in the top bar
4. **Given** the End Meeting button is visible, **When** clicked, **Then** it triggers the meeting end flow

---

### Edge Cases

- What happens when the microphone disconnects mid-meeting?
  - System should alert the user and attempt to reconnect; transcription should resume when reconnected
- What happens when internet connectivity is lost?
  - System buffers audio locally, shows degraded status indicator, retries connection, and syncs queued transcription when connection restores
- What happens when Azure Speech Service fails or degrades?
  - System buffers audio locally, displays visual degraded status indicator, automatically retries, and processes buffered audio when service recovers
- What happens when a speaker identification conflict occurs (same voice different contexts)?
  - System should prompt user to clarify via `/speaker` command
- What happens when the meeting exceeds 4 hours?
  - System should warn user at 3:45 and allow continuation with acknowledgment
- What happens when no speech is detected for extended periods?
  - System should indicate silence but maintain listening state
- What happens when multiple people speak simultaneously?
  - System should indicate overlapping speech and do best-effort transcription

## Requirements *(mandatory)*

### Functional Requirements

**Pre-Meeting:**
- **FR-001**: System MUST request microphone permissions on first page load
- **FR-002**: System MUST display all available system microphones in a dropdown selector
- **FR-003**: System MUST accept meeting title as text input
- **FR-004**: System MUST provide a rich text area supporting markdown and pasted rich text for meeting context (agenda, attendees, goals)
- **FR-005**: System MUST support file attachments with constraints: max 10MB per file, max 5 files per meeting, allowed types (.pdf, .docx, .xlsx, .pptx, .txt, .md, .png, .jpg, .jpeg); files stored in Azure Blob Storage
- **FR-006**: System MUST infer agenda items from the meeting description if not explicitly provided; if inference fails, display empty agenda with "Add agenda item" prompt
- **FR-007**: System MUST provide a "Start Meeting" button to transition to in-meeting view

**Real-Time Transcription:**
- **FR-008**: System MUST perform real-time speech-to-text transcription with latency under 500ms
- **FR-009**: System MUST support continuous transcription for meetings up to 4 hours
- **FR-010**: System MUST implement speaker diarization to distinguish between different speakers
- **FR-011**: System MUST assign unique identifiers to each detected speaker (Speaker 1, Speaker 2, etc.)
- **FR-012**: System MUST persist all transcribed utterances with speaker ID, timestamp, and meeting ID
- **FR-013**: System MUST infer speaker names from conversational context (e.g., when someone is addressed by name)
- **FR-014**: System MUST support manual speaker labeling via `/speaker {name}` command
- **FR-015**: System MUST display warning when microphone input level is too low
- **FR-016**: System MUST maintain a speakers table linked to each meeting
- **FR-062**: System MUST buffer audio locally when speech service connection fails or degrades
- **FR-063**: System MUST display visual degraded status indicator when operating in buffered mode
- **FR-064**: System MUST automatically retry speech service connection and process buffered audio when service recovers

**Multi-Agent Processing:**
- **FR-017**: System MUST run multiple AI agents concurrently without blocking each other
- **FR-018**: System MUST prioritize the Answer Agent (immediate questions) above all other agents
- **FR-019**: System MUST run Research Agent in a separate background thread/process
- **FR-020**: System MUST implement a Transcript Agent for continuous speech monitoring
- **FR-021**: System MUST implement a Research Agent for background topic research
- **FR-022**: System MUST implement a Summary Agent for agenda tracking and meeting summary
- **FR-023**: System MUST implement an Action Agent for extracting todos and action items

**Answer and Key Points:**
- **FR-024**: System MUST detect questions from the conversation and generate immediate answers
- **FR-025**: System MUST display answers in a dedicated Answer panel (max 200 words)
- **FR-026**: System MUST provide "Detailed Answer" button that opens full response in new tab
- **FR-027**: System MUST maintain a real-time list of top 5 key points sorted by importance
- **FR-028**: System MUST allow adding key points to parking lot via "Follow Up" button
- **FR-029**: System MUST prioritize questions using priority scores: Self=100, Executive/Director=80, Manager=60, Lead/Senior=50, Individual Contributor=40, External/Unknown=20
- **FR-068**: System MUST support `/speaker {name} {optional-role}` command syntax to assign both name and role (valid roles: executive, director, manager, lead, senior, contributor, external)
- **FR-069**: System MUST store participant role and company name alongside speaker identity

**Research and Summary:**
- **FR-030**: System MUST automatically research topics discussed in the meeting
- **FR-031**: System MUST support explicit research requests via `/research {topic}` command
- **FR-032**: System MUST display research results in dedicated Research panel with "More" button for details
- **FR-033**: System MUST provide subtle, non-audible notifications when research is ready
- **FR-034**: System MUST support web search capability for current information
- **FR-035**: System MUST track covered agenda items and suggest next discussion topics
- **FR-036**: System MUST support explicit summary update via `/suggest` command

**Action Items:**
- **FR-037**: System MUST extract action items, todos, and parking lot items from conversation
- **FR-038**: System MUST automatically assign owners to action items based on context
- **FR-039**: System MUST maintain a parking lot for follow-up items

**In-Meeting UI:**
- **FR-040**: System MUST display a collapsed input area at the bottom during meetings
- **FR-041**: System MUST accept rich text and markdown in the in-meeting input area
- **FR-042**: System MUST process all input submissions as high priority
- **FR-043**: System MUST support slash commands: `/research`, `/speaker`, `/suggest`
- **FR-044**: System MUST update all panels in real-time without requiring page refresh
- **FR-045**: System MUST display content concisely to minimize scrolling (fit within viewport)
- **FR-046**: System MUST pre-cache detailed views in memory for immediate display when requested

**Top Bar:**
- **FR-047**: System MUST display meeting title in top bar during meeting
- **FR-048**: System MUST display current speaker name in top bar
- **FR-049**: System MUST display elapsed meeting time (updating in real-time)
- **FR-050**: System MUST provide End Meeting button that transitions to summary view

**Meeting End:**
- **FR-051**: System MUST generate comprehensive meeting summary including: key takeaways, action items with owners, AI summary
- **FR-052**: System MUST provide access to full labeled transcript
- **FR-053**: System MUST provide key points transcript with only significant moments

**Data and Memory:**
- **FR-054**: System MUST store all transcripts in a vector store for RAG retrieval
- **FR-055**: System MUST use RAG to manage context within token limits
- **FR-056**: System MUST support automatic and on-demand memory retrieval
- **FR-057**: System MUST process and save data as quickly as possible
- **FR-060**: System MUST design data schema with tenant isolation fields (user_id) for future multi-tenant support
- **FR-061**: System MUST implement agent memory service pattern for AI agent state and context persistence
- **FR-065**: System MUST retain full transcripts for 90 days from meeting date
- **FR-066**: System MUST retain meeting summaries and action items indefinitely after transcript expiration
- **FR-067**: System MUST automatically archive or delete expired transcript data per retention policy
- **FR-070**: System MUST store meeting language preference (default: "en-US") to support future multi-language expansion

**Constraints:**
- **FR-058**: System MUST NOT generate or play any audio output
- **FR-059**: System MUST be listen-only mode during meetings

### Key Entities

- **Meeting**: Represents a single meeting session with title, description, agenda, start time, end time, and status
- **Speaker**: Represents a participant in a meeting with unique ID, display name, role (optional), company name (optional), is_self flag, and associated meeting
- **Utterance**: A single transcribed speech segment with speaker reference, timestamp, text content, and meeting reference
- **AgendaItem**: A topic from the meeting agenda with status (pending, in-progress, completed) and summary
- **KeyPoint**: An important discussion point with priority score, text, speaker reference, and parking lot flag
- **ActionItem**: A todo or follow-up task with description, owner (speaker reference), and status
- **ResearchResult**: Background research output with topic, content, source references, and timestamp
- **Attachment**: Pre-meeting file attachment with type, content/path, and meeting reference

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete pre-meeting setup (title, context, microphone selection) in under 2 minutes
- **SC-002**: Real-time transcription begins within 500ms of speech detection
- **SC-003**: System maintains stable transcription for meetings up to 4 hours without degradation
- **SC-004**: Speaker changes are detected and labeled within 1 second of speaker transition
- **SC-005**: Answers to detected questions appear in the Answer panel within 5 seconds
- **SC-006**: Key points list updates within 3 seconds of significant discussion points
- **SC-007**: Research results are available within 30 seconds of topic identification or explicit request
- **SC-008**: All UI panels update in real-time without requiring user refresh
- **SC-009**: Content fits within viewport without requiring scrolling for primary panels
- **SC-010**: End-of-meeting summary is generated within 10 seconds of clicking End Meeting
- **SC-011**: System correctly identifies speaker names through inference in 80% of cases where names are mentioned
- **SC-012**: Action items are extracted with correct owner assignment in 90% of clear assignment cases
- **SC-013**: System handles 4+ concurrent speakers with distinguishable diarization
- **SC-014**: Detailed information displays immediately (under 500ms) when "More" buttons are clicked due to background caching

## Clarifications

### Session 2025-12-06

- Q: How should the system handle data privacy and multi-tenancy? → A: Single-user local mode for MVP, but design database schema with multi-tenant isolation in mind for future SaaS support
- Q: What should happen when Azure Speech Service connection fails during a meeting? → A: Buffer audio locally, show degraded status indicator, retry and sync when connection restores
- Q: What is the data retention policy for meeting transcripts? → A: 90-day full transcript retention, then only summaries and action items retained indefinitely
- Q: How should participant importance/ranking be determined for question prioritization? → A: User (self) is always highest priority; others can be tagged with role via `/speaker {name} {optional-role}`; store role and company name in participants table
- Q: What is the language support strategy? → A: English only for MVP, but design with multi-language ready architecture for future language expansion

## Assumptions

- Users have a stable internet connection for Azure Speech Services
- Users grant microphone permissions when prompted
- Meeting participants speak in English (primary language support for MVP)
- Azure Speech Services provide reliable real-time diarization
- Users have modern browsers supporting Web Audio API
- Meeting content does not require specialized domain-specific transcription models
- MVP operates in single-user mode without authentication
- Database schema designed for future multi-tenant expansion with user isolation
- Agent memory service pattern follows Cosmos DB-based approach (reference: james-tn/agent-memory)
- Architecture designed for multi-language expansion; language-specific components isolated for future localization
