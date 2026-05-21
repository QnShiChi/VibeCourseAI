# Slide Editor Design

Date: 2026-05-21
Status: Draft for review

## Goal

Add an admin-facing Slide Editor for lesson AI content so admins can edit slide data as structured form fields instead of manually editing raw `slideOutlineJson`.

The first version only supports the current slide fields already produced by lesson-content generation:
- `title`
- `bulletPoints`
- `speakerNotes`

The design must preserve compatibility with the existing backend storage model, which currently stores slides as serialized JSON in `Lessons.SlideOutlineJson`.

## Non-Goals

This version does not:
- introduce a new `Slides` database table
- add slide-level backend APIs
- add image prompts, layout metadata, animation notes, or per-slide duration
- export PowerPoint, render video, or generate voiceover assets
- redesign the lesson generation pipeline

## Current State

The current admin workflow already supports:
- generated lesson script
- generated `slideOutlineJson`
- generated `voiceoverPlanJson`
- editing generated lesson content through a raw JSON textarea

This is functional but not usable for editorial work. Admins currently need to inspect and modify slide data as raw JSON, which is slow, error-prone, and not appropriate for non-technical content editing.

## Proposed Approach

Use a frontend-only structured editor layered on top of the existing `slideOutlineJson` string.

The editor will:
1. parse `slideOutlineJson` into an in-memory slide array
2. render a form-based slide editor for each slide
3. validate and serialize the edited array back into `slideOutlineJson`
4. save through the existing lesson generated-content update endpoint

This keeps backend scope minimal while delivering the editorial UX we need now.

## Why This Approach

### Option A: Keep raw JSON and add a nicer preview
- Lowest implementation cost
- Does not solve the core editing problem
- Still forces admins to edit JSON directly

### Option B: Structured Slide Editor over existing JSON storage
- Best short-term balance of UX and engineering scope
- Preserves current backend contract
- Easy to extend later into richer slide metadata

### Option C: Full slide entity model with dedicated backend CRUD
- Strong long-term domain model
- Too much scope for this stage
- Unnecessary before validating the editorial workflow

Recommendation: Option B.

## User Experience

### Entry Point

The existing lesson AI content area on `/admin/courses/:courseId` remains the editing surface.

When an admin opens lesson AI content:
- `Script` remains visible as before
- `Slides` becomes a structured editor instead of a raw JSON block
- `Voiceover` can stay as-is for now

### Slide Preview Mode

The lesson content preview should display slides as readable cards instead of plain JSON.

Each slide card shows:
- slide number
- title
- bullet points as a list
- speaker notes as a text block

### Slide Edit Mode

When editing generated lesson content, the `Slides` section shows a form-based list of slides.

Each slide supports:
- `Slide number` display only, derived from array order
- `Title` input
- `Bullet points` as repeatable line items
- `Speaker notes` textarea

Editor actions:
- `Thêm slide`
- `Xóa slide`
- `Di chuyển lên`
- `Di chuyển xuống`
- `Thêm bullet point`
- `Xóa bullet point`

Optional recovery action in this version:
- `Khôi phục từ dữ liệu AI hiện có`
This simply resets the in-memory editor state from the last fetched `slideOutlineJson`.

## Data Model

### Stored Format

Stored backend format remains unchanged:
- `Lesson.SlideOutlineJson` continues to store serialized JSON array data

Expected logical shape in frontend:

```json
[
  {
    "slideNumber": 1,
    "title": "...",
    "bulletPoints": ["..."],
    "speakerNotes": "..."
  }
]
```

### Frontend Form Model

Introduce a frontend slide form model:
- `slideNumber: number`
- `title: string`
- `bulletPoints: string[]`
- `speakerNotes: string`

`slideNumber` is not directly editable in the first version. It is recomputed from list order before save.

## Validation Rules

Before save, frontend validates:
- at least one slide exists
- every slide has non-empty `title`
- every slide has at least one non-empty bullet point
- every slide has non-empty `speakerNotes`

Normalization rules:
- trim whitespace on all text fields
- remove empty bullet point rows before save
- rewrite `slideNumber` sequentially starting from 1

If validation fails, block save and show a clear inline error message.

## Component Design

### `LessonContentPreview`

Change `Slides` rendering from raw JSON text to parsed slide cards.

Behavior:
- if JSON is valid and slides exist, render structured preview
- if JSON is empty, show empty-state text
- if JSON is invalid, show fallback warning and optionally render raw content for debugging

### `LessonContentEditor`

Refactor so the `Slides` area no longer uses a plain textarea by default.

New responsibilities:
- initialize slide form state from `slideOutlineJson`
- render structured editor UI
- propagate edited slide data back into `generatedContentForm.slideOutlineJson`

### New `SlideEditor` Component

Add a dedicated component, likely something like:
- `frontend/src/components/course/SlideEditor.jsx`

Responsibilities:
- render slide list and editor controls
- handle slide insert/remove/reorder
- handle bullet point insert/remove
- keep slide list state normalized

### Parsing Helpers

Add small helper utilities, likely under `frontend/src/utils/` or local component helpers:
- parse `slideOutlineJson` safely
- serialize slides safely
- normalize slide numbering
- surface validation errors predictably

## Error Handling

### Invalid Existing JSON

If `slideOutlineJson` cannot be parsed:
- preview mode should show a warning
- edit mode should show a recoverable error state
- admin can either cancel editing or reset slides manually

The system should not silently discard invalid raw JSON.

### Save Failures

If API update fails:
- preserve unsaved editor state in the form
- show backend error message if available
- do not revert UI automatically

## Backend Impact

Minimal backend impact.

No schema change required.

Existing generated-content update endpoint remains the write path. The backend continues to receive:
- `teachingScript`
- `slideOutlineJson`
- `voiceoverPlanJson`

Potential backend hardening worth including in implementation:
- validate that incoming `slideOutlineJson` is syntactically valid JSON before persistence
- optionally validate required slide fields server-side to avoid storing malformed data

This hardening is recommended because after adding a form editor, the API becomes the final trust boundary.

## Testing Strategy

### Frontend

Add tests for:
- preview renders slide cards from valid JSON
- preview shows fallback on invalid JSON
- editor initializes from valid JSON
- add/remove/reorder slide works
- add/remove bullet point works
- save serializes normalized `slideOutlineJson`
- validation blocks save on incomplete slides

### Backend

If server-side validation is added, add tests for:
- valid `slideOutlineJson` accepted
- malformed JSON rejected
- structurally incomplete slide data rejected

## Rollout Strategy

Implement in-place on the current admin lesson content page.

No migration or feature flag is required unless the page becomes too dense during implementation.

## Future Extensions

This design deliberately leaves room for later additions:
- `layout/type`
- `imagePrompt`
- `animationNotes`
- `estimatedDuration`
- slide-level export/render pipeline

Those should be introduced only after the editorial workflow proves useful.

## Open Decisions Already Resolved

The following are intentionally fixed for this version:
- structured form editor, not raw JSON editing
- no backend schema redesign yet
- only the 3 existing slide content fields are supported
- keep current lesson AI content page as the editing surface

## Implementation Summary

Build a structured `Slide Editor` in frontend on top of existing `slideOutlineJson`, keep backend storage unchanged, add client-side validation and structured preview, and optionally harden backend validation at the API boundary.
