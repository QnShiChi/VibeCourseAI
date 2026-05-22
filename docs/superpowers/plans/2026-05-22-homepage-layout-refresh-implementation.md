# Homepage Layout Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the homepage body to match the approved landing-page layout direction while keeping the current system header/footer unchanged and preserving the carousel directly under the hero.

**Architecture:** Keep the homepage as a single page-level composition in `HomePage.jsx`, replace the old feature-stack body with new hero/grid/stats/CTA sections, and drive the visual structure primarily through `HomePage.module.css`. Reuse `CarouselSection` as an existing interactive unit and adapt only its outer presentation needs through homepage-level wrappers or minimal copy changes if needed.

**Tech Stack:** React 18, React Router, Vite, Vitest, Testing Library, CSS Modules

---

## File Map

- Modify: `frontend/src/pages/HomePage.jsx`
  - Replace the old feature-section composition with the new hero, carousel wrapper, content grid, stats band, and CTA panel.
- Modify: `frontend/src/styles/HomePage.module.css`
  - Remove obsolete homepage-specific marketing section styles that are no longer used and add the new layout, spacing, card, hero, stats, and CTA styles.
- Modify: `frontend/src/pages/HomePage.test.jsx`
  - Rewrite tests to assert the new homepage structure and the required carousel placement under the hero.
- Optional verify-only read: `frontend/src/components/sections/CarouselSection.jsx`
  - Confirm the carousel region label and behavior still work inside the new homepage flow.

## Task 1: Rewrite Homepage Tests Around the New Layout

**Files:**
- Modify: `frontend/src/pages/HomePage.test.jsx`
- Read: `frontend/src/pages/HomePage.jsx`

- [ ] **Step 1: Write the failing homepage layout assertions**

```jsx
import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import HomePage from "./HomePage";

describe("HomePage", () => {
  it("renders the refreshed hero and keeps the carousel directly below it", () => {
    const { container } = render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText(/Tạo khóa học AI-ready/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Bắt đầu miễn phí/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Xem khóa học/i })).toBeInTheDocument();

    const orderedSections = Array.from(container.querySelectorAll("section"));
    expect(orderedSections[0]).toHaveTextContent(/Tạo khóa học AI-ready/i);
    expect(within(orderedSections[1]).getByRole("region", { name: /showcase carousel/i })).toBeInTheDocument();
  });

  it("renders the tools grid, stats band, and final CTA sections", () => {
    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText(/Công cụ cho toàn bộ pipeline khóa học/i)).toBeInTheDocument();
    expect(screen.getByText(/Import syllabus thông minh/i)).toBeInTheDocument();
    expect(screen.getByText(/Tăng tốc vận hành nội dung học tập/i)).toBeInTheDocument();
    expect(screen.getByText(/Sẵn sàng đưa khóa học lên production/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the focused homepage test to verify it fails**

Run: `cd frontend && npx vitest run src/pages/HomePage.test.jsx`
Expected: FAIL because the current homepage still renders the old feature-stack content and section order.

- [ ] **Step 3: Confirm the failure is the expected one**

Expected failure shape:
- missing new hero headline / CTA names
- missing new tools-grid wording
- section order does not show carousel directly after the hero

Do not proceed until the failure is caused by missing new layout behavior rather than syntax or test mistakes.

## Task 2: Replace the Old Homepage Composition With the New Section Structure

**Files:**
- Modify: `frontend/src/pages/HomePage.jsx`
- Read: `frontend/src/components/sections/CarouselSection.jsx`

- [ ] **Step 1: Replace the old data-heavy feature section arrays with explicit homepage sections**

Implement a page structure like this:

```jsx
import { Link } from "react-router-dom";
import CarouselSection from "../components/sections/CarouselSection";
import Button from "../components/ui/Button";
import { homeCarouselItems } from "../data/homeCarousel";
import styles from "../styles/HomePage.module.css";

const toolCards = [
  {
    title: "Import syllabus thông minh",
    description: "Đưa đề cương vào hệ thống, chuẩn hóa cấu trúc và mở đường cho toàn bộ pipeline course generation.",
    tone: "warm",
    size: "large"
  },
  {
    title: "AI video workflow",
    description: "Đi từ lesson script, slide outline, voiceover đến video-ready flow trong cùng một nhịp vận hành.",
    tone: "mint",
    size: "tall"
  },
  {
    title: "Theo dõi tiến trình rõ ràng",
    description: "Giúp admin và learner nhìn được lesson status, publish rhythm và tiến độ học tập.",
    tone: "lavender",
    size: "compact"
  },
  {
    title: "Vận hành khóa học tập trung",
    description: "Quản lý import, generate, publish và course delivery trong một bề mặt điều phối nhất quán.",
    tone: "neutral",
    size: "wide"
  }
];

const stats = [
  { value: "24+", label: "Course pipelines" },
  { value: "180+", label: "Lesson videos" },
  { value: "96%", label: "Workflow visibility" },
  { value: "12x", label: "Learning momentum" }
];
```

- [ ] **Step 2: Render the new hero first, then carousel second**

Implement the page JSX so the order is:

```jsx
<section className={styles.heroSection}>...</section>
<section className={styles.carouselBlock}>
  <CarouselSection items={homeCarouselItems} />
</section>
<section className={styles.toolsSection}>...</section>
<section className={styles.statsBand}>...</section>
<section className={styles.ctaSection}>...</section>
```

Hero content requirements:
- eyebrow badge
- large headline in Vietnamese aligned to VibeCourseAI domain
- short descriptive paragraph
- primary CTA to `/register`
- secondary CTA to `/courses`
- right-side media placeholder frame for future real image insertion

- [ ] **Step 3: Render the tools grid, stats band, and bottom CTA using semantic sections**

Use explicit markup instead of the old feature-section abstraction. The tools grid should map over `toolCards`; the stats band should map over `stats`; the final CTA should keep 2 strong action buttons.

- [ ] **Step 4: Run the homepage test to verify the new structure passes**

Run: `cd frontend && npx vitest run src/pages/HomePage.test.jsx`
Expected: PASS

## Task 3: Rebuild HomePage Styles To Match the Approved Layout Direction

**Files:**
- Modify: `frontend/src/styles/HomePage.module.css`

- [ ] **Step 1: Remove homepage styles that only support the old feature-stack sections**

Delete or stop referencing obsolete blocks such as:
- `.featureSection`
- `.featureContent`
- `.featureVisual`
- `.visualStack`
- `.visualProcess`
- `.visualLearner`
- old `.bottomCta`

Keep any carousel classes that are still used by `CarouselSection`.

- [ ] **Step 2: Add the new hero, tools grid, stats band, and CTA layout styles**

Add styles with responsibilities like:

```css
.homepage {
  display: grid;
  gap: clamp(72px, 8vw, 128px);
}

.heroSection {
  display: grid;
  gap: 32px;
  align-items: center;
}

.heroBody {
  display: grid;
  gap: 20px;
}

.heroMediaFrame {
  min-height: 440px;
  border: 1px solid var(--color-charcoal-border);
  border-radius: 28px;
  background: linear-gradient(145deg, #d9f7ec 0%, #b9efe0 42%, #eff8d8 100%);
  box-shadow: var(--shadow-subtle);
}

.toolsGrid {
  display: grid;
  gap: 16px;
}

.statsBand {
  display: grid;
  gap: 24px;
  padding: 28px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: #2d3224;
  color: var(--color-canvas-white);
}

.ctaPanel {
  display: grid;
  gap: 16px;
  padding: clamp(28px, 5vw, 56px);
  border: 1px solid var(--color-charcoal-border);
  border-radius: 28px;
  background: var(--color-accent-green);
  box-shadow: var(--shadow-subtle);
}
```

Also add:
- card tone modifiers
- asymmetrical desktop grid placement for tool cards
- mobile stack behavior
- spacing wrappers so the carousel visually sits under the hero cleanly

- [ ] **Step 3: Add responsive breakpoints for desktop/tablet/mobile behavior**

Desktop expectations:
- hero becomes 2 columns
- tool cards form an asymmetric grid
- stats tiles align horizontally
- CTA buttons can sit inline

Mobile expectations:
- hero stacks to 1 column
- media frame drops below text
- tools grid collapses cleanly
- stats wrap without overflow
- CTA buttons can stack vertically

- [ ] **Step 4: Run homepage tests again after the CSS refactor**

Run: `cd frontend && npx vitest run src/pages/HomePage.test.jsx`
Expected: PASS

## Task 4: Verify Carousel Integration and Full Frontend Build

**Files:**
- Verify: `frontend/src/components/sections/CarouselSection.jsx`
- Verify: `frontend/src/components/sections/CarouselSection.test.jsx`
- Verify: `frontend/src/pages/HomePage.test.jsx`

- [ ] **Step 1: Run the carousel component test to ensure the homepage style changes did not break slider behavior**

Run: `cd frontend && npx vitest run src/components/sections/CarouselSection.test.jsx`
Expected: PASS

- [ ] **Step 2: Run both focused homepage and carousel tests together**

Run: `cd frontend && npx vitest run src/pages/HomePage.test.jsx src/components/sections/CarouselSection.test.jsx`
Expected: PASS

- [ ] **Step 3: Run the frontend production build**

Run: `cd frontend && npm run build`
Expected: Vite build succeeds and emits the production bundle without homepage-specific errors.

- [ ] **Step 4: Rebuild the frontend container used by the local system**

Run: `docker compose up -d --build frontend`
Expected: frontend image rebuilds successfully and the running site serves the new homepage layout.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/HomePage.jsx frontend/src/styles/HomePage.module.css frontend/src/pages/HomePage.test.jsx docs/superpowers/specs/2026-05-22-homepage-layout-refresh-design.md docs/superpowers/plans/2026-05-22-homepage-layout-refresh-implementation.md
git commit -m "feat: refresh homepage layout"
```

## Self-Review

Spec coverage check:
- hero redesign: Task 2 + Task 3
- carousel directly below hero: Task 1 + Task 2
- content grid: Task 2 + Task 3
- stats band: Task 2 + Task 3
- CTA panel: Task 2 + Task 3
- keep header/footer unchanged: enforced by file scope, since no layout wrapper files are modified
- no SVG / use placeholders: Task 2 content structure + Task 3 media frame styling
- responsive behavior: Task 3
- tests/build verification: Task 4

Placeholder scan:
- all paths and commands are explicit
- no TODO/TBD markers remain
- test targets and build commands are concrete

Type and naming consistency:
- `toolCards`, `stats`, `heroSection`, `carouselBlock`, `toolsSection`, `statsBand`, and `ctaSection` are the names used consistently across the plan
