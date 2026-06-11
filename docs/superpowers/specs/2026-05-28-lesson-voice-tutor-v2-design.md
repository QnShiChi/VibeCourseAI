# Lesson Voice Tutor V2 Design

Date: 2026-05-28
Status: Draft for review

## Goal

Refine the existing lesson voice tutor into a voice-first experience that feels immediate during learning:

- reduce perceived latency after the learner finishes speaking
- start assistant speech as soon as the first answer segment is ready
- remove assistant answer text from the learner UI
- replace the large inline tutor panel with a compact floating microphone control

This is a V2 refinement of the existing lesson voice tutor, not a greenfield redesign.

## Product Decisions

- Interaction remains one-shot recording per turn: learner taps mic, records, then stops.
- Video pauses when the learner starts asking.
- Video stays paused while the assistant is answering.
- After the assistant finishes speaking, the learner chooses `Hoi tiep` or `Tiep tuc hoc`.
- The learner UI does not render the assistant answer as text.
- The tutor control is a floating mic button with a short label, visible on the video without consuming page layout space.

## Non-Goals

- Always-listening wake word behavior
- Browser-native speech recognition
- Full duplex conversation where learner and assistant speak over each other
- True byte-level live audio streaming from the TTS engine
- Replacing the existing provider stack for STT, LLM, or TTS

## User Experience

### Primary Control

The lesson page will replace the current tutor panel with a compact floating control anchored over the video player.

Desktop behavior:

- circular mic button, visually prominent but compact
- short pill label adjacent to the mic button
- default placement: bottom-right area of the video frame
- spacing avoids overlap with native video controls

Mobile behavior:

- same floating mic button pattern
- shorter label text
- label may visually compress sooner than desktop to preserve video space

### Visual States

- `idle`
  - mic button enabled
  - label: `Hoi ngay`
- `recording`
  - mic button highlighted or pulsing
  - label: `Dang nghe`
- `thinking`
  - mic button disabled
  - label: `Dang tra loi`
- `speaking`
  - mic button disabled
  - label: `Dang tra loi`
- `awaitingDecision`
  - mic button visible
  - compact actions appear near the control:
    - `Hoi tiep`
    - `Tiep tuc hoc`
- `error`
  - mic button remains accessible
  - show a short inline tooltip/toast style error
  - do not expand into a large panel

### Learner Flow

1. Learner taps the floating mic control.
2. Frontend pauses the lesson video and stores the playback timestamp.
3. Frontend records one audio turn.
4. Learner stops recording.
5. Frontend uploads the recorded audio to the backend through the tutor hub.
6. Backend performs STT.
7. Backend streams LLM output incrementally.
8. Backend segments the answer into short speech-ready chunks.
9. Backend synthesizes and emits each chunk as soon as it is ready.
10. Frontend plays each returned audio segment in order.
11. When all segments finish, frontend shows `Hoi tiep` and `Tiep tuc hoc`.
12. If the learner chooses `Tiep tuc hoc`, video resumes from the saved timestamp.
13. If the learner chooses `Hoi tiep`, a new recorded turn starts within the same lesson session.

## Backend Architecture

### Current V1 Problem

The existing pipeline is strictly sequential:

`STT -> full LLM answer -> full TTS segmentation -> return all segments`

This creates avoidable dead time because the learner hears nothing until the complete answer has already been generated and synthesized.

### V2 Target Pipeline

The backend should move to an incremental pipeline:

`STT -> LLM token stream -> answer segmenter -> per-segment TTS -> SignalR segment emission`

This is the key architectural change in V2.

### Components

- `LessonVoiceTutorHub`
  - remains the realtime transport
  - receives the recorded learner audio
  - emits tutor lifecycle events and speech segment events

- `ILessonVoiceTutorService`
  - still owns one turn orchestration
  - now coordinates a streaming answer path instead of waiting for a full answer result

- `ILessonTutorResponseStreamService`
  - new interface
  - streams assistant text progressively from the LLM provider

- `ILessonTutorSegmenter`
  - new interface
  - converts token flow into speech-ready text segments

- `ILessonTutorSpeechService`
  - reused, but extended for immediate per-segment synthesis

- `ITranscriptionService`
  - unchanged in role
  - still transcribes the learner’s audio after recording stops

### Streaming Behavior

The backend should:

1. transcribe the learner question
2. begin LLM streaming
3. accumulate tokens into a buffer
4. flush a segment when a stable boundary is reached
5. synthesize that segment immediately
6. send the resulting audio segment event to the client
7. continue until the full answer is complete

Stable boundaries:

- sentence-ending punctuation
- long pause punctuation such as commas in a sufficiently long segment
- newline boundaries
- maximum character threshold when no punctuation arrives

Recommended segment target:

- roughly 120-180 characters
- biased toward natural sentence boundaries over hard length cuts

## SignalR Contract

### Client to Server

Existing shape remains:

- `CompleteTurn(sessionId, playbackTimeSeconds, audioBytes)`

The turn is still uploaded after recording stops. V2 does not require live mic upload.

### Server to Client

The contract should move to voice-focused events:

- `TranscriptionStarted(sessionId)`
- `TranscriptionCompleted()`
- `AssistantSpeechSegmentReady(sequenceIndex, audioUrl, durationSeconds)`
- `AssistantSpeechCompleted(sessionId)`
- `AwaitingFollowUpDecision(sessionId)`
- `TutorFailed(message)`

Removed from learner UI concerns:

- no `AnswerCompleted(text, sourceType)` event for rendering
- no answer text payload in speech segment events

The backend may still persist text internally for history and diagnostics, but frontend should not display it.

## Frontend Architecture

### New Component

Introduce a focused floating control component, for example:

- `LessonVoiceTutorFab`

Responsibilities:

- render mic button
- render short label
- render minimal decision actions after assistant speech
- reflect tutor state visually

This component should live in the video area, not below the player as a block section.

### Existing Hook

`useLessonVoiceTutor` should remain the main stateful integration point, but simplify its exposed data:

- keep:
  - `state`
  - `errorMessage`
  - action handlers
  - internal audio queue handling
- remove UI-facing text state:
  - `transcriptText`
  - `answerText`

### Audio Playback

Frontend continues to use queued segment playback:

- maintain a queue ordered by `sequenceIndex`
- start playback as soon as the first segment arrives
- continue playback segment-by-segment
- only expose follow-up actions after:
  - `AssistantSpeechCompleted` has been received
  - the local playback queue is empty

The video must not auto-resume after assistant speech.

## Data and Persistence

Existing lesson voice tutor persistence can remain:

- session
- turn
- message history

The backend should continue storing the textual answer internally even though the learner UI no longer renders it. This preserves:

- auditability
- debugging
- future analytics
- potential future transcript/history screens

No new persistence model is required for V2.

## Error Handling

### Learner-Facing

Errors should remain short and operational:

- could not start recording
- could not send the voice question
- tutor could not answer right now

Do not expand errors into a large tutor panel.

### Backend

If a segment fails during TTS:

- fail the current turn cleanly
- emit `TutorFailed`
- keep the lesson paused so the learner can choose to retry or continue

If STT or LLM fails:

- fail the turn
- do not leave the client in a stuck `thinking` state

## Performance Expectations

V2 should improve perceived responsiveness by shifting the first audible response earlier.

The expected latency profile becomes:

- one wait for STT completion
- then progressive answer generation and speech playback

Instead of:

- one wait for STT
- another wait for full answer generation
- another wait for full TTS generation

This is not true end-to-end live audio streaming, but it removes the largest avoidable waiting period from the learner experience.

## Migration Strategy

Implement V2 incrementally:

1. replace the large tutor panel with the floating mic control
2. stop rendering tutor answer text in the frontend
3. refactor answer generation from full-response to streaming-response
4. add the answer segmenter
5. emit and play TTS segments as they are generated
6. preserve follow-up and resume logic from V1

This avoids throwing away the working lesson session model and existing provider integrations.

## Testing Strategy

### Backend

- unit test the segmenter against:
  - punctuation boundaries
  - long unpunctuated text
  - multi-segment answers
- integration test that a turn emits:
  - `TranscriptionStarted`
  - one or more `AssistantSpeechSegmentReady`
  - `AssistantSpeechCompleted`
  - `AwaitingFollowUpDecision`
- verify that no full-answer wait is required before the first segment event

### Frontend

- verify the floating mic control renders in the video area
- verify state labels change correctly
- verify answer text is not rendered
- verify audio segments are queued and played in order
- verify follow-up actions appear only after speech playback completes
- verify video resumes only when the learner chooses `Tiep tuc hoc`

## Open Implementation Constraints

V2 assumes the chosen LLM provider supports incremental streaming suitable for backend orchestration.

If the current provider path cannot provide a stable streaming interface, the implementation must still preserve the V2 UX contract by using the nearest available backend streaming mechanism without changing the learner-facing behavior.

## Success Criteria

V2 is successful when:

- the learner sees only a compact floating mic control instead of a large tutor panel
- the learner does not see assistant answer text in the lesson UI
- the assistant begins audible reply before the full answer has completed end-to-end processing
- the lesson remains paused while the assistant speaks
- the learner can cleanly choose `Hoi tiep` or `Tiep tuc hoc` after each response
