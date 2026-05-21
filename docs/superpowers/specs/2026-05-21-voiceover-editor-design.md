# Voiceover Editor Design

## Overview

The lesson AI content flow already supports:

- teaching script editing as plain text
- structured slide preview and editing

The remaining weak spot in the lesson content editor is `voiceoverPlanJson`, which is still exposed as raw JSON in both preview and edit flows. This creates the same usability problem that the slide editor recently solved: admins can technically edit the content, but the format is hard to read, easy to break, and not aligned with a production-oriented lesson authoring workflow.

This feature introduces a structured `Voiceover Editor` and `Voiceover Preview` while keeping the existing backend storage model unchanged. The system will continue storing the voiceover data in `voiceoverPlanJson`, but the frontend will parse, validate, and edit it through structured fields.

## Goals

- Replace raw `voiceoverPlanJson` display with a readable preview.
- Replace raw `voiceoverPlanJson` textarea editing with a structured form.
- Preserve the current lesson generated content API contract and database schema.
- Validate `voiceoverPlanJson` consistently before saving generated lesson content.
- Support both legacy PascalCase JSON keys and new camelCase keys to avoid breaking existing lesson data.

## Non-Goals

- No text-to-speech generation.
- No audio file preview or playback.
- No detailed narration editor per paragraph or per slide.
- No new database tables or separate voiceover entity/API.

## Existing Context

Current lesson generated content consists of:

- `teachingScript`
- `slideOutlineJson`
- `voiceoverPlanJson`

`slideOutlineJson` now has a structured editor and preview. `voiceoverPlanJson` is still rendered as raw JSON in:

- `frontend/src/components/course/LessonContentEditor.jsx`
- `frontend/src/components/course/LessonContentPreview.jsx`

The backend update endpoint already accepts `VoiceoverPlanJson` as a string through:

- `backend/CourseVideo.API/DTOs/Lessons/UpdateLessonGeneratedContentRequest.cs`

The backend also now validates slide outline JSON before saving generated lesson content. The voiceover plan should follow the same pattern.

## Voiceover Data Shape

Version one of the voiceover editor will support exactly five fields:

- `estimatedDurationMinutes`
- `tone`
- `pacing`
- `targetAudience`
- `pronunciationNotes`

The system must accept both of these representations when reading existing data:

Legacy PascalCase:

```json
{
  "EstimatedDurationMinutes": 8,
  "Tone": "Trang trọng, rõ ràng",
  "Pacing": "Vừa phải",
  "TargetAudience": "Sinh viên đại học",
  "PronunciationNotes": "Phát âm chuẩn các thuật ngữ"
}
```

New camelCase:

```json
{
  "estimatedDurationMinutes": 8,
  "tone": "Trang trọng, rõ ràng",
  "pacing": "Vừa phải",
  "targetAudience": "Sinh viên đại học",
  "pronunciationNotes": "Phát âm chuẩn các thuật ngữ"
}
```

When the frontend saves edited data, it should serialize into a single normalized format. The preferred normalized output is camelCase JSON because it aligns with the frontend helper conventions introduced for slide editing.

## UX Design

### Preview

The lesson AI content preview will render voiceover data as a structured card instead of raw JSON.

Displayed fields:

- `Thời lượng dự kiến`
- `Giọng điệu`
- `Nhịp đọc`
- `Đối tượng nghe`
- `Lưu ý phát âm`

Behavior:

- If `voiceoverPlanJson` is empty, show `Chưa có voiceover plan.`
- If parsing fails or required fields are invalid, show `Voiceover plan JSON hiện tại không hợp lệ.`
- If parsing succeeds, render all five fields in a readable admin-oriented layout.

### Editor

Inside `Chỉnh nội dung AI`, replace the raw textarea for voiceover JSON with a structured form.

Fields:

- `Thời lượng dự kiến (phút)`
- `Giọng điệu`
- `Nhịp đọc`
- `Đối tượng nghe`
- `Lưu ý phát âm`

Behavior:

- Form loads from parsed `voiceoverPlanJson`.
- Editing any field updates local structured state and serializes back into `voiceoverPlanJson`.
- Invalid or incomplete form data should show inline validation feedback.
- If the existing JSON is malformed, the editor should fall back to a safe default object and surface the validation/parsing error clearly.

## Validation Rules

### Frontend

The frontend voiceover helper should validate that:

- all five fields are present
- `estimatedDurationMinutes` is a finite positive number
- `tone` is non-empty
- `pacing` is non-empty
- `targetAudience` is non-empty
- `pronunciationNotes` is non-empty

If validation fails during editing or serialization:

- the UI should show a clear validation message
- save should still rely on the serialized `generatedContentForm`, so the structured editor must prevent invalid serialization from silently replacing valid data

### Backend

The backend should validate `VoiceoverPlanJson` when saving lesson generated content, similar to slide outline validation.

The validation should reject:

- malformed JSON
- non-object JSON values
- missing required fields
- invalid `estimatedDurationMinutes`
- empty string values for the four text fields

On validation failure, the API should return `400 Bad Request` with a user-facing error message, following the same pattern used for slide outline validation.

## Technical Design

### Frontend Helpers

Add a new helper module for voiceover parsing and serialization, similar in role to `slideOutline.js`.

Recommended responsibilities:

- parse `voiceoverPlanJson`
- map PascalCase and camelCase keys into one normalized JS object
- validate structured voiceover data
- serialize normalized voiceover data back to JSON

Expected outputs:

- parsed object for preview/editor
- validation errors for malformed or incomplete data

### Frontend Components

Add a dedicated `VoiceoverEditor` component.

Responsibilities:

- render the five structured fields
- manage field changes through parent state callbacks
- display inline validation errors

Update `LessonContentEditor.jsx`:

- replace the raw `Voiceover plan JSON` textarea with `VoiceoverEditor`
- keep the same external `onChange(field, value)` contract by serializing back into `voiceoverPlanJson`

Update `LessonContentPreview.jsx`:

- replace the raw voiceover JSON `pre` block with a structured preview card

### Backend Validation

Add a backend helper similar to `SlideOutlineValidation`.

Responsibilities:

- parse `VoiceoverPlanJson`
- ensure it is an object with valid required fields
- throw `InvalidOperationException` with a clear message on invalid input

Update `LessonService.UpdateGeneratedContentAsync`:

- validate `VoiceoverPlanJson` before persisting it

No DTO or schema change is required in version one.

## Compatibility Strategy

Existing lesson content in the database may contain:

- PascalCase keys generated by backend serializers
- manually edited JSON that may already use camelCase

To avoid migration work:

- the frontend parser must accept both formats
- backend validation should also accept both formats if practical

This keeps the feature backward-compatible without rewriting stored lesson content.

## Error Handling

User-visible errors should be concise and specific.

Preview:

- `Voiceover plan JSON hiện tại không hợp lệ.`

Editor:

- `Thời lượng dự kiến phải lớn hơn 0.`
- `Giọng điệu là bắt buộc.`
- `Nhịp đọc là bắt buộc.`
- `Đối tượng nghe là bắt buộc.`
- `Lưu ý phát âm là bắt buộc.`

Backend:

- `Voiceover plan JSON không hợp lệ.`
- `Voiceover plan phải có estimatedDurationMinutes, tone, pacing, targetAudience và pronunciationNotes hợp lệ.`

## Testing Strategy

Frontend tests:

- helper parses camelCase JSON
- helper parses PascalCase JSON
- helper rejects malformed JSON
- helper rejects missing required fields
- editor serializes structured field updates correctly
- preview renders structured voiceover fields correctly
- course page save flow still sends normalized `voiceoverPlanJson`

Backend checks:

- valid voiceover JSON is accepted
- malformed JSON returns `400`
- missing fields return `400`

Because the current workspace has known frontend dependency and backend test-environment friction, verification may continue using:

- focused frontend tests in the writable verify copy
- backend build
- live API request validation against the running backend container

## Rollout Impact

This feature improves authoring UX without changing storage or public data contracts. It prepares the lesson content model for the next likely step, which is TTS/audio generation, while avoiding unnecessary schema work now.
