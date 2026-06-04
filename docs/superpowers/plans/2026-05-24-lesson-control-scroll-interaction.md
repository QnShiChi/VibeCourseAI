# Lesson Control Scroll Interaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the `Điều khiển` button on each lesson card select that lesson in the centralized lesson panel, smoothly scroll the page to the panel, and briefly highlight the panel so the admin can immediately see the active control area.

**Architecture:** Keep the change inside `CourseStructurePage` by extending the existing `handleControlLesson(lessonId)` interaction rather than restructuring the page. Add one short-lived focus state for the centralized panel, verify the behavior with a failing UI test first, then apply the existing focused-panel CSS class during the brief highlight window.

**Tech Stack:** React, Vitest, React Testing Library, CSS in `frontend/src/styles/theme.css`

---

## File structure

- `frontend/src/pages/CourseStructurePage.jsx`
  - Owns lesson selection state, the `handleControlLesson(lessonId)` click path, and the centralized lesson panel markup.
  - Will gain a short-lived `isPanelFocused` state plus a timeout-based reset.
- `frontend/src/pages/CourseStructurePage.test.jsx`
  - Owns page-level interaction tests.
  - Will gain one focused regression test that proves click → select → scroll → highlight.
- `frontend/src/styles/theme.css`
  - Already defines `.centralized-panel--focused`.
  - May need a small adjustment only if the class is not applied by the component in a testable way.

### Task 1: Add the failing lesson control interaction test

**Files:**
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`
- Test: `frontend/src/pages/CourseStructurePage.test.jsx`

- [ ] **Step 1: Write the failing test**

Add this test near the existing lesson interaction tests in `frontend/src/pages/CourseStructurePage.test.jsx`:

```jsx
it("selects, scrolls to, and highlights the centralized panel when controlling a lesson", async () => {
  const scrollIntoView = vi.fn();
  Object.defineProperty(HTMLElement.prototype, "scrollIntoView", {
    configurable: true,
    value: scrollIntoView
  });

  mockGetCourseStructure.mockResolvedValue({
    ...baseCourse,
    modules: [
      {
        ...baseCourse.modules[0],
        lessons: [
          {
            ...baseCourse.modules[0].lessons[0],
            title: "Bai 1"
          },
          {
            ...baseCourse.modules[0].lessons[0],
            id: "lesson-2",
            orderIndex: 2,
            title: "Bai 2"
          }
        ]
      }
    ]
  });

  renderPage();

  const controlButtons = await screen.findAllByRole("button", { name: "Điều khiển" });
  fireEvent.click(controlButtons[1]);

  await waitFor(() => {
    expect(screen.getByRole("heading", { name: "Bài 2: Bai 2" })).toBeInTheDocument();
  });

  expect(scrollIntoView).toHaveBeenCalledWith({ behavior: "smooth", block: "start" });
  expect(document.getElementById("centralized-lesson-action-panel")).toHaveClass("centralized-panel--focused");
});
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
npm test -- frontend/src/pages/CourseStructurePage.test.jsx -t "selects, scrolls to, and highlights the centralized panel when controlling a lesson"
```

Expected: FAIL because the panel is scrolled and the lesson is selected, but the panel does not yet receive the `centralized-panel--focused` class in the rendered DOM.

- [ ] **Step 3: Commit the failing test only if your workflow explicitly allows red commits**

Do not commit if the repository expects green commits only. If red commits are allowed, use:

```bash
git add frontend/src/pages/CourseStructurePage.test.jsx
git commit -m "test: cover lesson control panel focus"
```

### Task 2: Implement the minimal panel focus behavior

**Files:**
- Modify: `frontend/src/pages/CourseStructurePage.jsx`
- Modify: `frontend/src/styles/theme.css`
- Test: `frontend/src/pages/CourseStructurePage.test.jsx`

- [ ] **Step 1: Add short-lived focus state in the page component**

In `frontend/src/pages/CourseStructurePage.jsx`, extend the React import and add one state plus one ref-backed timeout handle near the existing centralized panel state:

```jsx
import { useEffect, useRef, useState } from "react";
```

```jsx
const [isPanelFocused, setIsPanelFocused] = useState(false);
const panelFocusTimerRef = useRef(null);
```

- [ ] **Step 2: Add a cleanup effect for the focus timeout**

In `frontend/src/pages/CourseStructurePage.jsx`, place this effect near the existing effects so the timeout is cleared on unmount:

```jsx
useEffect(() => {
  return () => {
    if (panelFocusTimerRef.current) {
      window.clearTimeout(panelFocusTimerRef.current);
    }
  };
}, []);
```

- [ ] **Step 3: Update `handleControlLesson(lessonId)` with focus timing**

Replace the current function body in `frontend/src/pages/CourseStructurePage.jsx` with this minimal behavior:

```jsx
function handleControlLesson(lessonId) {
  handleSelectLesson(lessonId);
  setIsPanelFocused(true);

  if (panelFocusTimerRef.current) {
    window.clearTimeout(panelFocusTimerRef.current);
  }

  panelFocusTimerRef.current = window.setTimeout(() => {
    setIsPanelFocused(false);
    panelFocusTimerRef.current = null;
  }, 1200);

  const panelElement = document.getElementById("centralized-lesson-action-panel");
  if (panelElement) {
    panelElement.scrollIntoView({ behavior: "smooth", block: "start" });
  }
}
```

- [ ] **Step 4: Apply the focus class to the centralized panel**

Update the centralized panel card in `frontend/src/pages/CourseStructurePage.jsx` so the class name includes the focused modifier only while the timer is active:

```jsx
<Card
  id="centralized-lesson-action-panel"
  className={`centralized-panel ${isPanelFocused ? "centralized-panel--focused" : ""}`.trim()}
  variant="shadowed"
>
```

- [ ] **Step 5: Keep CSS unchanged unless the existing focus class is missing in the DOM state**

Check `frontend/src/styles/theme.css`. If the current class definitions already exist exactly like this, do not edit the file:

```css
@keyframes panel-focus-flash {
  0%   { box-shadow: 8px 8px 0 rgba(0,0,0,0.08), 0 0 0 0px rgba(132, 204, 22, 0); }
  30%  { box-shadow: 8px 8px 0 rgba(0,0,0,0.08), 0 0 0 8px rgba(132, 204, 22, 0.45); border-color: #84cc16; }
  60%  { box-shadow: 8px 8px 0 rgba(0,0,0,0.08), 0 0 0 5px rgba(132, 204, 22, 0.25); border-color: #84cc16; }
  100% { box-shadow: 8px 8px 0 rgba(0,0,0,0.08), 0 0 0 0px rgba(132, 204, 22, 0); }
}

.centralized-panel--focused {
  animation: panel-focus-flash 1.2s ease-out forwards;
  border-color: #84cc16 !important;
}
```

If the file has drifted, restore just this block and nothing more.

- [ ] **Step 6: Run the focused test to verify it passes**

Run:

```bash
npm test -- frontend/src/pages/CourseStructurePage.test.jsx -t "selects, scrolls to, and highlights the centralized panel when controlling a lesson"
```

Expected: PASS.

- [ ] **Step 7: Commit the minimal implementation**

```bash
git add frontend/src/pages/CourseStructurePage.jsx frontend/src/pages/CourseStructurePage.test.jsx frontend/src/styles/theme.css
git commit -m "fix: focus centralized lesson panel on control"
```

### Task 3: Run the broader regression check for the page

**Files:**
- Test: `frontend/src/pages/CourseStructurePage.test.jsx`

- [ ] **Step 1: Run the full page test file**

Run:

```bash
npm test -- frontend/src/pages/CourseStructurePage.test.jsx
```

Expected: PASS for the new lesson control interaction test and the existing course structure page tests.

- [ ] **Step 2: Inspect for accidental tab reset or broken selection behavior**

Verify from the test output and final code review that:

```text
- `handleControlLesson(lessonId)` still calls `handleSelectLesson(lessonId)`
- `activeTab` is never reset inside the new focus behavior
- missing panel DOM still cannot throw because the code keeps `if (panelElement)`
```

Expected: all three conditions are true.

- [ ] **Step 3: Commit only if Task 2 was intentionally left uncommitted**

If you already created the green commit in Task 2, skip this step. Otherwise:

```bash
git add frontend/src/pages/CourseStructurePage.jsx frontend/src/pages/CourseStructurePage.test.jsx frontend/src/styles/theme.css
git commit -m "fix: focus centralized lesson panel on control"
```

## Self-review against spec

- Spec coverage:
  - Select lesson in panel: covered in Task 1 assertion and Task 2 implementation.
  - Smooth scroll to panel: covered in Task 1 assertion and Task 2 `scrollIntoView` call.
  - Temporary highlight: covered in Task 1 assertion and Task 2 focus-state timer.
  - Keep current tab: covered in Task 3 inspection step and no `activeTab` changes in Task 2.
  - No crash if panel DOM missing: covered in Task 2 existing guard retention and Task 3 inspection step.
- Placeholder scan:
  - No `TODO`, `TBD`, or “implement later” placeholders remain.
  - Commands, code, expected outcomes, and file paths are explicit.
- Type consistency:
  - `isPanelFocused`, `panelFocusTimerRef`, and `centralized-panel--focused` are named consistently across component, test, and CSS.
