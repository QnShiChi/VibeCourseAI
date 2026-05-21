# Voiceover Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace raw `voiceoverPlanJson` preview and editing with a structured voiceover editor that preserves current storage and API contracts.

**Architecture:** Add a focused frontend helper that parses, validates, and serializes voiceover plan JSON, then layer a dedicated `VoiceoverEditor` and structured preview on top of the existing lesson content flow. Mirror slide outline validation on the backend so lesson generated content saves reject malformed or incomplete voiceover plan objects while remaining backward-compatible with PascalCase and camelCase keys.

**Tech Stack:** React, Vite, Vitest, ASP.NET Core, C#

---

## File Structure

- Create: `frontend/src/utils/voiceoverPlan.js`
  Parses PascalCase/camelCase voiceover JSON into a normalized object, validates fields, and serializes normalized output.
- Create: `frontend/src/utils/voiceoverPlan.test.js`
  Focused helper tests for parse, validate, and serialize behavior.
- Create: `frontend/src/components/course/VoiceoverEditor.jsx`
  Structured editor for the five voiceover plan fields.
- Create: `frontend/src/components/course/VoiceoverEditor.test.jsx`
  Component tests for field editing and validation display.
- Modify: `frontend/src/components/course/LessonContentEditor.jsx`
  Replace raw voiceover JSON textarea with `VoiceoverEditor`.
- Modify: `frontend/src/components/course/LessonContentEditor.test.jsx`
  Cover structured voiceover editing inside the lesson content editor.
- Modify: `frontend/src/components/course/LessonContentPreview.jsx`
  Replace raw voiceover JSON preview with structured field cards.
- Modify: `frontend/src/components/course/LessonContentPreview.test.jsx`
  Cover structured voiceover preview and invalid JSON behavior.
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`
  Keep the save flow test aligned with normalized `voiceoverPlanJson`.
- Modify: `frontend/src/styles/theme.css`
  Add minimal styles for structured voiceover preview/editor blocks that match the slide editor visual language.
- Create: `backend/CourseVideo.API/Services/VoiceoverPlanValidation.cs`
  Backend validation helper for `VoiceoverPlanJson`.
- Create: `backend/CourseVideo.API.Tests/Services/VoiceoverPlanValidationTests.cs`
  Validation tests for valid/invalid voiceover plan payloads.
- Modify: `backend/CourseVideo.API/Services/LessonService.cs`
  Call backend voiceover validation before persisting generated lesson content.
- Modify: `backend/CourseVideo.API/Controllers/LessonsController.cs`
  Reuse existing validation error handling path; only touch if needed for message consistency.

### Task 1: Frontend Voiceover Helper

**Files:**
- Create: `frontend/src/utils/voiceoverPlan.js`
- Test: `frontend/src/utils/voiceoverPlan.test.js`

- [ ] **Step 1: Write the failing helper tests**

```javascript
import { describe, expect, it } from "vitest";
import {
  normalizeVoiceoverPlan,
  parseVoiceoverPlanJson,
  serializeVoiceoverPlan,
  validateVoiceoverPlan
} from "./voiceoverPlan";

describe("voiceoverPlan helpers", () => {
  it("parses camelCase voiceover JSON into a normalized object", () => {
    const plan = parseVoiceoverPlanJson(
      '{"estimatedDurationMinutes":8,"tone":" Clear ","pacing":" Moderate ","targetAudience":" Students ","pronunciationNotes":" OOP "}'
    );

    expect(plan).toEqual({
      estimatedDurationMinutes: 8,
      tone: " Clear ",
      pacing: " Moderate ",
      targetAudience: " Students ",
      pronunciationNotes: " OOP "
    });
  });

  it("parses PascalCase voiceover JSON into a normalized object", () => {
    const plan = parseVoiceoverPlanJson(
      '{"EstimatedDurationMinutes":8,"Tone":" Clear ","Pacing":" Moderate ","TargetAudience":" Students ","PronunciationNotes":" OOP "}'
    );

    expect(plan).toEqual({
      estimatedDurationMinutes: 8,
      tone: " Clear ",
      pacing: " Moderate ",
      targetAudience: " Students ",
      pronunciationNotes: " OOP "
    });
  });

  it("normalizes and trims voiceover plan fields before save", () => {
    expect(
      normalizeVoiceoverPlan({
        estimatedDurationMinutes: "8",
        tone: " Clear ",
        pacing: " Moderate ",
        targetAudience: " Students ",
        pronunciationNotes: " OOP "
      })
    ).toEqual({
      estimatedDurationMinutes: 8,
      tone: "Clear",
      pacing: "Moderate",
      targetAudience: "Students",
      pronunciationNotes: "OOP"
    });
  });

  it("rejects malformed JSON", () => {
    expect(() => parseVoiceoverPlanJson("{bad json}")).toThrow();
  });

  it("rejects missing or invalid required fields", () => {
    expect(() =>
      validateVoiceoverPlan({
        estimatedDurationMinutes: 0,
        tone: "",
        pacing: "Moderate",
        targetAudience: "Students",
        pronunciationNotes: "OOP"
      })
    ).toThrow("Thời lượng dự kiến phải lớn hơn 0.");
  });

  it("serializes normalized voiceover plans to camelCase JSON", () => {
    expect(
      serializeVoiceoverPlan({
        estimatedDurationMinutes: 8,
        tone: "Clear",
        pacing: "Moderate",
        targetAudience: "Students",
        pronunciationNotes: "OOP"
      })
    ).toBe(
      '{"estimatedDurationMinutes":8,"tone":"Clear","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
    );
  });
});
```

- [ ] **Step 2: Run the helper test to verify it fails**

Run: `npm run test -- --run src/utils/voiceoverPlan.test.js`

Expected: FAIL with module-not-found or missing export errors for `voiceoverPlan`.

- [ ] **Step 3: Write the minimal helper implementation**

```javascript
export function parseVoiceoverPlanJson(value) {
  if (!value?.trim()) {
    return null;
  }

  const parsed = JSON.parse(value);
  if (!parsed || Array.isArray(parsed) || typeof parsed !== "object") {
    throw new Error("Voiceover plan phải là một object.");
  }

  return {
    estimatedDurationMinutes: Number(
      parsed.estimatedDurationMinutes ?? parsed.EstimatedDurationMinutes ?? 0
    ),
    tone: String(parsed.tone ?? parsed.Tone ?? ""),
    pacing: String(parsed.pacing ?? parsed.Pacing ?? ""),
    targetAudience: String(parsed.targetAudience ?? parsed.TargetAudience ?? ""),
    pronunciationNotes: String(
      parsed.pronunciationNotes ?? parsed.PronunciationNotes ?? ""
    )
  };
}

export function normalizeVoiceoverPlan(plan) {
  return {
    estimatedDurationMinutes: Number(plan.estimatedDurationMinutes),
    tone: String(plan.tone ?? "").trim(),
    pacing: String(plan.pacing ?? "").trim(),
    targetAudience: String(plan.targetAudience ?? "").trim(),
    pronunciationNotes: String(plan.pronunciationNotes ?? "").trim()
  };
}

export function validateVoiceoverPlan(plan) {
  const normalized = normalizeVoiceoverPlan(plan);

  if (!Number.isFinite(normalized.estimatedDurationMinutes) || normalized.estimatedDurationMinutes <= 0) {
    throw new Error("Thời lượng dự kiến phải lớn hơn 0.");
  }

  if (!normalized.tone) {
    throw new Error("Giọng điệu là bắt buộc.");
  }

  if (!normalized.pacing) {
    throw new Error("Nhịp đọc là bắt buộc.");
  }

  if (!normalized.targetAudience) {
    throw new Error("Đối tượng nghe là bắt buộc.");
  }

  if (!normalized.pronunciationNotes) {
    throw new Error("Lưu ý phát âm là bắt buộc.");
  }
}

export function serializeVoiceoverPlan(plan) {
  validateVoiceoverPlan(plan);
  return JSON.stringify(normalizeVoiceoverPlan(plan));
}
```

- [ ] **Step 4: Run the helper test to verify it passes**

Run: `npm run test -- --run src/utils/voiceoverPlan.test.js`

Expected: PASS with `6 passed`.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/utils/voiceoverPlan.js frontend/src/utils/voiceoverPlan.test.js
git commit -m "feat: add voiceover plan helper"
```

### Task 2: Voiceover Editor Component

**Files:**
- Create: `frontend/src/components/course/VoiceoverEditor.jsx`
- Modify: `frontend/src/styles/theme.css`
- Test: `frontend/src/components/course/VoiceoverEditor.test.jsx`

- [ ] **Step 1: Write the failing component test**

```javascript
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import VoiceoverEditor from "./VoiceoverEditor";

describe("VoiceoverEditor", () => {
  it("updates structured voiceover fields", () => {
    const onChange = vi.fn();

    render(
      <VoiceoverEditor
        voiceoverPlan={{
          estimatedDurationMinutes: 8,
          tone: "Clear",
          pacing: "Moderate",
          targetAudience: "Students",
          pronunciationNotes: "OOP"
        }}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.change(screen.getByLabelText("Giọng điệu"), {
      target: { value: "Warm" }
    });

    expect(onChange).toHaveBeenCalledWith({
      estimatedDurationMinutes: 8,
      tone: "Warm",
      pacing: "Moderate",
      targetAudience: "Students",
      pronunciationNotes: "OOP"
    });
  });
});
```

- [ ] **Step 2: Run the component test to verify it fails**

Run: `npm run test -- --run src/components/course/VoiceoverEditor.test.jsx`

Expected: FAIL with module-not-found for `VoiceoverEditor`.

- [ ] **Step 3: Implement the minimal voiceover editor and styles**

```javascript
import FormField from "../ui/FormField";

export default function VoiceoverEditor({ voiceoverPlan, onChange, validationError }) {
  function updateField(field, value) {
    onChange({
      ...voiceoverPlan,
      [field]: field === "estimatedDurationMinutes" ? value : value
    });
  }

  return (
    <div className="voiceover-editor-card">
      <FormField id="voiceover-duration" label="Thời lượng dự kiến (phút)">
        <input
          className="ui-input"
          id="voiceover-duration"
          min="1"
          type="number"
          value={voiceoverPlan.estimatedDurationMinutes}
          onChange={(event) => updateField("estimatedDurationMinutes", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-tone" label="Giọng điệu">
        <input
          className="ui-input"
          id="voiceover-tone"
          value={voiceoverPlan.tone}
          onChange={(event) => updateField("tone", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-pacing" label="Nhịp đọc">
        <textarea
          className="ui-input ui-textarea"
          id="voiceover-pacing"
          rows="3"
          value={voiceoverPlan.pacing}
          onChange={(event) => updateField("pacing", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-target" label="Đối tượng nghe">
        <textarea
          className="ui-input ui-textarea"
          id="voiceover-target"
          rows="3"
          value={voiceoverPlan.targetAudience}
          onChange={(event) => updateField("targetAudience", event.target.value)}
        />
      </FormField>

      <FormField id="voiceover-pronunciation" label="Lưu ý phát âm">
        <textarea
          className="ui-input ui-textarea"
          id="voiceover-pronunciation"
          rows="3"
          value={voiceoverPlan.pronunciationNotes}
          onChange={(event) => updateField("pronunciationNotes", event.target.value)}
        />
      </FormField>

      {validationError ? <p className="lesson-card__error">{validationError}</p> : null}
    </div>
  );
}
```

```css
.voiceover-editor-card {
  display: grid;
  gap: 1rem;
}

.voiceover-preview-grid {
  display: grid;
  gap: 0.75rem;
}

.voiceover-preview-item {
  border: 1px solid var(--color-border-soft);
  border-radius: 1rem;
  padding: 1rem 1.125rem;
  background: rgba(255, 255, 255, 0.72);
}

.voiceover-preview-item strong {
  display: block;
  margin-bottom: 0.4rem;
}
```

- [ ] **Step 4: Run the component test to verify it passes**

Run: `npm run test -- --run src/components/course/VoiceoverEditor.test.jsx`

Expected: PASS with `1 passed`.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/course/VoiceoverEditor.jsx frontend/src/components/course/VoiceoverEditor.test.jsx frontend/src/styles/theme.css
git commit -m "feat: add voiceover editor component"
```

### Task 3: Lesson Content Editor Integration

**Files:**
- Modify: `frontend/src/components/course/LessonContentEditor.jsx`
- Test: `frontend/src/components/course/LessonContentEditor.test.jsx`

- [ ] **Step 1: Extend the lesson content editor test**

```javascript
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonContentEditor from "./LessonContentEditor";

describe("LessonContentEditor", () => {
  it("serializes structured voiceover edits back into voiceoverPlanJson", () => {
    const onChange = vi.fn();

    render(
      <LessonContentEditor
        form={{
          teachingScript: "Script",
          slideOutlineJson: '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]',
          voiceoverPlanJson:
            '{"estimatedDurationMinutes":8,"tone":"Clear","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
        }}
        onChange={onChange}
        onCancel={() => {}}
        onSave={() => {}}
      />
    );

    fireEvent.change(screen.getByLabelText("Giọng điệu"), {
      target: { value: "Warm" }
    });

    expect(onChange).toHaveBeenCalledWith(
      "voiceoverPlanJson",
      '{"estimatedDurationMinutes":8,"tone":"Warm","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
    );
  });
});
```

- [ ] **Step 2: Run the lesson content editor test to verify it fails**

Run: `npm run test -- --run src/components/course/LessonContentEditor.test.jsx`

Expected: FAIL because the current editor still renders a raw `Voiceover plan JSON` textarea and never emits serialized voiceover JSON from structured fields.

- [ ] **Step 3: Implement voiceover state and editor wiring**

```javascript
import { useEffect, useState } from "react";
import { parseSlideOutlineJson, serializeSlideOutline } from "../../utils/slideOutline";
import {
  parseVoiceoverPlanJson,
  serializeVoiceoverPlan
} from "../../utils/voiceoverPlan";
import Button from "../ui/Button";
import FormField from "../ui/FormField";
import SlideEditor from "./SlideEditor";
import VoiceoverEditor from "./VoiceoverEditor";

export default function LessonContentEditor({ form, onChange, onSave, onCancel }) {
  const [slideError, setSlideError] = useState("");
  const [voiceoverError, setVoiceoverError] = useState("");
  const [slides, setSlides] = useState(() => safeParseSlides(form.slideOutlineJson).slides);
  const [voiceoverPlan, setVoiceoverPlan] = useState(
    () => safeParseVoiceover(form.voiceoverPlanJson).voiceoverPlan
  );

  useEffect(() => {
    const parsedSlides = safeParseSlides(form.slideOutlineJson);
    setSlides(parsedSlides.slides);
    setSlideError(parsedSlides.error);

    const parsedVoiceover = safeParseVoiceover(form.voiceoverPlanJson);
    setVoiceoverPlan(parsedVoiceover.voiceoverPlan);
    setVoiceoverError(parsedVoiceover.error);
  }, [form.slideOutlineJson, form.voiceoverPlanJson]);

  function handleSlidesChange(nextSlides) {
    setSlides(nextSlides);

    try {
      const serialized = serializeSlideOutline(nextSlides);
      setSlideError("");
      onChange("slideOutlineJson", serialized);
    } catch (error) {
      setSlideError(error.message);
    }
  }

  function handleVoiceoverChange(nextPlan) {
    setVoiceoverPlan(nextPlan);

    try {
      const serialized = serializeVoiceoverPlan(nextPlan);
      setVoiceoverError("");
      onChange("voiceoverPlanJson", serialized);
    } catch (error) {
      setVoiceoverError(error.message);
    }
  }

  return (
    <div className="inline-edit-card">
      {/* existing script field */}
      <div className="form-field">
        <span className="form-field__label">Slides</span>
        <SlideEditor slides={slides} onChange={handleSlidesChange} validationError={slideError} />
      </div>

      <div className="form-field">
        <span className="form-field__label">Voiceover</span>
        <VoiceoverEditor
          voiceoverPlan={voiceoverPlan}
          onChange={handleVoiceoverChange}
          validationError={voiceoverError}
        />
      </div>
    </div>
  );
}

function safeParseVoiceover(voiceoverPlanJson) {
  try {
    const voiceoverPlan = parseVoiceoverPlanJson(voiceoverPlanJson);
    return {
      voiceoverPlan: voiceoverPlan ?? {
        estimatedDurationMinutes: 1,
        tone: "",
        pacing: "",
        targetAudience: "",
        pronunciationNotes: ""
      },
      error: ""
    };
  } catch (error) {
    return {
      voiceoverPlan: {
        estimatedDurationMinutes: 1,
        tone: "",
        pacing: "",
        targetAudience: "",
        pronunciationNotes: ""
      },
      error: error.message
    };
  }
}
```

- [ ] **Step 4: Run the lesson content editor test to verify it passes**

Run: `npm run test -- --run src/components/course/LessonContentEditor.test.jsx`

Expected: PASS with the editor now emitting normalized `voiceoverPlanJson`.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/course/LessonContentEditor.jsx frontend/src/components/course/LessonContentEditor.test.jsx
git commit -m "feat: wire voiceover editor into lesson content editing"
```

### Task 4: Structured Voiceover Preview

**Files:**
- Modify: `frontend/src/components/course/LessonContentPreview.jsx`
- Test: `frontend/src/components/course/LessonContentPreview.test.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Write the preview test for structured voiceover display**

```javascript
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import LessonContentPreview from "./LessonContentPreview";

describe("LessonContentPreview", () => {
  it("renders structured voiceover fields", () => {
    render(
      <LessonContentPreview
        content={{
          teachingScript: "Script",
          slideOutlineJson: '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]',
          voiceoverPlanJson:
            '{"estimatedDurationMinutes":8,"tone":"Clear","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
        }}
      />
    );

    expect(screen.getByText("Thời lượng dự kiến")).toBeInTheDocument();
    expect(screen.getByText("8 phút")).toBeInTheDocument();
    expect(screen.getByText("Giọng điệu")).toBeInTheDocument();
    expect(screen.getByText("Clear")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the preview test to verify it fails**

Run: `npm run test -- --run src/components/course/LessonContentPreview.test.jsx`

Expected: FAIL because the current preview still renders raw voiceover JSON inside a `pre`.

- [ ] **Step 3: Implement the structured preview**

```javascript
import { parseSlideOutlineJson } from "../../utils/slideOutline";
import { parseVoiceoverPlanJson } from "../../utils/voiceoverPlan";

export default function LessonContentPreview({ content }) {
  if (!content) {
    return null;
  }

  const { slides, error } = getSlidePreviewState(content.slideOutlineJson);
  const { voiceoverPlan, voiceoverError } = getVoiceoverPreviewState(content.voiceoverPlanJson);

  return (
    <div className="lesson-generated-stack">
      {/* existing script and slides sections */}
      <section className="surface-card lesson-generated-card">
        <h4>Voiceover</h4>
        {voiceoverError ? (
          <p className="lesson-card__error">{voiceoverError}</p>
        ) : voiceoverPlan ? (
          <div className="voiceover-preview-grid">
            <article className="voiceover-preview-item">
              <strong>Thời lượng dự kiến</strong>
              <p>{voiceoverPlan.estimatedDurationMinutes} phút</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Giọng điệu</strong>
              <p>{voiceoverPlan.tone}</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Nhịp đọc</strong>
              <p>{voiceoverPlan.pacing}</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Đối tượng nghe</strong>
              <p>{voiceoverPlan.targetAudience}</p>
            </article>
            <article className="voiceover-preview-item">
              <strong>Lưu ý phát âm</strong>
              <p>{voiceoverPlan.pronunciationNotes}</p>
            </article>
          </div>
        ) : (
          <p>Chưa có voiceover plan.</p>
        )}
      </section>
    </div>
  );
}

function getVoiceoverPreviewState(voiceoverPlanJson) {
  if (!voiceoverPlanJson?.trim()) {
    return { voiceoverPlan: null, voiceoverError: "" };
  }

  try {
    return {
      voiceoverPlan: parseVoiceoverPlanJson(voiceoverPlanJson),
      voiceoverError: ""
    };
  } catch {
    return {
      voiceoverPlan: null,
      voiceoverError: "Voiceover plan JSON hiện tại không hợp lệ."
    };
  }
}
```

- [ ] **Step 4: Run the preview test to verify it passes**

Run: `npm run test -- --run src/components/course/LessonContentPreview.test.jsx`

Expected: PASS with structured voiceover fields visible.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/course/LessonContentPreview.jsx frontend/src/components/course/LessonContentPreview.test.jsx frontend/src/styles/theme.css
git commit -m "feat: add structured voiceover preview"
```

### Task 5: Course Page Save Flow Regression Coverage

**Files:**
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`

- [ ] **Step 1: Extend the course page save-flow test with normalized voiceover JSON**

```javascript
mockGetLessonGeneratedContent.mockResolvedValue({
  lessonId: "lesson-1",
  lessonTitle: "Bai 1",
  teachingScript: "Script goc",
  slideOutlineJson: '[{"slideNumber":1,"title":"S1","bulletPoints":["A"],"speakerNotes":"N"}]',
  voiceoverPlanJson:
    '{"EstimatedDurationMinutes":8,"Tone":"Clear","Pacing":"Moderate","TargetAudience":"Students","PronunciationNotes":"OOP"}',
  contentGenerationStatus: "Completed"
});

fireEvent.click(screen.getByRole("button", { name: "Chỉnh nội dung AI" }));
fireEvent.change(screen.getByLabelText("Giọng điệu"), {
  target: { value: "Warm" }
});
fireEvent.click(screen.getByRole("button", { name: "Lưu nội dung AI" }));

await waitFor(() =>
  expect(mockUpdateLessonGeneratedContent).toHaveBeenCalledWith("lesson-1", {
    teachingScript: "Script goc",
    slideOutlineJson: '[{"slideNumber":1,"title":"S1","bulletPoints":["A"],"speakerNotes":"N"}]',
    voiceoverPlanJson:
      '{"estimatedDurationMinutes":8,"tone":"Warm","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
  })
);
```

- [ ] **Step 2: Run the course page test to verify it fails**

Run: `npm run test -- --run src/pages/CourseStructurePage.test.jsx`

Expected: FAIL until the new voiceover editor is wired into the page flow.

- [ ] **Step 3: Adjust the page test expectations to the normalized voiceover behavior**

```javascript
mockUpdateLessonGeneratedContent.mockResolvedValue({
  lessonId: "lesson-1",
  lessonTitle: "Bai 1",
  teachingScript: "Script goc",
  slideOutlineJson: '[{"slideNumber":1,"title":"S1","bulletPoints":["A"],"speakerNotes":"N"}]',
  voiceoverPlanJson:
    '{"estimatedDurationMinutes":8,"tone":"Warm","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}',
  contentGenerationStatus: "ManuallyEdited"
});
```

- [ ] **Step 4: Run the course page test to verify it passes**

Run: `npm run test -- --run src/pages/CourseStructurePage.test.jsx`

Expected: PASS with the page save flow still calling `updateLessonGeneratedContent` once and using normalized voiceover JSON.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/CourseStructurePage.test.jsx
git commit -m "test: cover voiceover editor save flow"
```

### Task 6: Backend Voiceover Validation

**Files:**
- Create: `backend/CourseVideo.API/Services/VoiceoverPlanValidation.cs`
- Modify: `backend/CourseVideo.API/Services/LessonService.cs`
- Test: `backend/CourseVideo.API.Tests/Services/VoiceoverPlanValidationTests.cs`

- [ ] **Step 1: Write the backend validation tests**

```csharp
using FluentAssertions;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class VoiceoverPlanValidationTests
{
    [Fact]
    public void ParseAndValidate_AcceptsCamelCasePayload()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate(
            "{\"estimatedDurationMinutes\":8,\"tone\":\"Clear\",\"pacing\":\"Moderate\",\"targetAudience\":\"Students\",\"pronunciationNotes\":\"OOP\"}"
        );

        action.Should().NotThrow();
    }

    [Fact]
    public void ParseAndValidate_AcceptsPascalCasePayload()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate(
            "{\"EstimatedDurationMinutes\":8,\"Tone\":\"Clear\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        );

        action.Should().NotThrow();
    }

    [Fact]
    public void ParseAndValidate_RejectsMalformedJson()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate("{bad json}");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Voiceover plan JSON không hợp lệ.");
    }

    [Fact]
    public void ParseAndValidate_RejectsMissingFields()
    {
        var action = () => VoiceoverPlanValidation.ParseAndValidate(
            "{\"EstimatedDurationMinutes\":0,\"Tone\":\"\",\"Pacing\":\"Moderate\",\"TargetAudience\":\"Students\",\"PronunciationNotes\":\"OOP\"}"
        );

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Voiceover plan phải có estimatedDurationMinutes, tone, pacing, targetAudience và pronunciationNotes hợp lệ.");
    }
}
```

- [ ] **Step 2: Run the backend validation test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj \
  --filter VoiceoverPlanValidationTests \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.Tests.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.Tests.bin/
```

Expected: FAIL because `VoiceoverPlanValidation` does not exist yet or environment-specific test restore issues still block the run.

- [ ] **Step 3: Implement backend validation and wire it into lesson saves**

```csharp
using System.Text.Json;

namespace CourseVideo.API.Services;

public static class VoiceoverPlanValidation
{
    public static void ParseAndValidate(string json)
    {
        JsonElement root;

        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Voiceover plan JSON không hợp lệ.");
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Voiceover plan JSON không hợp lệ.");
        }

        var duration = ReadNumber(root, "estimatedDurationMinutes", "EstimatedDurationMinutes");
        var tone = ReadString(root, "tone", "Tone");
        var pacing = ReadString(root, "pacing", "Pacing");
        var targetAudience = ReadString(root, "targetAudience", "TargetAudience");
        var pronunciationNotes = ReadString(root, "pronunciationNotes", "PronunciationNotes");

        if (duration <= 0 ||
            string.IsNullOrWhiteSpace(tone) ||
            string.IsNullOrWhiteSpace(pacing) ||
            string.IsNullOrWhiteSpace(targetAudience) ||
            string.IsNullOrWhiteSpace(pronunciationNotes))
        {
            throw new InvalidOperationException(
                "Voiceover plan phải có estimatedDurationMinutes, tone, pacing, targetAudience và pronunciationNotes hợp lệ."
            );
        }
    }

    private static double ReadNumber(JsonElement root, string camelCase, string pascalCase)
    {
        if (root.TryGetProperty(camelCase, out var camelValue) && camelValue.TryGetDouble(out var camelNumber))
        {
            return camelNumber;
        }

        if (root.TryGetProperty(pascalCase, out var pascalValue) && pascalValue.TryGetDouble(out var pascalNumber))
        {
            return pascalNumber;
        }

        return 0;
    }

    private static string ReadString(JsonElement root, string camelCase, string pascalCase)
    {
        if (root.TryGetProperty(camelCase, out var camelValue) && camelValue.ValueKind == JsonValueKind.String)
        {
            return camelValue.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty(pascalCase, out var pascalValue) && pascalValue.ValueKind == JsonValueKind.String)
        {
            return pascalValue.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
```

```csharp
lesson.TeachingScript = request.TeachingScript.Trim();
SlideOutlineValidation.ParseAndValidate(request.SlideOutlineJson.Trim());
VoiceoverPlanValidation.ParseAndValidate(request.VoiceoverPlanJson.Trim());
lesson.SlideOutlineJson = request.SlideOutlineJson.Trim();
lesson.VoiceoverPlanJson = request.VoiceoverPlanJson.Trim();
```

- [ ] **Step 4: Run backend verification**

Run build:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj \
  -p:BaseIntermediateOutputPath=/tmp/CourseVideo.API.obj/ \
  -p:BaseOutputPath=/tmp/CourseVideo.API.bin/ \
  -p:GenerateAssemblyInfo=false \
  -p:GenerateTargetFrameworkAttribute=false
```

Run API validation check:

```bash
curl -s -X PUT http://localhost:5000/api/lessons/<lesson-id>/content \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"teachingScript":"Script","slideOutlineJson":"[{\"slideNumber\":1,\"title\":\"Intro\",\"bulletPoints\":[\"A\"],\"speakerNotes\":\"N\"}]","voiceoverPlanJson":"{bad json}"}'
```

Expected:

- build PASS
- API returns `400` with `Voiceover plan JSON không hợp lệ.`

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Services/VoiceoverPlanValidation.cs backend/CourseVideo.API/Services/LessonService.cs backend/CourseVideo.API.Tests/Services/VoiceoverPlanValidationTests.cs
git commit -m "feat: validate voiceover plans on lesson save"
```

### Task 7: Full Verification

**Files:**
- Modify: `frontend/src/components/course/LessonContentEditor.jsx`
- Modify: `frontend/src/components/course/LessonContentPreview.jsx`
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`
- Modify: `frontend/src/styles/theme.css`
- Create: `frontend/src/utils/voiceoverPlan.js`
- Create: `frontend/src/components/course/VoiceoverEditor.jsx`
- Create: `backend/CourseVideo.API/Services/VoiceoverPlanValidation.cs`

- [ ] **Step 1: Run focused frontend tests in the writable verify copy**

Run:

```bash
cp frontend/src/utils/voiceoverPlan.js /tmp/vibecourseai-frontend-verify/frontend/src/utils/voiceoverPlan.js
cp frontend/src/utils/voiceoverPlan.test.js /tmp/vibecourseai-frontend-verify/frontend/src/utils/voiceoverPlan.test.js
cp frontend/src/components/course/VoiceoverEditor.jsx /tmp/vibecourseai-frontend-verify/frontend/src/components/course/VoiceoverEditor.jsx
cp frontend/src/components/course/VoiceoverEditor.test.jsx /tmp/vibecourseai-frontend-verify/frontend/src/components/course/VoiceoverEditor.test.jsx
cp frontend/src/components/course/LessonContentEditor.jsx /tmp/vibecourseai-frontend-verify/frontend/src/components/course/LessonContentEditor.jsx
cp frontend/src/components/course/LessonContentEditor.test.jsx /tmp/vibecourseai-frontend-verify/frontend/src/components/course/LessonContentEditor.test.jsx
cp frontend/src/components/course/LessonContentPreview.jsx /tmp/vibecourseai-frontend-verify/frontend/src/components/course/LessonContentPreview.jsx
cp frontend/src/components/course/LessonContentPreview.test.jsx /tmp/vibecourseai-frontend-verify/frontend/src/components/course/LessonContentPreview.test.jsx
cp frontend/src/pages/CourseStructurePage.test.jsx /tmp/vibecourseai-frontend-verify/frontend/src/pages/CourseStructurePage.test.jsx
cp frontend/src/styles/theme.css /tmp/vibecourseai-frontend-verify/frontend/src/styles/theme.css
cd /tmp/vibecourseai-frontend-verify/frontend
npm run test -- --run src/utils/voiceoverPlan.test.js src/components/course/VoiceoverEditor.test.jsx src/components/course/LessonContentEditor.test.jsx src/components/course/LessonContentPreview.test.jsx src/pages/CourseStructurePage.test.jsx
```

Expected: PASS for all focused frontend tests.

- [ ] **Step 2: Run the frontend production build in the verify copy**

Run:

```bash
cd /tmp/vibecourseai-frontend-verify/frontend
npm run build
```

Expected: PASS and a generated `dist/assets/index-*.js` bundle.

- [ ] **Step 3: Rebuild and redeploy containers if the user wants runtime verification**

Run:

```bash
docker compose build frontend backend
docker compose up -d frontend backend
curl -s http://localhost:3000 | grep -o 'index-[^\" ]*\\.js' | head -n 1
curl -i http://localhost:5000/api/health
```

Expected:

- frontend container recreated with new bundle
- backend health returns `200 OK`

- [ ] **Step 4: Perform a live lesson content save smoke test**

Run:

```bash
python3 - <<'PY'
import json, urllib.request

base='http://localhost:5000'
lesson_id='<lesson-id>'
login_payload=json.dumps({'email':'admin@vibecourse.local','password':'ChangeMe@123'}).encode()
with urllib.request.urlopen(
    urllib.request.Request(base+'/api/auth/login', data=login_payload, headers={'Content-Type':'application/json'}, method='POST')
) as resp:
    token=json.load(resp)['accessToken']

headers={'Authorization': f'Bearer {token}'}
with urllib.request.urlopen(
    urllib.request.Request(base+f'/api/lessons/{lesson_id}/content', headers=headers)
) as resp:
    original=json.load(resp)

payload={
    'teachingScript': original['teachingScript'],
    'slideOutlineJson': original['slideOutlineJson'],
    'voiceoverPlanJson': '{"estimatedDurationMinutes":9,"tone":"Warm","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
}

with urllib.request.urlopen(
    urllib.request.Request(
        base+f'/api/lessons/{lesson_id}/content',
        data=json.dumps(payload).encode(),
        headers={'Content-Type':'application/json', **headers},
        method='PUT'
    )
) as resp:
    updated=json.load(resp)

print(updated['voiceoverPlanJson'])
PY
```

Expected: live API returns normalized `voiceoverPlanJson` and `contentGenerationStatus` becomes `ManuallyEdited`.

- [ ] **Step 5: Commit the verified final integration**

```bash
git add frontend/src/utils/voiceoverPlan.js frontend/src/utils/voiceoverPlan.test.js frontend/src/components/course/VoiceoverEditor.jsx frontend/src/components/course/VoiceoverEditor.test.jsx frontend/src/components/course/LessonContentEditor.jsx frontend/src/components/course/LessonContentEditor.test.jsx frontend/src/components/course/LessonContentPreview.jsx frontend/src/components/course/LessonContentPreview.test.jsx frontend/src/pages/CourseStructurePage.test.jsx frontend/src/styles/theme.css backend/CourseVideo.API/Services/VoiceoverPlanValidation.cs backend/CourseVideo.API/Services/LessonService.cs backend/CourseVideo.API.Tests/Services/VoiceoverPlanValidationTests.cs
git commit -m "feat: add structured voiceover editor"
```
