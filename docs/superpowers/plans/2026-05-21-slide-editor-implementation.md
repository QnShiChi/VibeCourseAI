# Slide Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace raw `slideOutlineJson` editing on the admin lesson AI content screen with a structured slide editor and structured slide preview while preserving the current backend storage model.

**Architecture:** Keep `Lessons.SlideOutlineJson` as the backend source of truth. Add frontend parsing/serialization helpers plus a dedicated `SlideEditor` component that edits structured slide state and writes back normalized JSON through the existing generated-content update flow. Add optional backend validation at the API boundary so malformed slide JSON cannot be persisted.

**Tech Stack:** React, Vite, Vitest, Testing Library, ASP.NET Core 8, existing lesson generated-content API.

---

## File Map

**Frontend existing files**
- Modify: `frontend/src/components/course/LessonContentPreview.jsx`
  Purpose: replace raw JSON display with structured slide preview cards.
- Modify: `frontend/src/components/course/LessonContentEditor.jsx`
  Purpose: replace slide JSON textarea with structured `SlideEditor` UI while keeping script and voiceover editing intact.
- Modify: `frontend/src/components/course/LessonContentEditor.test.jsx`
  Purpose: cover editor wiring after the component changes.
- Modify: `frontend/src/pages/CourseStructurePage.jsx`
  Purpose: keep generated-content save flow working with normalized `slideOutlineJson`.
- Modify: `frontend/src/styles/theme.css`
  Purpose: style slide preview cards and slide editor controls.

**Frontend new files**
- Create: `frontend/src/components/course/SlideEditor.jsx`
  Purpose: render editable slide list with add/remove/reorder/bullet controls.
- Create: `frontend/src/components/course/SlideEditor.test.jsx`
  Purpose: cover slide editing interactions and validation behavior.
- Create: `frontend/src/utils/slideOutline.js`
  Purpose: parse, normalize, validate, and serialize slide outline JSON.
- Create: `frontend/src/utils/slideOutline.test.js`
  Purpose: cover parsing and serialization edge cases.

**Backend existing files**
- Modify: `backend/CourseVideo.API/Controllers/LessonsController.cs`
  Purpose: reject malformed generated-content payloads earlier with clear validation messages if server-side slide validation is added.
- Modify: `backend/CourseVideo.API/Services/LessonService.cs`
  Purpose: validate `slideOutlineJson` structure before persistence.

**Backend new files**
- Create: `backend/CourseVideo.API/Services/SlideOutlineValidation.cs`
  Purpose: shared server-side parser/validator for `slideOutlineJson`.
- Create: `backend/CourseVideo.API.Tests/Services/SlideOutlineValidationTests.cs`
  Purpose: cover valid, malformed, and incomplete slide outline payloads.

---

### Task 1: Add Frontend Slide Outline Helpers

**Files:**
- Create: `frontend/src/utils/slideOutline.js`
- Create: `frontend/src/utils/slideOutline.test.js`

- [ ] **Step 1: Write the failing helper tests**

```js
import { describe, expect, it } from "vitest";
import {
  parseSlideOutlineJson,
  serializeSlideOutline,
  validateSlides,
  normalizeSlides
} from "./slideOutline";

describe("slideOutline helpers", () => {
  it("parses valid slide JSON into structured slides", () => {
    const slides = parseSlideOutlineJson(
      '[{"slideNumber":2,"title":" Intro ","bulletPoints":[" A ",""],"speakerNotes":" Notes "}]'
    );

    expect(slides).toEqual([
      {
        slideNumber: 2,
        title: " Intro ",
        bulletPoints: [" A ", ""],
        speakerNotes: " Notes "
      }
    ]);
  });

  it("normalizes slides before save", () => {
    const normalized = normalizeSlides([
      {
        slideNumber: 8,
        title: " Intro ",
        bulletPoints: [" A ", "", " B "],
        speakerNotes: " Notes "
      }
    ]);

    expect(normalized).toEqual([
      {
        slideNumber: 1,
        title: "Intro",
        bulletPoints: ["A", "B"],
        speakerNotes: "Notes"
      }
    ]);
  });

  it("rejects slides missing title, bullet points, or speaker notes", () => {
    expect(() =>
      validateSlides([
        { slideNumber: 1, title: "", bulletPoints: ["A"], speakerNotes: "N" }
      ])
    ).toThrow("Tiêu đề slide là bắt buộc.");
  });

  it("serializes normalized slides to JSON array string", () => {
    const json = serializeSlideOutline([
      { slideNumber: 1, title: "Intro", bulletPoints: ["A"], speakerNotes: "N" }
    ]);

    expect(json).toBe(
      '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]'
    );
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- --run frontend/src/utils/slideOutline.test.js`
Expected: FAIL because `slideOutline.js` does not exist yet.

- [ ] **Step 3: Write minimal helper implementation**

```js
export function parseSlideOutlineJson(value) {
  if (!value?.trim()) {
    return [];
  }

  const parsed = JSON.parse(value);
  if (!Array.isArray(parsed)) {
    throw new Error("Slide outline phải là một mảng slide.");
  }

  return parsed.map((slide, index) => ({
    slideNumber: Number(slide.slideNumber ?? index + 1),
    title: String(slide.title ?? ""),
    bulletPoints: Array.isArray(slide.bulletPoints) ? slide.bulletPoints.map(String) : [],
    speakerNotes: String(slide.speakerNotes ?? "")
  }));
}

export function normalizeSlides(slides) {
  return slides.map((slide, index) => ({
    slideNumber: index + 1,
    title: slide.title.trim(),
    bulletPoints: slide.bulletPoints.map((item) => item.trim()).filter(Boolean),
    speakerNotes: slide.speakerNotes.trim()
  }));
}

export function validateSlides(slides) {
  if (!slides.length) {
    throw new Error("Lesson phải có ít nhất một slide.");
  }

  for (const slide of normalizeSlides(slides)) {
    if (!slide.title) throw new Error("Tiêu đề slide là bắt buộc.");
    if (!slide.bulletPoints.length) throw new Error("Mỗi slide phải có ít nhất một bullet point.");
    if (!slide.speakerNotes) throw new Error("Speaker notes là bắt buộc.");
  }
}

export function serializeSlideOutline(slides) {
  validateSlides(slides);
  return JSON.stringify(normalizeSlides(slides));
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm run test -- --run frontend/src/utils/slideOutline.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/utils/slideOutline.js frontend/src/utils/slideOutline.test.js
git commit -m "feat: add slide outline helpers"
```

### Task 2: Build Structured SlideEditor Component

**Files:**
- Create: `frontend/src/components/course/SlideEditor.jsx`
- Create: `frontend/src/components/course/SlideEditor.test.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Write the failing component tests**

```jsx
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import SlideEditor from "./SlideEditor";

describe("SlideEditor", () => {
  it("renders slides and updates title", () => {
    const onChange = vi.fn();

    render(
      <SlideEditor
        slides={[{ slideNumber: 1, title: "Intro", bulletPoints: ["A"], speakerNotes: "N" }]}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.change(screen.getByLabelText("Tiêu đề slide 1"), {
      target: { value: "Overview" }
    });

    expect(onChange).toHaveBeenCalled();
  });

  it("adds and removes bullet points", () => {
    const onChange = vi.fn();

    render(
      <SlideEditor
        slides={[{ slideNumber: 1, title: "Intro", bulletPoints: ["A"], speakerNotes: "N" }]}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Thêm bullet point" }));
    fireEvent.click(screen.getByRole("button", { name: "Xóa bullet point 1-1" }));

    expect(onChange).toHaveBeenCalledTimes(2);
  });

  it("adds, removes, and reorders slides", () => {
    const onChange = vi.fn();

    render(
      <SlideEditor
        slides={[
          { slideNumber: 1, title: "One", bulletPoints: ["A"], speakerNotes: "N1" },
          { slideNumber: 2, title: "Two", bulletPoints: ["B"], speakerNotes: "N2" }
        ]}
        onChange={onChange}
        validationError=""
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Di chuyển xuống slide 1" }));
    fireEvent.click(screen.getByRole("button", { name: "Thêm slide" }));
    fireEvent.click(screen.getByRole("button", { name: "Xóa slide 2" }));

    expect(onChange).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- --run frontend/src/components/course/SlideEditor.test.jsx`
Expected: FAIL because `SlideEditor.jsx` does not exist.

- [ ] **Step 3: Write minimal SlideEditor implementation**

```jsx
import Button from "../ui/Button";
import FormField from "../ui/FormField";

export default function SlideEditor({ slides, onChange, validationError }) {
  function updateSlide(index, patch) {
    onChange(slides.map((slide, currentIndex) =>
      currentIndex === index ? { ...slide, ...patch } : slide
    ));
  }

  function addSlide() {
    onChange([
      ...slides,
      { slideNumber: slides.length + 1, title: "", bulletPoints: [""], speakerNotes: "" }
    ]);
  }

  return (
    <div className="slide-editor-stack">
      {slides.map((slide, index) => (
        <section className="surface-card slide-editor-card" key={`${slide.slideNumber}-${index}`}>
          <h4>Slide {index + 1}</h4>
          <FormField id={`slide-title-${index}`} label={`Tiêu đề slide ${index + 1}`}>
            <input
              className="ui-input"
              id={`slide-title-${index}`}
              value={slide.title}
              onChange={(event) => updateSlide(index, { title: event.target.value })}
            />
          </FormField>
        </section>
      ))}
      {validationError ? <p className="lesson-card__error">{validationError}</p> : null}
      <Button onClick={addSlide} variant="ghost">Thêm slide</Button>
    </div>
  );
}
```

- [ ] **Step 4: Add minimal styles for editor layout**

```css
.slide-editor-stack {
  display: grid;
  gap: 16px;
}

.slide-editor-card {
  display: grid;
  gap: 12px;
}

.slide-editor-bullets {
  display: grid;
  gap: 8px;
}

.slide-editor-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `npm run test -- --run frontend/src/components/course/SlideEditor.test.jsx`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/course/SlideEditor.jsx frontend/src/components/course/SlideEditor.test.jsx frontend/src/styles/theme.css
git commit -m "feat: add structured slide editor component"
```

### Task 3: Replace Raw Slide JSON Editing in LessonContentEditor

**Files:**
- Modify: `frontend/src/components/course/LessonContentEditor.jsx`
- Modify: `frontend/src/components/course/LessonContentEditor.test.jsx`
- Modify: `frontend/src/pages/CourseStructurePage.jsx`

- [ ] **Step 1: Write the failing integration test update for LessonContentEditor**

```jsx
it("edits generated lesson content with structured slide editor", () => {
  const onChange = vi.fn();
  const onSave = vi.fn();

  render(
    <LessonContentEditor
      form={{
        teachingScript: "Script",
        slideOutlineJson: '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]',
        voiceoverPlanJson: '{"tone":"clear"}'
      }}
      onChange={onChange}
      onCancel={() => {}}
      onSave={onSave}
    />
  );

  fireEvent.change(screen.getByLabelText("Tiêu đề slide 1"), {
    target: { value: "Overview" }
  });

  expect(onChange).toHaveBeenCalledWith(
    "slideOutlineJson",
    '[{"slideNumber":1,"title":"Overview","bulletPoints":["A"],"speakerNotes":"N"}]'
  );
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- --run frontend/src/components/course/LessonContentEditor.test.jsx`
Expected: FAIL because the component still renders a textarea for slide JSON.

- [ ] **Step 3: Refactor LessonContentEditor to use SlideEditor**

```jsx
import { parseSlideOutlineJson, serializeSlideOutline } from "../../utils/slideOutline";
import SlideEditor from "./SlideEditor";

const slides = safeParseSlides(form.slideOutlineJson);

<SlideEditor
  slides={slides}
  validationError={slideError}
  onChange={(nextSlides) => {
    try {
      onChange("slideOutlineJson", serializeSlideOutline(nextSlides));
      setSlideError("");
    } catch (error) {
      setSlideError(error.message);
    }
  }}
/>
```

- [ ] **Step 4: Keep save flow stable in CourseStructurePage**

```jsx
const updated = await updateLessonGeneratedContent(lessonId, generatedContentForm);
```

No API change here. Ensure only that the editor writes valid `slideOutlineJson` back into `generatedContentForm`.

- [ ] **Step 5: Run tests to verify they pass**

Run: `npm run test -- --run frontend/src/components/course/LessonContentEditor.test.jsx`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/course/LessonContentEditor.jsx frontend/src/components/course/LessonContentEditor.test.jsx frontend/src/pages/CourseStructurePage.jsx
git commit -m "feat: connect structured slide editor to lesson content form"
```

### Task 4: Render Structured Slide Preview

**Files:**
- Modify: `frontend/src/components/course/LessonContentPreview.jsx`
- Create or extend tests near preview usage, likely: `frontend/src/pages/CourseStructurePage.test.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Write the failing preview test**

```jsx
it("renders slide cards instead of raw JSON", async () => {
  render(<LessonContentPreview content={{
    teachingScript: "Script",
    slideOutlineJson: '[{"slideNumber":1,"title":"Intro","bulletPoints":["A"],"speakerNotes":"N"}]',
    voiceoverPlanJson: '{}'
  }} />);

  expect(screen.getByText("Slide 1")).toBeInTheDocument();
  expect(screen.getByText("Intro")).toBeInTheDocument();
  expect(screen.getByText("A")).toBeInTheDocument();
  expect(screen.queryByText('[{"slideNumber":1')).not.toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm run test -- --run CourseStructurePage.test.jsx`
Expected: FAIL because preview still uses `<pre>` with raw JSON.

- [ ] **Step 3: Implement structured preview rendering**

```jsx
const slides = tryParseSlides(content.slideOutlineJson);

<section className="surface-card lesson-generated-card">
  <h4>Slides</h4>
  {slides.length ? slides.map((slide) => (
    <article className="slide-preview-card" key={slide.slideNumber}>
      <strong>Slide {slide.slideNumber}</strong>
      <h5>{slide.title}</h5>
      <ul>{slide.bulletPoints.map((point) => <li key={point}>{point}</li>)}</ul>
      <p>{slide.speakerNotes}</p>
    </article>
  )) : <p>Chưa có slide outline.</p>}
</section>
```

- [ ] **Step 4: Add fallback behavior for invalid slide JSON**

```jsx
if (parseError) {
  return <p className="lesson-card__error">Slide outline JSON hiện tại không hợp lệ.</p>;
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `npm run test -- --run CourseStructurePage.test.jsx`
Expected: PASS for the updated preview expectations.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/course/LessonContentPreview.jsx frontend/src/pages/CourseStructurePage.test.jsx frontend/src/styles/theme.css
git commit -m "feat: render structured lesson slide preview"
```

### Task 5: Add Backend Validation for SlideOutlineJson

**Files:**
- Create: `backend/CourseVideo.API/Services/SlideOutlineValidation.cs`
- Modify: `backend/CourseVideo.API/Services/LessonService.cs`
- Create: `backend/CourseVideo.API.Tests/Services/SlideOutlineValidationTests.cs`
- Optionally modify: `backend/CourseVideo.API/Controllers/LessonsController.cs`

- [ ] **Step 1: Write the failing validation tests**

```csharp
[Fact]
public void ParseAndValidate_Throws_WhenJsonIsMalformed()
{
    var action = () => SlideOutlineValidation.ParseAndValidate("{bad json}");
    action.Should().Throw<InvalidOperationException>()
        .WithMessage("Slide outline JSON không hợp lệ.");
}

[Fact]
public void ParseAndValidate_Throws_WhenSlideMissingRequiredFields()
{
    var action = () => SlideOutlineValidation.ParseAndValidate(
        "[{\"slideNumber\":1,\"title\":\"\",\"bulletPoints\":[],\"speakerNotes\":\"\"}]"
    );

    action.Should().Throw<InvalidOperationException>()
        .WithMessage("Slide outline phải có title, bulletPoints và speakerNotes hợp lệ.");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter SlideOutlineValidationTests -v minimal`
Expected: FAIL because validation helper does not exist yet.

- [ ] **Step 3: Implement minimal validation helper**

```csharp
public static class SlideOutlineValidation
{
    public static void ParseAndValidate(string json)
    {
        try
        {
            var slides = JsonSerializer.Deserialize<List<SlideOutlineItem>>(json);
            if (slides is null || slides.Count == 0)
            {
                throw new InvalidOperationException("Slide outline phải có ít nhất một slide.");
            }

            if (slides.Any(slide =>
                string.IsNullOrWhiteSpace(slide.Title) ||
                slide.BulletPoints is null ||
                slide.BulletPoints.Count == 0 ||
                string.IsNullOrWhiteSpace(slide.SpeakerNotes)))
            {
                throw new InvalidOperationException("Slide outline phải có title, bulletPoints và speakerNotes hợp lệ.");
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Slide outline JSON không hợp lệ.", exception);
        }
    }
}
```

- [ ] **Step 4: Call the validator before persisting generated content**

```csharp
lesson.TeachingScript = request.TeachingScript.Trim();
SlideOutlineValidation.ParseAndValidate(request.SlideOutlineJson);
lesson.SlideOutlineJson = request.SlideOutlineJson.Trim();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter SlideOutlineValidationTests -v minimal`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Services/SlideOutlineValidation.cs backend/CourseVideo.API/Services/LessonService.cs backend/CourseVideo.API.Tests/Services/SlideOutlineValidationTests.cs
git commit -m "feat: validate slide outline json before persistence"
```

### Task 6: Full Verification and Cleanup

**Files:**
- Review: `frontend/src/components/course/SlideEditor.jsx`
- Review: `frontend/src/components/course/LessonContentEditor.jsx`
- Review: `frontend/src/components/course/LessonContentPreview.jsx`
- Review: `backend/CourseVideo.API/Services/LessonService.cs`

- [ ] **Step 1: Run focused frontend test suite**

Run:
```bash
npm run test -- --run frontend/src/utils/slideOutline.test.js
npm run test -- --run frontend/src/components/course/SlideEditor.test.jsx
npm run test -- --run frontend/src/components/course/LessonContentEditor.test.jsx
npm run test -- --run frontend/src/pages/CourseStructurePage.test.jsx
```
Expected: PASS.

- [ ] **Step 2: Run backend validation tests**

Run:
```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "SlideOutlineValidationTests" -v minimal
```
Expected: PASS.

- [ ] **Step 3: Build production artifacts**

Run:
```bash
npm run build --prefix frontend
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj
```
Expected: successful frontend and backend builds.

- [ ] **Step 4: Manual admin smoke test**

Verify on `/admin/courses/:courseId`:
- open a lesson with AI content
- preview shows slide cards, not raw JSON
- edit slide title/bullets/notes
- add and remove slides
- save generated content
- reload page and confirm saved slide structure persists

- [ ] **Step 5: Commit final integration changes**

```bash
git add frontend backend
git commit -m "feat: add structured lesson slide editor"
```

---

## Self-Review

### Spec Coverage
- Structured preview: covered in Task 4.
- Structured editor: covered in Tasks 2 and 3.
- Existing backend storage preserved: covered in Tasks 1, 3, and 5.
- Client-side validation: covered in Task 1.
- Optional backend hardening: covered in Task 5.
- Testing: covered in Tasks 1, 2, 4, 5, and 6.

### Placeholder Scan
- No `TODO` or `TBD` placeholders remain.
- All tasks include exact file paths.
- Each code-changing step includes concrete code or command examples.

### Type Consistency
- Shared field names are consistent across plan steps: `slideNumber`, `title`, `bulletPoints`, `speakerNotes`, `slideOutlineJson`.
- Frontend keeps JSON string persistence model aligned with backend request shape.
