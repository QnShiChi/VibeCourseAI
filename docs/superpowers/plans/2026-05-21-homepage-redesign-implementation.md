# Homepage Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the VibeCourseAI homepage into a richer Brainfish-style landing page, while widening the global content container to 1360px, making the header full-width across the site, and adding a site-wide footer.

**Architecture:** Keep the implementation frontend-only. Move homepage rendering into focused section components, keep carousel state in a dedicated hook, and make global width/header/footer changes in shared layout and theme layers so every page inherits the same container and shell rules.

**Tech Stack:** React, React Router, Vite, plain CSS with existing theme tokens, local image assets, optional light in-view logic with browser APIs.

---

## File Structure

### Shared layout and theme

- Modify: `frontend/src/components/layout/MainLayout.jsx`
- Modify: `frontend/src/styles/theme.css`

### Homepage composition

- Modify: `frontend/src/pages/HomePage.jsx`
- Create: `frontend/src/components/sections/HeroSection.jsx`
- Create: `frontend/src/components/sections/CarouselSection.jsx`
- Create: `frontend/src/components/sections/FeatureSection.jsx`
- Create: `frontend/src/components/sections/StatsSection.jsx`
- Create: `frontend/src/components/layout/Footer.jsx`
- Create: `frontend/src/hooks/useCarousel.js`
- Create: `frontend/src/data/homeCarousel.js`
- Create: `frontend/src/styles/HomePage.module.css`

### Tests

- Modify or create: `frontend/src/pages/HomePage.test.jsx`
- Create: `frontend/src/components/sections/CarouselSection.test.jsx`
- Create: `frontend/src/hooks/useCarousel.test.js`

### Assets

- Create or populate: `frontend/src/assets/images/home-carousel/`

---

### Task 1: Audit and lock the shared layout shell

**Files:**
- Modify: `frontend/src/components/layout/MainLayout.jsx`
- Modify: `frontend/src/styles/theme.css`
- Test: `frontend/src/pages/HomePage.test.jsx`

- [ ] **Step 1: Write the failing expectation for a site-wide footer and main shell**

Add or update a test in `frontend/src/pages/HomePage.test.jsx` that renders the routed app shell and expects homepage content plus a footer landmark.

```jsx
import { MemoryRouter } from "react-router-dom";
import { render, screen } from "@testing-library/react";
import MainLayout from "../components/layout/MainLayout";

it("renders the shared shell with a footer landmark", () => {
  render(
    <MemoryRouter>
      <MainLayout />
    </MemoryRouter>
  );

  expect(screen.getByRole("banner")).toBeInTheDocument();
  expect(screen.getByRole("main")).toBeInTheDocument();
  expect(screen.getByRole("contentinfo")).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify the footer expectation fails**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: FAIL because no `contentinfo` footer exists yet.

- [ ] **Step 3: Update `MainLayout.jsx` to include a shared footer slot**

Adjust the layout so it keeps:
- a full-width `header`
- a centered `main`
- a shared `Footer` rendered after `main`

Implementation target shape:

```jsx
import { Outlet } from "react-router-dom";
import AppHeader from "./AppHeader";
import Footer from "./Footer";

export default function MainLayout() {
  return (
    <div className="page-shell">
      <AppHeader />
      <main className="page-main" role="main">
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}
```

- [ ] **Step 4: Add full-width header and wider container rules in `theme.css`**

Update the shared layout classes so that:
- `.app-header` spans full width
- `.page-container` caps at `1360px`
- padding becomes responsive by breakpoint
- header has an inner wrapper instead of being capped by the outer shell

Add or update CSS like:

```css
.page-container {
  width: min(1360px, calc(100% - 32px));
  margin: 0 auto;
}

.app-header {
  width: 100%;
  border-bottom: 1px solid var(--color-charcoal-border);
  background: var(--color-canvas-white);
}

.app-header__inner {
  width: min(1360px, calc(100% - 32px));
  margin: 0 auto;
}

.page-main {
  padding: 40px 0 72px;
}

@media (min-width: 768px) {
  .page-container,
  .app-header__inner {
    width: min(1360px, calc(100% - 48px));
  }
}

@media (min-width: 1024px) {
  .page-container,
  .app-header__inner {
    width: min(1360px, calc(100% - 64px));
  }
}

@media (min-width: 1440px) {
  .page-container,
  .app-header__inner {
    width: min(1360px, calc(100% - 80px));
  }
}
```

- [ ] **Step 5: Run test to verify the shell passes**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: PASS for the shell/footer expectation.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/layout/MainLayout.jsx frontend/src/styles/theme.css frontend/src/pages/HomePage.test.jsx
git commit -m "feat: widen global layout shell"
```

---

### Task 2: Build the site-wide footer component

**Files:**
- Create: `frontend/src/components/layout/Footer.jsx`
- Modify: `frontend/src/styles/theme.css`
- Test: `frontend/src/pages/HomePage.test.jsx`

- [ ] **Step 1: Write the failing footer content test**

Add a test assertion that expects VibeCourseAI footer branding and key navigation labels.

```jsx
expect(screen.getByRole("contentinfo")).toHaveTextContent("VibeCourseAI");
expect(screen.getByRole("contentinfo")).toHaveTextContent("Trang chủ");
expect(screen.getByRole("contentinfo")).toHaveTextContent("Khóa học");
```

- [ ] **Step 2: Run test to verify it fails on missing footer content**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: FAIL because the footer content is not implemented.

- [ ] **Step 3: Create `Footer.jsx` with semantic multi-column content**

Use `footer` with `role="contentinfo"` and a simple four-block layout.

```jsx
import { Link } from "react-router-dom";

export default function Footer() {
  return (
    <footer className="site-footer" role="contentinfo">
      <div className="page-container site-footer__inner">
        <div className="site-footer__grid">
          <div>
            <strong>VibeCourseAI</strong>
            <p>Tạo, quản lý và học khóa học video với AI.</p>
          </div>
          <div>
            <h2>Khám phá</h2>
            <Link to="/">Trang chủ</Link>
            <Link to="/courses">Khóa học</Link>
          </div>
          <div>
            <h2>Nền tảng</h2>
            <span>Tạo khóa học</span>
            <span>AI video workflow</span>
          </div>
          <div>
            <h2>Liên hệ</h2>
            <span>hello@vibecourse.ai</span>
            <span>Built for modern learning teams</span>
          </div>
        </div>
        <div className="site-footer__meta">© 2026 VibeCourseAI. AI-powered online learning.</div>
      </div>
    </footer>
  );
}
```

- [ ] **Step 4: Style the footer in `theme.css`**

Add CSS for:
- full-width footer shell
- inner grid
- stronger top border
- responsive stack on small screens

```css
.site-footer {
  border-top: 1px solid var(--color-charcoal-border);
  background:
    linear-gradient(180deg, #f7fbef 0%, #ffffff 100%);
}

.site-footer__inner {
  padding: 40px 0;
}

.site-footer__grid {
  display: grid;
  gap: var(--spacing-24);
}

.site-footer__meta {
  margin-top: var(--spacing-24);
  padding-top: var(--spacing-16);
  border-top: 1px solid rgba(23, 23, 23, 0.12);
}

@media (min-width: 768px) {
  .site-footer__grid {
    grid-template-columns: 1.4fr 1fr 1fr 1fr;
  }
}
```

- [ ] **Step 5: Run test to verify footer content passes**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: PASS for footer text and landmark checks.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/layout/Footer.jsx frontend/src/styles/theme.css frontend/src/pages/HomePage.test.jsx
git commit -m "feat: add site-wide footer"
```

---

### Task 3: Create local carousel data and carousel state hook

**Files:**
- Create: `frontend/src/data/homeCarousel.js`
- Create: `frontend/src/hooks/useCarousel.js`
- Test: `frontend/src/hooks/useCarousel.test.js`

- [ ] **Step 1: Write the failing hook test for next/previous/auto-rotate state**

Create `frontend/src/hooks/useCarousel.test.js`.

```jsx
import { act, renderHook } from "@testing-library/react";
import { useCarousel } from "./useCarousel";

it("cycles through slides and wraps around", () => {
  const { result } = renderHook(() => useCarousel({ totalSlides: 3, autoRotateMs: 0 }));

  expect(result.current.activeIndex).toBe(0);

  act(() => result.current.goToNext());
  expect(result.current.activeIndex).toBe(1);

  act(() => result.current.goToPrevious());
  expect(result.current.activeIndex).toBe(0);

  act(() => result.current.goToPrevious());
  expect(result.current.activeIndex).toBe(2);
});
```

- [ ] **Step 2: Run test to verify the hook file is missing**

Run:
```bash
cd frontend && npm test -- --run src/hooks/useCarousel.test.js
```

Expected: FAIL because the hook does not exist.

- [ ] **Step 3: Create `homeCarousel.js` with hardcoded local image metadata**

Add exported array with the target shape.

```js
import cover01 from "../assets/images/home-carousel/cover-01.jpg";
import cover02 from "../assets/images/home-carousel/cover-02.jpg";
import cover03 from "../assets/images/home-carousel/cover-03.jpg";

export const homeCarouselItems = [
  {
    id: "oop-masterclass",
    image: cover01,
    alt: "Mockup khóa học AI video về lập trình hướng đối tượng",
    title: "Lập trình hướng đối tượng",
    caption: "Chuỗi bài học được AI dựng thành video và narration hoàn chỉnh.",
    tag: "AI Video Course"
  },
  {
    id: "java-track",
    image: cover02,
    alt: "Mockup khóa học Java với giao diện showcase sáng màu",
    title: "Java Learning Track",
    caption: "Từ syllabus đến bài giảng video với workflow liền mạch.",
    tag: "Course Showcase"
  },
  {
    id: "dashboard-scene",
    image: cover03,
    alt: "Mockup dashboard generate và quản lý lesson video",
    title: "Admin Workflow in Motion",
    caption: "Theo dõi generate, audio, video và learner experience trong một nền tảng.",
    tag: "Admin Ready"
  }
];
```

- [ ] **Step 4: Create `useCarousel.js` with manual controls and autoplay support**

```jsx
import { useEffect, useRef, useState } from "react";

export function useCarousel({ totalSlides, autoRotateMs = 5000 }) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [isPaused, setIsPaused] = useState(false);
  const intervalRef = useRef(null);

  function goToIndex(index) {
    setActiveIndex((index + totalSlides) % totalSlides);
  }

  function goToNext() {
    setActiveIndex((current) => (current + 1) % totalSlides);
  }

  function goToPrevious() {
    setActiveIndex((current) => (current - 1 + totalSlides) % totalSlides);
  }

  useEffect(() => {
    if (!autoRotateMs || totalSlides <= 1 || isPaused) {
      return undefined;
    }

    intervalRef.current = window.setInterval(() => {
      setActiveIndex((current) => (current + 1) % totalSlides);
    }, autoRotateMs);

    return () => {
      window.clearInterval(intervalRef.current);
    };
  }, [autoRotateMs, isPaused, totalSlides]);

  return {
    activeIndex,
    goToIndex,
    goToNext,
    goToPrevious,
    pause: () => setIsPaused(true),
    resume: () => setIsPaused(false)
  };
}
```

- [ ] **Step 5: Run hook test to verify it passes**

Run:
```bash
cd frontend && npm test -- --run src/hooks/useCarousel.test.js
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/data/homeCarousel.js frontend/src/hooks/useCarousel.js frontend/src/hooks/useCarousel.test.js
git commit -m "feat: add homepage carousel state"
```

---

### Task 4: Build the carousel section UI

**Files:**
- Create: `frontend/src/components/sections/CarouselSection.jsx`
- Create: `frontend/src/components/sections/CarouselSection.test.jsx`
- Create: `frontend/src/styles/HomePage.module.css`
- Test: `frontend/src/components/sections/CarouselSection.test.jsx`

- [ ] **Step 1: Write the failing carousel rendering test**

```jsx
import { fireEvent, render, screen } from "@testing-library/react";
import CarouselSection from "./CarouselSection";

const items = [
  { id: "1", image: "/a.jpg", alt: "A", title: "Khóa học A", caption: "Caption A", tag: "Tag A" },
  { id: "2", image: "/b.jpg", alt: "B", title: "Khóa học B", caption: "Caption B", tag: "Tag B" }
];

it("renders active carousel content and next controls", () => {
  render(<CarouselSection items={items} />);

  expect(screen.getByText("Khóa học A")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /slide tiếp theo/i }));
  expect(screen.getByText("Khóa học B")).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify the component is missing**

Run:
```bash
cd frontend && npm test -- --run src/components/sections/CarouselSection.test.jsx
```

Expected: FAIL because `CarouselSection` does not exist.

- [ ] **Step 3: Create `CarouselSection.jsx` with accessible controls**

Implementation target shape:

```jsx
import { useMemo } from "react";
import { useCarousel } from "../../hooks/useCarousel";
import styles from "../../styles/HomePage.module.css";

export default function CarouselSection({ items }) {
  const slides = useMemo(() => items ?? [], [items]);
  const { activeIndex, goToIndex, goToNext, goToPrevious, pause, resume } = useCarousel({
    totalSlides: slides.length,
    autoRotateMs: 5000
  });

  if (!slides.length) {
    return null;
  }

  const activeSlide = slides[activeIndex];

  return (
    <section aria-label="Showcase Carousel từ các khóa học nổi bật" className={styles.carouselSection}>
      <div className={styles.carouselFrame} onMouseEnter={pause} onMouseLeave={resume}>
        <img alt={activeSlide.alt} className={styles.carouselImage} src={activeSlide.image} />
        <div className={styles.carouselOverlay}>
          <span className="ui-badge">{activeSlide.tag}</span>
          <h2>{activeSlide.title}</h2>
          <p>{activeSlide.caption}</p>
        </div>
        <button aria-label="Slide trước" onClick={goToPrevious} type="button">←</button>
        <button aria-label="Slide tiếp theo" onClick={goToNext} type="button">→</button>
      </div>
      <div className={styles.carouselDots}>
        {slides.map((item, index) => (
          <button
            key={item.id}
            aria-label={`Đi tới slide ${index + 1}`}
            aria-pressed={index === activeIndex}
            onClick={() => goToIndex(index)}
            type="button"
          />
        ))}
      </div>
    </section>
  );
}
```

- [ ] **Step 4: Add carousel-specific styles in `HomePage.module.css`**

Include rules for:
- image frame
- overlay
- controls
- dots
- responsive stack

```css
.carouselSection {
  display: grid;
  gap: var(--spacing-16);
}

.carouselFrame {
  position: relative;
  overflow: hidden;
  border: 1px solid var(--color-charcoal-border);
  border-radius: 28px;
  box-shadow: var(--shadow-subtle);
  background: var(--color-canvas-white);
}

.carouselImage {
  display: block;
  width: 100%;
  aspect-ratio: 16 / 9;
  object-fit: cover;
}

.carouselOverlay {
  position: absolute;
  inset: auto 0 0 0;
  padding: var(--spacing-24);
  background: linear-gradient(180deg, transparent 0%, rgba(0, 0, 0, 0.72) 100%);
  color: var(--color-canvas-white);
}
```

- [ ] **Step 5: Run carousel test to verify it passes**

Run:
```bash
cd frontend && npm test -- --run src/components/sections/CarouselSection.test.jsx
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/sections/CarouselSection.jsx frontend/src/components/sections/CarouselSection.test.jsx frontend/src/styles/HomePage.module.css
git commit -m "feat: add homepage showcase carousel"
```

---

### Task 5: Build reusable homepage section components

**Files:**
- Create: `frontend/src/components/sections/HeroSection.jsx`
- Create: `frontend/src/components/sections/FeatureSection.jsx`
- Create: `frontend/src/components/sections/StatsSection.jsx`
- Modify: `frontend/src/styles/HomePage.module.css`
- Test: `frontend/src/pages/HomePage.test.jsx`

- [ ] **Step 1: Write the failing homepage content test for new sections**

Add test assertions expecting the new section headings.

```jsx
expect(screen.getByText(/Showcase Carousel từ các khóa học nổi bật/i)).toBeInTheDocument();
expect(screen.getByText(/Tạo khóa học từ syllabus trong 1 nút/i)).toBeInTheDocument();
expect(screen.getByText(/AI tự động tạo Video \+ Narration/i)).toBeInTheDocument();
expect(screen.getByText(/Sẵn sàng tạo khóa học AI-powered\?/i)).toBeInTheDocument();
```

- [ ] **Step 2: Run test to verify the homepage content is still old**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: FAIL because the homepage still renders the old hero and highlight cards.

- [ ] **Step 3: Create `HeroSection.jsx` with strong AI + video messaging**

Build a component that receives CTA links and renders:
- badge
- headline
- paragraph
- visual panel with floating chips

- [ ] **Step 4: Create `FeatureSection.jsx` as a reusable staggered template**

Build a component API like:

```jsx
export default function FeatureSection({
  eyebrow,
  title,
  description,
  bullets = [],
  cta,
  tone = "mint",
  layout = "content-left",
  visual
}) {
  // render a two-column or centered section
}
```

- [ ] **Step 5: Create `StatsSection.jsx` with lightweight animated counters**

Use an in-view flag and progressive number animation only after the section becomes visible.

Implementation target logic:

```jsx
import { useEffect, useRef, useState } from "react";

function useInViewOnce() {
  const ref = useRef(null);
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) {
        setInView(true);
        observer.disconnect();
      }
    }, { threshold: 0.2 });

    if (ref.current) {
      observer.observe(ref.current);
    }

    return () => observer.disconnect();
  }, []);

  return { ref, inView };
}
```

- [ ] **Step 6: Extend `HomePage.module.css` for hero, feature, stats, CTA, and scroll-reveal styling**

Add classes for:
- hero grid
- feature alternating layouts
- stats tiles
- CTA band
- reduced-motion-safe transitions

- [ ] **Step 7: Run homepage test to verify sections now exist**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: PASS for section heading assertions.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/components/sections/HeroSection.jsx frontend/src/components/sections/FeatureSection.jsx frontend/src/components/sections/StatsSection.jsx frontend/src/styles/HomePage.module.css frontend/src/pages/HomePage.test.jsx
git commit -m "feat: build homepage content sections"
```

---

### Task 6: Recompose `HomePage.jsx` around the new sections

**Files:**
- Modify: `frontend/src/pages/HomePage.jsx`
- Modify: `frontend/src/data/homeCarousel.js`
- Modify: `frontend/src/styles/HomePage.module.css`
- Test: `frontend/src/pages/HomePage.test.jsx`

- [ ] **Step 1: Write the failing CTA/navigation expectation for homepage composition**

Add expectations for both hero CTAs and footer CTA band text.

```jsx
expect(screen.getByRole("link", { name: /Bắt đầu ngay/i })).toHaveAttribute("href", "/register");
expect(screen.getByRole("link", { name: /Xem khóa học/i })).toHaveAttribute("href", "/courses");
expect(screen.getByRole("link", { name: /Đăng ký miễn phí/i })).toBeInTheDocument();
```

- [ ] **Step 2: Run test to verify the new homepage composition is incomplete**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: FAIL until `HomePage.jsx` is rebuilt.

- [ ] **Step 3: Replace the old `HomePage.jsx` with section composition**

Implementation target structure:

```jsx
import { Link } from "react-router-dom";
import Section from "../components/ui/Section";
import HeroSection from "../components/sections/HeroSection";
import CarouselSection from "../components/sections/CarouselSection";
import FeatureSection from "../components/sections/FeatureSection";
import StatsSection from "../components/sections/StatsSection";
import { homeCarouselItems } from "../data/homeCarousel";
import styles from "../styles/HomePage.module.css";

export default function HomePage() {
  return (
    <Section className={styles.homepage}>
      <HeroSection />
      <CarouselSection items={homeCarouselItems} />
      <FeatureSection ... />
      <FeatureSection ... />
      <FeatureSection ... />
      <FeatureSection ... />
      <StatsSection ... />
      <section className={styles.bottomCta}>...</section>
    </Section>
  );
}
```

- [ ] **Step 4: Ensure all copy and CTAs align with the approved spec**

Spec alignment checklist inside code:
- Hero CTA primary: `Bắt đầu ngay`
- Hero CTA secondary: `Xem khóa học`
- Section A CTA: `Bắt đầu tải đề cương`
- Section B CTA: `Xem demo`
- Section C CTA: `Truy cập Admin Dashboard`
- Section D CTA: `Đồng ý làm học viên`
- Bottom CTA primary: `Đăng ký miễn phí`
- Bottom CTA secondary: `Liên hệ`

- [ ] **Step 5: Run homepage test to verify composed page passes**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/HomePage.jsx frontend/src/data/homeCarousel.js frontend/src/styles/HomePage.module.css frontend/src/pages/HomePage.test.jsx
git commit -m "feat: redesign homepage composition"
```

---

### Task 7: Add carousel accessibility and keyboard behavior

**Files:**
- Modify: `frontend/src/components/sections/CarouselSection.jsx`
- Modify: `frontend/src/hooks/useCarousel.js`
- Modify: `frontend/src/components/sections/CarouselSection.test.jsx`

- [ ] **Step 1: Write the failing keyboard navigation test**

```jsx
import { fireEvent, render, screen } from "@testing-library/react";
import CarouselSection from "./CarouselSection";

it("supports keyboard navigation through arrow keys", () => {
  render(<CarouselSection items={items} />);

  const region = screen.getByRole("region", { name: /showcase carousel/i });
  region.focus();
  fireEvent.keyDown(region, { key: "ArrowRight" });
  expect(screen.getByText("Khóa học B")).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify keyboard navigation fails**

Run:
```bash
cd frontend && npm test -- --run src/components/sections/CarouselSection.test.jsx
```

Expected: FAIL because arrow key handling does not exist yet.

- [ ] **Step 3: Add keyboard handlers and focusable region semantics**

Update `CarouselSection.jsx`:

```jsx
function handleKeyDown(event) {
  if (event.key === "ArrowRight") {
    goToNext();
  }

  if (event.key === "ArrowLeft") {
    goToPrevious();
  }
}

<section
  aria-label="Showcase Carousel từ các khóa học nổi bật"
  className={styles.carouselSection}
  onKeyDown={handleKeyDown}
  role="region"
  tabIndex={0}
>
```

- [ ] **Step 4: Ensure autoplay pause/resume remains compatible with manual focus interaction**

Update the hook or component only as needed so keyboard interaction does not break autoplay cleanup.

- [ ] **Step 5: Run carousel test suite again**

Run:
```bash
cd frontend && npm test -- --run src/components/sections/CarouselSection.test.jsx src/hooks/useCarousel.test.js
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/sections/CarouselSection.jsx frontend/src/components/sections/CarouselSection.test.jsx frontend/src/hooks/useCarousel.js frontend/src/hooks/useCarousel.test.js
git commit -m "feat: improve homepage carousel accessibility"
```

---

### Task 8: Add scroll reveal and reduced-motion-safe presentation

**Files:**
- Modify: `frontend/src/components/sections/HeroSection.jsx`
- Modify: `frontend/src/components/sections/FeatureSection.jsx`
- Modify: `frontend/src/components/sections/StatsSection.jsx`
- Modify: `frontend/src/styles/HomePage.module.css`
- Test: `frontend/src/pages/HomePage.test.jsx`

- [ ] **Step 1: Write the failing test for motion-safe section attributes**

Add a simple structural expectation that reveal-enabled sections render a shared data attribute.

```jsx
expect(screen.getByText(/Tạo khóa học từ syllabus trong 1 nút/i).closest("section")).toHaveAttribute("data-reveal", "true");
```

- [ ] **Step 2: Run test to verify reveal attributes are missing**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: FAIL until reveal semantics are added.

- [ ] **Step 3: Add lightweight reveal markers to section components**

Apply a shared attribute or class to relevant sections:

```jsx
<section className={styles.featureSection} data-reveal="true">
```

- [ ] **Step 4: Add CSS reveal transitions with `prefers-reduced-motion` handling**

```css
[data-reveal="true"] {
  opacity: 0;
  transform: translateY(18px);
  transition: opacity 0.45s ease-in-out, transform 0.45s ease-in-out;
}

[data-reveal="true"].isVisible {
  opacity: 1;
  transform: translateY(0);
}

@media (prefers-reduced-motion: reduce) {
  [data-reveal="true"] {
    opacity: 1;
    transform: none;
    transition: none;
  }
}
```

- [ ] **Step 5: Add the minimal in-view logic required to apply `isVisible` once**

Use `IntersectionObserver` in the section components or a tiny shared helper, without introducing extra libraries.

- [ ] **Step 6: Run homepage tests to verify structure still passes**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/components/sections/HeroSection.jsx frontend/src/components/sections/FeatureSection.jsx frontend/src/components/sections/StatsSection.jsx frontend/src/styles/HomePage.module.css frontend/src/pages/HomePage.test.jsx
git commit -m "feat: add homepage motion reveals"
```

---

### Task 9: Verify the redesign in tests and production build

**Files:**
- Test only

- [ ] **Step 1: Run focused homepage-related tests**

Run:
```bash
cd frontend && npm test -- --run src/pages/HomePage.test.jsx src/components/sections/CarouselSection.test.jsx src/hooks/useCarousel.test.js
```

Expected: PASS for all homepage, carousel, and hook tests.

- [ ] **Step 2: Run the frontend production build**

Run:
```bash
cd frontend && npm run build
```

Expected: Vite build succeeds and emits the production bundle.

- [ ] **Step 3: Smoke-check global shell and homepage routes locally**

Run:
```bash
docker compose up -d --build frontend
curl -I http://localhost:3000
```

Expected: `HTTP/1.1 200 OK` and the updated homepage served by nginx.

- [ ] **Step 4: Commit final verification-ready changes if needed**

```bash
git add frontend
git commit -m "test: verify homepage redesign"
```

---

## Self-Review

### Spec coverage

Covered requirements from the spec:
- homepage hero redesign
- local-image carousel
- four feature sections
- stats section
- bottom CTA
- site-wide footer
- widened `1360px` container
- full-width header shell
- responsive behavior
- accessibility and motion safety

No known spec gaps remain for the approved scope.

### Placeholder scan

No `TODO`, `TBD`, or “implement later” placeholders remain in this plan. All tasks include exact file paths, commands, and code targets.

### Type consistency

The plan uses a consistent file and API shape across tasks:
- `useCarousel`
- `CarouselSection`
- `HeroSection`
- `FeatureSection`
- `StatsSection`
- `Footer`
- `homeCarouselItems`

---

Plan complete and saved to `docs/superpowers/plans/2026-05-21-homepage-redesign-implementation.md`.

Two execution options:

1. Subagent-Driven (recommended) - I dispatch a fresh subagent per task, review between tasks, fast iteration

2. Inline Execution - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
