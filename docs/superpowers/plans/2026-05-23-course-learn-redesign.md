# Course Learn Page Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `courses/:courseId/learn` to match the approved learning-page mockup while preserving lesson selection, video playback, comments, progress, and previous/next navigation.

**Architecture:** Keep `frontend/src/pages/CourseLearnPage.jsx` as the orchestration component, but add small local helpers for flattening lessons and computing navigation/progress so the rendering code stays readable. Rework the page structure and `theme.css` learn-page section together, then verify behavior through focused `vitest` tests on lesson switching, progress text, and previous/next controls.

**Tech Stack:** React 18, React Router 6, Vite, Vitest, Testing Library, shared theme tokens in `frontend/src/styles/theme.css`

---

## File Structure

- Modify: `frontend/src/pages/CourseLearnPage.jsx`
  Responsibility: derive current lesson/module/progress/navigation state and render the new learn-page layout.
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`
  Responsibility: cover the redesigned page behavior, especially progress and previous/next interactions.
- Modify: `frontend/src/styles/theme.css`
  Responsibility: replace the current learn-page styles with the approved two-column sticky layout and responsive/mobile behavior.
- Reference: `frontend/src/components/comments/LessonComments.jsx`
  Responsibility: existing lesson-scoped comments component that must continue receiving the selected `lessonId`.
- Reference: `frontend/src/components/layout/MainLayout.jsx`
  Responsibility: provides the sticky site header with `min-height: 78px`, which informs the sidebar sticky offset.

### Task 1: Lock Down Learn-Page Behavior With Tests

**Files:**
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`
- Reference: `frontend/src/pages/CourseLearnPage.jsx`

- [ ] **Step 1: Expand the test payload helper to support multi-lesson navigation**

Update `buildLearnPayload()` so tests can reuse one payload with multiple lessons in one module.

```jsx
function buildLearnPayload() {
  return {
    courseId: "course-1",
    courseTitle: "TRÍ TUỆ NHÂN TẠO ỨNG DỤNG",
    courseDescription: "Desc",
    selectedLessonId: "lesson-1",
    selectedLesson: {
      lessonId: "lesson-1",
      lessonTitle: "Tổng quan về AI",
      description: "Mo dau",
      contentSeed: "Noi dung lesson 1",
      videoUrl: "",
      videoGenerationStatus: "NotGenerated",
      videoGenerationError: "",
      orderIndex: 1
    },
    modules: [
      {
        moduleId: "module-1",
        moduleTitle: "Định nghĩa và Lịch sử",
        moduleDescription: "M1",
        orderIndex: 2,
        lessons: [
          {
            lessonId: "lesson-1",
            lessonTitle: "Tổng quan về AI",
            description: "Mo dau",
            contentSeed: "Noi dung lesson 1",
            videoUrl: "",
            videoGenerationStatus: "NotGenerated",
            videoGenerationError: "",
            orderIndex: 1
          },
          {
            lessonId: "lesson-2",
            lessonTitle: "Các mốc lịch sử",
            description: "Tiep theo",
            contentSeed: "Noi dung lesson 2",
            videoUrl: "",
            videoGenerationStatus: "NotGenerated",
            videoGenerationError: "",
            orderIndex: 2
          }
        ]
      }
    ]
  };
}
```

- [ ] **Step 2: Add a failing test for progress text and next-lesson navigation**

Append this test near the end of `CourseLearnPage.test.jsx`.

```jsx
it("shows progress and moves to the next lesson from the footer navigation", async () => {
  mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

  render(
    <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
      <Routes>
        <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
      </Routes>
    </MemoryRouter>
  );

  expect(await screen.findByText(/Tiến độ: 50%/i)).toBeInTheDocument();
  expect(screen.getByRole("button", { name: /Bài trước/i })).toBeDisabled();

  fireEvent.click(screen.getByRole("button", { name: /Tiếp tục bài học/i }));

  expect(await screen.findByText("Noi dung lesson 2")).toBeInTheDocument();
  expect(await screen.findByText(/Tiến độ: 100%/i)).toBeInTheDocument();
});
```

- [ ] **Step 3: Add a failing test for previous-lesson navigation and sidebar heading**

Append this second test after the previous one.

```jsx
it("moves back to the previous lesson and renders the course content heading", async () => {
  const payload = buildLearnPayload();
  payload.selectedLessonId = "lesson-2";
  payload.selectedLesson = payload.modules[0].lessons[1];
  mockGetCourseLearnPayload.mockResolvedValue(payload);

  render(
    <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
      <Routes>
        <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
      </Routes>
    </MemoryRouter>
  );

  expect(await screen.findByRole("heading", { name: /Nội dung khóa học/i })).toBeInTheDocument();
  expect(await screen.findByText(/Tiến độ: 100%/i)).toBeInTheDocument();

  fireEvent.click(screen.getByRole("button", { name: /Bài trước/i }));

  expect(await screen.findByText("Noi dung lesson 1")).toBeInTheDocument();
  expect(await screen.findByText(/Tiến độ: 50%/i)).toBeInTheDocument();
});
```

- [ ] **Step 4: Run the targeted test file and verify it fails**

Run:

```bash
npm run test -- src/pages/CourseLearnPage.test.jsx --run
```

Expected:

```text
FAIL  src/pages/CourseLearnPage.test.jsx
TestingLibraryElementError: Unable to find an element with the text: /Tiến độ: 50%/i
```

- [ ] **Step 5: Commit the failing-test checkpoint**

```bash
git add frontend/src/pages/CourseLearnPage.test.jsx
git commit -m "test: cover learn page navigation and progress"
```

### Task 2: Implement Derived Lesson Navigation And New Learn-Page Markup

**Files:**
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
- Test: `frontend/src/pages/CourseLearnPage.test.jsx`

- [ ] **Step 1: Add small helpers and derived state for lesson order, progress, and adjacent lessons**

Insert the helper functions near the top of `CourseLearnPage.jsx`, above the component.

```jsx
function sortLessons(items) {
  return [...items].sort((left, right) => left.orderIndex - right.orderIndex);
}

function flattenLessons(modules) {
  return sortLessons(modules).flatMap((module) =>
    sortLessons(module.lessons).map((lesson) => ({
      ...lesson,
      moduleId: module.moduleId,
      moduleTitle: module.moduleTitle,
      moduleOrderIndex: module.orderIndex
    }))
  );
}

function buildExpandedModuleState(modules, selectedLessonId) {
  const defaults = {};
  modules.forEach((module, index) => {
    defaults[module.moduleId] = index === 0 || module.lessons.some((lesson) => lesson.lessonId === selectedLessonId);
  });
  return defaults;
}
```

- [ ] **Step 2: Replace the current selected-lesson derivation with computed navigation state**

Update the middle of the component, after `handleSelectLesson`, to derive the main page state from `course.modules`.

```jsx
  const modules = sortLessons(course?.modules ?? []);
  const flatLessons = flattenLessons(modules);
  const selectedLesson =
    flatLessons.find((lesson) => lesson.lessonId === selectedLessonId) ?? course?.selectedLesson ?? null;
  const selectedModule =
    modules.find((module) => module.moduleId === selectedLesson?.moduleId) ??
    modules.find((module) => module.lessons.some((lesson) => lesson.lessonId === selectedLessonId)) ??
    null;
  const currentLessonIndex = flatLessons.findIndex((lesson) => lesson.lessonId === selectedLessonId);
  const totalLessons = flatLessons.length;
  const progressPercent =
    currentLessonIndex >= 0 && totalLessons ? Math.round(((currentLessonIndex + 1) / totalLessons) * 100) : 0;
  const previousLesson = currentLessonIndex > 0 ? flatLessons[currentLessonIndex - 1] : null;
  const nextLesson = currentLessonIndex >= 0 && currentLessonIndex < totalLessons - 1 ? flatLessons[currentLessonIndex + 1] : null;
```

- [ ] **Step 3: Add a shared navigation handler for footer buttons**

Place this function next to `handleSelectLesson`.

```jsx
  function handleNavigateLesson(targetLesson) {
    if (!targetLesson) {
      return;
    }

    setSelectedLessonId(targetLesson.lessonId);
    setExpandedModules((current) => ({
      ...current,
      [targetLesson.moduleId]: true
    }));
  }
```

- [ ] **Step 4: Update `loadCourse()` to initialize expanded modules through the helper**

Replace the `defaults` logic in `loadCourse()` with:

```jsx
      setCourse(data);
      setSelectedLessonId(data.selectedLessonId);
      setExpandedModules(buildExpandedModuleState(sortLessons(data.modules ?? []), data.selectedLessonId));
```

- [ ] **Step 5: Replace the page JSX with the redesigned layout**

Replace the current success-state branch with this structure.

```jsx
        <div className="learn-shell">
          <div className="learn-layout">
            <div className="learn-layout__main">
              <section className="learn-hero">
                <p className="learn-hero__eyebrow">Đang học</p>
                <h1>{course.courseTitle}</h1>
                <p>{course.courseDescription}</p>
              </section>

              <article className="learn-stage-card">
                <div className="learn-stage-card__media">
                  <span className="learn-stage-card__badge">
                    {selectedModule ? `${selectedModule.orderIndex}.${selectedLesson.orderIndex}` : `Bài ${currentLessonIndex + 1}`}
                  </span>
                  {selectedLesson.videoUrl ? (
                    <video controls preload="metadata" src={selectedLesson.videoUrl}>
                      Trình duyệt của bạn không hỗ trợ phát video.
                    </video>
                  ) : (
                    <div className="learn-stage-card__placeholder">
                      <strong>{selectedLesson.lessonTitle}</strong>
                      <span>
                        {selectedLesson.videoGenerationStatus === "Failed"
                          ? "Video lesson đang lỗi, vui lòng thử lại sau."
                          : "Bài học đang được chuẩn bị video."}
                      </span>
                    </div>
                  )}
                </div>

                <div className="learn-stage-card__summary">
                  <h2>{selectedLesson.lessonTitle}</h2>
                  <p>{selectedLesson.description}</p>
                </div>
              </article>

              <Card className="learn-reading-card" variant="shadowed">
                <h2>Nội dung bài học</h2>
                <pre className="text-preview learn-content-preview">{selectedLesson.contentSeed}</pre>
              </Card>

              <Card className="learn-comments-card" variant="shadowed">
                <LessonComments isAdmin={isAdmin} lessonId={selectedLesson.lessonId} />
              </Card>

              <div className="learn-footer-nav">
                <button disabled={!previousLesson} onClick={() => handleNavigateLesson(previousLesson)} type="button">
                  Bài trước
                </button>
                <p>
                  <span>Đang học:</span> {selectedLesson.lessonTitle}
                </p>
                <button disabled={!nextLesson} onClick={() => handleNavigateLesson(nextLesson)} type="button">
                  Tiếp tục bài học
                </button>
              </div>
            </div>

            <aside className="learn-sidebar-panel">
              <div className="learn-sidebar-panel__inner">
                <div className="learn-sidebar-panel__header">
                  <h2>Nội dung khóa học</h2>
                  <div className="learn-progress">
                    <div aria-hidden="true" className="learn-progress__track">
                      <span className="learn-progress__value" style={{ width: `${progressPercent}%` }} />
                    </div>
                    <p>Tiến độ: {progressPercent}%</p>
                  </div>
                </div>

                <div className="learn-sidebar-panel__modules">
                  {modules.map((module) => {
                    const isExpanded = Boolean(expandedModules[module.moduleId]);
                    return (
                      <div className="learn-module" key={module.moduleId}>
                        <button
                          aria-expanded={isExpanded}
                          className={`learn-module__header${isExpanded ? " learn-module__header--expanded" : ""}`}
                          onClick={() => handleToggleModule(module.moduleId)}
                          type="button"
                        >
                          <div>
                            <strong>{module.orderIndex}. {module.moduleTitle}</strong>
                            <span>{module.lessons.length} bài học</span>
                          </div>
                          <span>{isExpanded ? "⌃" : "⌄"}</span>
                        </button>

                        {isExpanded ? (
                          <div className="learn-module__lessons">
                            {sortLessons(module.lessons).map((lesson) => (
                              <button
                                className={`learn-lesson-button${selectedLessonId === lesson.lessonId ? " learn-lesson-button--active" : ""}`}
                                key={lesson.lessonId}
                                onClick={() => handleSelectLesson(module.moduleId, lesson.lessonId)}
                                type="button"
                              >
                                <span className="learn-lesson-button__index">{String(lesson.orderIndex).padStart(2, "0")}</span>
                                <strong>{lesson.lessonTitle}</strong>
                              </button>
                            ))}
                          </div>
                        ) : null}
                      </div>
                    );
                  })}
                </div>
              </div>
            </aside>
          </div>
        </div>
```

- [ ] **Step 6: Run the targeted test file and verify it passes**

Run:

```bash
npm run test -- src/pages/CourseLearnPage.test.jsx --run
```

Expected:

```text
PASS  src/pages/CourseLearnPage.test.jsx
5 passed
```

- [ ] **Step 7: Commit the page-behavior implementation**

```bash
git add frontend/src/pages/CourseLearnPage.jsx frontend/src/pages/CourseLearnPage.test.jsx
git commit -m "feat: redesign learn page behavior"
```

### Task 3: Replace Learn-Page Styling With Sticky Sidebar And Responsive Layout

**Files:**
- Modify: `frontend/src/styles/theme.css`
- Test: `frontend/src/pages/CourseLearnPage.test.jsx`

- [ ] **Step 1: Replace the existing `.learn-*` block in `theme.css` with the new layout styles**

Replace the current learn-page section beginning at `.learn-layout` with:

```css
.learn-shell {
  display: grid;
  gap: var(--spacing-24);
}

.learn-layout {
  display: grid;
  grid-template-columns: minmax(0, 1.6fr) minmax(320px, 420px);
  gap: var(--spacing-32);
  align-items: start;
}

.learn-layout__main {
  display: grid;
  gap: var(--spacing-24);
}

.learn-hero {
  display: grid;
  gap: 12px;
}

.learn-hero__eyebrow {
  margin: 0;
  font-size: var(--text-body-sm);
  font-weight: 700;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.learn-hero h1 {
  margin: 0;
  font-size: clamp(2.4rem, 4vw, 4rem);
  line-height: 0.98;
  letter-spacing: -0.06em;
  text-transform: uppercase;
}

.learn-hero p {
  margin: 0;
  max-width: 68ch;
}

.learn-stage-card,
.learn-reading-card,
.learn-comments-card {
  border: 2px solid var(--color-charcoal-border);
  border-radius: 24px;
  background: var(--color-canvas-white);
  box-shadow: 4px 4px 0 0 var(--color-shadow-base);
}

.learn-stage-card {
  padding: 24px;
  display: grid;
  gap: 24px;
}

.learn-stage-card__media {
  position: relative;
  overflow: hidden;
  border: 2px solid var(--color-charcoal-border);
  border-radius: 24px;
  background: #091106;
  min-height: 360px;
}

.learn-stage-card__media video {
  width: 100%;
  min-height: 360px;
  display: block;
  background: #000;
}

.learn-stage-card__badge {
  position: absolute;
  top: 20px;
  left: 20px;
  z-index: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-height: 36px;
  padding: 0 16px;
  border: 2px solid var(--color-charcoal-border);
  border-radius: 8px;
  background: var(--color-accent-green);
  font-weight: 700;
}

.learn-stage-card__placeholder {
  min-height: 360px;
  display: grid;
  place-items: center;
  gap: 12px;
  padding: 32px;
  text-align: center;
  color: var(--color-canvas-white);
}

.learn-stage-card__summary {
  display: grid;
  gap: 12px;
}

.learn-stage-card__summary h2,
.learn-reading-card h2,
.learn-sidebar-panel__header h2 {
  margin: 0;
}

.learn-stage-card__summary p,
.learn-sidebar-panel__header p {
  margin: 0;
}

.learn-reading-card,
.learn-comments-card {
  padding: 24px;
}

.learn-content-preview {
  margin: 16px 0 0;
  max-height: none;
  white-space: pre-wrap;
}

.learn-sidebar-panel {
  position: sticky;
  top: 110px;
}

.learn-sidebar-panel__inner {
  height: calc(100vh - 134px);
  min-height: 520px;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr);
  border-left: 2px solid var(--color-charcoal-border);
  background: linear-gradient(180deg, #f8faeb 0%, #f4f7d9 100%);
}

.learn-sidebar-panel__header {
  padding: 32px;
  display: grid;
  gap: 20px;
  border-bottom: 2px solid var(--color-charcoal-border);
}

.learn-progress {
  display: grid;
  gap: 12px;
}

.learn-progress__track {
  height: 14px;
  overflow: hidden;
  border: 2px solid var(--color-charcoal-border);
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.75);
}

.learn-progress__value {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--color-accent-green);
}

.learn-progress p {
  font-weight: 700;
  text-transform: uppercase;
}

.learn-sidebar-panel__modules {
  overflow-y: auto;
  padding: 24px 28px 32px;
  display: grid;
  gap: 18px;
}

.learn-module {
  overflow: hidden;
  border: 2px solid var(--color-charcoal-border);
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.86);
}

.learn-module__header {
  width: 100%;
  padding: 18px 20px;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  border: 0;
  background: transparent;
  cursor: pointer;
  text-align: left;
}

.learn-module__header--expanded {
  background: rgba(163, 230, 53, 0.92);
}

.learn-module__header div {
  display: grid;
  gap: 6px;
}

.learn-module__lessons {
  display: grid;
  gap: 10px;
  padding: 0 18px 18px;
}

.learn-lesson-button {
  width: 100%;
  padding: 16px 18px;
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 14px;
  align-items: center;
  border: 2px solid var(--color-charcoal-border);
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
  text-align: left;
}

.learn-lesson-button__index {
  width: 34px;
  height: 34px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  background: #eff3df;
  font-size: 0.875rem;
  font-weight: 700;
}

.learn-lesson-button--active {
  background: var(--color-canvas-white);
}

.learn-lesson-button--active .learn-lesson-button__index {
  background: #4b6d00;
  color: var(--color-canvas-white);
}

.learn-footer-nav {
  display: grid;
  grid-template-columns: minmax(0, 180px) minmax(0, 1fr) minmax(0, 240px);
  gap: 24px;
  align-items: center;
  padding: 16px 0 8px;
}

.learn-footer-nav button {
  min-height: 64px;
  padding: 12px 24px;
  border: 2px solid var(--color-charcoal-border);
  border-radius: 16px;
  background: var(--color-canvas-white);
  box-shadow: 4px 4px 0 0 var(--color-shadow-base);
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.learn-footer-nav button:last-child {
  background: var(--color-accent-green);
}

.learn-footer-nav button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.learn-footer-nav p {
  margin: 0;
  text-align: center;
  font-weight: 700;
}

.learn-footer-nav p span {
  margin-right: 8px;
  font-style: italic;
  text-transform: uppercase;
}

@media (max-width: 1100px) {
  .learn-layout {
    grid-template-columns: 1fr;
  }

  .learn-sidebar-panel {
    position: static;
  }

  .learn-sidebar-panel__inner {
    height: auto;
    min-height: 0;
    border: 2px solid var(--color-charcoal-border);
    border-radius: 24px;
    box-shadow: 4px 4px 0 0 var(--color-shadow-base);
  }

  .learn-sidebar-panel__modules {
    max-height: 420px;
  }
}

@media (max-width: 720px) {
  .learn-stage-card,
  .learn-reading-card,
  .learn-comments-card,
  .learn-sidebar-panel__inner {
    border-radius: 20px;
  }

  .learn-stage-card,
  .learn-reading-card,
  .learn-comments-card,
  .learn-sidebar-panel__header {
    padding: 20px;
  }

  .learn-stage-card__media,
  .learn-stage-card__media video,
  .learn-stage-card__placeholder {
    min-height: 260px;
  }

  .learn-footer-nav {
    grid-template-columns: 1fr;
  }

  .learn-footer-nav p {
    order: -1;
  }
}
```

- [ ] **Step 2: Run the targeted tests again after the CSS rewrite**

Run:

```bash
npm run test -- src/pages/CourseLearnPage.test.jsx --run
```

Expected:

```text
PASS  src/pages/CourseLearnPage.test.jsx
5 passed
```

- [ ] **Step 3: Run the full frontend test suite for regression coverage**

Run:

```bash
npm run test -- --run
```

Expected:

```text
PASS  All tests passed
```

- [ ] **Step 4: Run a production build to verify the redesigned page compiles cleanly**

Run:

```bash
npm run build
```

Expected:

```text
vite v5...
✓ built in ...
```

- [ ] **Step 5: Commit the styling and verification pass**

```bash
git add frontend/src/styles/theme.css frontend/src/pages/CourseLearnPage.jsx frontend/src/pages/CourseLearnPage.test.jsx
git commit -m "style: redesign course learn layout"
```

## Self-Review

### Spec Coverage

- New two-column desktop layout: covered in Task 2 Step 5 and Task 3 Step 1.
- Sticky sidebar with internal module scroll: covered in Task 3 Step 1.
- Mobile first learning flow: covered in Task 3 Step 1 responsive blocks.
- Progress calculation: covered in Task 2 Step 2 and validated in Task 1 tests.
- Previous/next lesson navigation: covered in Task 2 Step 3 and Task 1 tests.
- Selected lesson still drives comments/video/content: covered in Task 2 Step 5 and existing/expanded tests.

### Placeholder Scan

- No `TODO`, `TBD`, or “implement later” placeholders remain.
- Every code-changing step includes concrete snippets.
- Every verification step includes an exact command and expected outcome.

### Type Consistency

- The plan consistently uses `selectedLessonId`, `flatLessons`, `progressPercent`, `previousLesson`, and `nextLesson`.
- Helper code and JSX both expect `moduleId`, `orderIndex`, and `lessonId`, matching the current payload shape in the existing tests.
