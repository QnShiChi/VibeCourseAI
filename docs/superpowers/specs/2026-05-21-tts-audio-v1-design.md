# TTS Audio V1 Design

## Overview

The lesson authoring flow now supports:

- generated and editable teaching scripts
- structured slide preview and editing
- structured voiceover plan preview and editing

The next product step is to turn lesson content into spoken audio. The core requirement is not just generating speech from a lesson in bulk, but ensuring the spoken narration remains aligned to slide boundaries so that later video rendering can keep narration synchronized with the correct slide.

Version one will generate one final audio file per lesson, but the internal pipeline will work per slide:

1. generate narration text for each slide
2. synthesize one audio segment per slide using OpenAI TTS
3. concatenate the slide segments into one final lesson audio file

This preserves slide alignment while still giving the lesson a single `audioUrl`.

## Goals

- Generate one final audio file per lesson.
- Keep narration aligned to individual slides.
- Support both lesson-level generation and whole-course background generation.
- Reuse the existing background job pattern already used for lesson content generation.
- Store enough segment metadata to support future video rendering.
- Use OpenAI TTS for version one.

## Non-Goals

- No video rendering in this version.
- No custom voice cloning.
- No multi-speaker conversational audio.
- No learner-facing waveform/timeline editor.
- No manual slide-by-slide narration editor in version one.

## Product Requirements

### Audio Output Model

Each lesson should end up with:

- one final lesson audio file
- one `audioUrl` stored on the lesson
- per-slide segment metadata retained for future slide-to-audio synchronization work

### Slide Alignment

The spoken output must follow the slide structure closely enough that later video rendering can switch slides in sync with the narration.

This means:

- the system must not generate one undifferentiated audio blob from the whole teaching script
- each slide must have its own narration segment
- the final lesson audio is produced by concatenating slide segments in order

### Narration Source Strategy

Using `speakerNotes` alone is too risky because it may be too short, uneven, or incomplete. Using `teachingScript` alone is also risky because the system would have to guess slide boundaries.

Version one should therefore generate a derived `narration per slide` artifact using:

- `speakerNotes` as the preferred slide-local signal
- `teachingScript` as the fallback/context signal
- `voiceoverPlanJson` to influence tone and pacing guidance

The derived narration should be generated before TTS synthesis.

## Architecture

### Pipeline

For each lesson:

1. Parse and validate lesson inputs:
   - `teachingScript`
   - `slideOutlineJson`
   - `voiceoverPlanJson`
2. Generate `narration per slide`
3. Generate one TTS audio segment per slide using OpenAI TTS
4. Concatenate slide segments into one final lesson audio file
5. Save:
   - lesson `audioUrl`
   - lesson duration if available
   - generation status
   - segment metadata for future use

### Generation Modes

Version one supports both:

- lesson-level generation
- course-level background generation

Lesson-level generation is for testing and targeted retries.
Course-level generation is for operational use and should follow the existing background job model with progress polling.

### Provider Choice

Version one will target OpenAI TTS rather than VibeVoice.

Reasons:

- simpler integration into the current backend/API flow
- less infrastructure burden than self-hosted model serving
- better fit for single-speaker lesson narration
- lower product risk for an initial production-oriented implementation

## Data Model Changes

The current `Lesson` model already has:

- `AudioUrl`
- `Duration`

Version one needs additional storage for slide-level audio metadata. The simplest path is to add one new lesson field:

- `AudioSegmentsJson`

Suggested structure:

```json
[
  {
    "slideNumber": 1,
    "title": "Giới thiệu bài học",
    "narrationText": "Chào mừng các bạn...",
    "audioUrl": "/storage/audio/lesson-1-slide-1.mp3",
    "durationSeconds": 18.4
  }
]
```

This keeps version one simple while preserving the information needed for future video sync.

## Lesson Status Model

Version one should introduce audio-generation status separate from lesson content-generation status.

Recommended lesson audio statuses:

- `NotGenerated`
- `Pending`
- `GeneratingNarration`
- `GeneratingAudio`
- `Completed`
- `Failed`

These can be stored either directly on `Lesson` or in job state plus persisted summary fields, depending on how tightly the backend wants to couple audio state to lesson data.

Because lesson content generation already uses job records, audio generation should reuse that pattern rather than inventing a separate tracking mechanism.

## Background Jobs

### Course-Level Job

Whole-course generation should:

- enqueue one job for the course
- process lessons one by one
- expose:
  - total lessons
  - processed lessons
  - failed lessons
  - current phase
  - current lesson title if available

### Lesson-Level Job

Single-lesson generation should:

- create a job scoped to one lesson
- support retry for failed lessons
- reuse the same job detail endpoint shape where practical

### Failure Handling

Common failure points:

- narration generation failure
- OpenAI TTS API failure
- audio concatenation failure
- file write/storage failure

The system should:

- mark only the affected lesson as failed
- allow targeted regenerate for that lesson
- avoid invalidating already-completed lessons during a course-wide run

## Narration Generation Design

### Purpose

This step converts lesson content into stable, slide-aligned narration text before audio synthesis.

### Inputs

- `teachingScript`
- parsed slide list from `slideOutlineJson`
- parsed voiceover plan from `voiceoverPlanJson`

### Output

A structured list of narration items, one per slide:

```json
[
  {
    "slideNumber": 1,
    "title": "Giới thiệu bài học",
    "narrationText": "Chào mừng các bạn đến với bài học..."
  }
]
```

### Behavior

The narration generator should:

- preserve slide order
- keep each narration segment focused on the slide it belongs to
- prefer `speakerNotes`
- expand weak or too-short notes using teaching-script context
- avoid repeating the exact same ideas across adjacent slides
- produce text suitable for direct TTS

### Validation

Generated narration should be rejected if:

- any slide is missing narration text
- narration items do not match slide count
- slide order or slide numbers are inconsistent

## OpenAI TTS Integration

### Version One Scope

Use OpenAI TTS to generate one segment per slide from narration text.

Version one should standardize on:

- one default TTS model
- one default voice
- one audio format for storage, preferably `mp3`

The exact model/voice can remain configurable through application settings.

### Why OpenAI TTS Fits

OpenAI’s TTS offering currently provides:

- API-managed speech generation
- steerable speaking style
- production-friendly API workflow
- documented rate limits and pricing

This is a better fit than VibeVoice for an initial production-oriented release where reliability and integration simplicity matter more than long-form multi-speaker expressiveness.

## Audio Concatenation

Version one must join per-slide audio segments into a single final lesson file.

The implementation should:

- preserve slide order
- tolerate variable segment lengths
- output one final file path for `lesson.AudioUrl`

This likely belongs in the `ai-worker`, since file-heavy media processing is a better fit there than in the web API process.

## Service Boundaries

### Backend API

Responsibilities:

- start jobs
- expose job status
- validate lesson readiness for audio generation
- persist lesson-level metadata and URLs

### AI Worker

Responsibilities:

- narration generation orchestration or delegated execution
- OpenAI TTS calls
- segment file creation
- audio concatenation
- writing files into `storage/audio`

### Storage

Version one should store:

- per-slide segment files in `storage/audio`
- final lesson audio files in `storage/audio`

The URL strategy should match how the app already serves storage-backed media.

## UI Design

### Lesson-Level Controls

Each lesson should gain:

- `Generate audio`
- `Generate lại audio` when failed or when audio already exists and replacement is allowed
- status badge for audio generation
- audio preview player when `audioUrl` exists

### Course-Level Controls

The course page should gain:

- `Generate audio khóa học`
- progress display for course-wide audio jobs
- feedback on failed lessons

### Progress Messaging

Progress should communicate the pipeline phase, for example:

- `Đang tạo narration theo slide`
- `Đang sinh audio cho slide 3/8`
- `Đang ghép audio lesson`

This is more informative than a generic “processing” label.

## Validation and Preconditions

A lesson should only be eligible for audio generation if:

- it has non-empty `teachingScript`
- it has valid `slideOutlineJson`
- it has valid `voiceoverPlanJson`

If prerequisites are missing, the API should reject the generation request with a clear message.

## Testing Strategy

### Backend

Need coverage for:

- lesson readiness validation
- narration-per-slide response validation
- TTS generation orchestration
- job lifecycle updates
- failed lesson retry behavior

### Worker

Need coverage for:

- segment generation flow
- audio file naming/path generation
- concatenation command/service behavior

### Frontend

Need coverage for:

- lesson-level generate audio button state
- course-level job progress UI
- failed lesson regenerate action
- audio preview rendering when `audioUrl` exists

### Runtime Verification

Need smoke verification for:

- one lesson generates audio successfully
- one course job reports progress
- failed lesson can be regenerated
- final file is written to `storage/audio`

## Out-of-Scope Follow-up

The next likely step after this feature is:

- video rendering from slides + synced audio

Because version one stores per-slide narration and per-slide audio metadata, it creates the right foundation for that future step without requiring video rendering now.
