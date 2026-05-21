# Frontend Design Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor all existing frontend screens so the application shell, typography, colors, spacing, cards, buttons, and forms are consistent with `DESIGN.md` while keeping current auth behavior intact.

**Architecture:** Add a small shared design foundation inside the existing React app: global CSS custom properties for `DESIGN.md` tokens, a few reusable UI primitives, and a refactored `MainLayout` that all current pages inherit. Then migrate auth and content pages off inline styles onto the shared primitives, preserving existing routes and auth flow.

**Tech Stack:** React 18, React Router 6, Vite, plain CSS with CSS custom properties, Vitest, Testing Library

---

## File Structure

### Files to create
- `frontend/src/styles/theme.css` — global design tokens, reset, typography, layout, card, button, input, badge, alert, and grid classes based on `DESIGN.md`
- `frontend/src/components/ui/Button.jsx` — shared button primitive with `primary`, `ghost`, and `link` variants
- `frontend/src/components/ui/Card.jsx` — shared card primitive with `default`, `shadowed`, and `highlight` variants
- `frontend/src/components/ui/PageHeader.jsx` — common page header block for title and supporting text
- `frontend/src/components/ui/Section.jsx` — section wrapper with consistent spacing and container behavior
- `frontend/src/components/ui/FormField.jsx` — shared labeled field wrapper for input layout consistency
- `frontend/src/pages/LoginPage.test.jsx` — focused UI regression test for login card layout and button text

### Files to modify
- `frontend/src/main.jsx` — import global theme stylesheet
- `frontend/src/components/layout/MainLayout.jsx` — replace dark inline shell with light sticky navigation and shared action styles
- `frontend/src/components/layout/MainLayout.test.jsx` — assert updated shell labels remain visible for authenticated admin
- `frontend/src/pages/HomePage.jsx` — convert plain text block into hero + feature card composition
- `frontend/src/pages/LoginPage.jsx` — move to centered auth card using shared components/classes
- `frontend/src/pages/RegisterPage.jsx` — move to centered auth card using shared components/classes
- `frontend/src/pages/DashboardPage.jsx` — replace skeleton paragraph with structured dashboard placeholder cards
- `frontend/src/pages/CoursesPage.jsx` — replace text-only page with course list cards/empty state UI
- `frontend/src/pages/ProfilePage.jsx` — replace plain text rows with structured profile info card
- `frontend/src/pages/ChangePasswordPage.jsx` — move to shared form card and alert blocks

### Files to verify but not change unless required
- `frontend/src/routes/AppRoutes.jsx`
- `frontend/src/auth/RequireAuth.jsx`
- `frontend/src/auth/AuthContext.jsx`
- `frontend/package.json`

## Task 1: Add the global design foundation

**Files:**
- Create: `frontend/src/styles/theme.css`
- Modify: `frontend/src/main.jsx`
- Test: `frontend/src/components/layout/MainLayout.test.jsx`

- [ ] **Step 1: Write the failing test for the shell to assert the new brand label and nav still render**

```jsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import MainLayout from "./MainLayout";

vi.mock("../../auth/useAuth", () => ({
  useAuth: () => ({
    isAuthenticated: true,
    user: { fullName: "Quản trị viên hệ thống", role: "Admin" },
    logout: vi.fn()
  })
}));

describe("MainLayout", () => {
  it("renders the shared navigation shell for an authenticated admin", () => {
    render(
      <MemoryRouter>
        <MainLayout />
      </MemoryRouter>
    );

    expect(screen.getByText("VibeCourseAI")).toBeInTheDocument();
    expect(screen.getByText("Dashboard")).toBeInTheDocument();
    expect(screen.getByText("Đăng xuất")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the focused test to confirm the current shell is the starting point**

Run: `npm run test -- --run frontend/src/components/layout/MainLayout.test.jsx`
Expected: PASS or FAIL; record the current baseline before theme changes.

- [ ] **Step 3: Add the global `DESIGN.md` token layer**

```css
/* frontend/src/styles/theme.css */
:root {
  --color-midnight-ink: #000000;
  --color-canvas-white: #ffffff;
  --color-charcoal-border: #171717;
  --color-shadow-base: #0a0a0d;
  --color-pale-ash: #f5f5f5;
  --color-accent-green: #a3e635;
  --color-card-saffron: #fef3c8;
  --color-card-lavender: #fae9ff;
  --color-card-mint: #d2fae5;
  --color-card-pink: #f5d1fe;
  --color-highlight-yellow: #fbbf25;
  --gradient-sky-breeze: linear-gradient(rgb(137, 229, 240), rgb(182, 239, 246) 27%, rgb(204, 243, 250) 35%, rgb(197, 243, 248) 55%);
  --font-satoshi: "Satoshi", ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  --text-body: 16px;
  --text-heading: 24px;
  --text-display: 48px;
  --spacing-8: 8px;
  --spacing-16: 16px;
  --spacing-24: 24px;
  --spacing-32: 32px;
  --spacing-40: 40px;
  --radius-md: 4px;
  --radius-lg: 8px;
  --radius-full: 100px;
  --shadow-subtle: rgb(10, 10, 13) 2px 2px 0px 0px;
  --shadow-subtle-3: rgb(10, 10, 13) 1px 1px 0px 0px;
}

* { box-sizing: border-box; }
body {
  margin: 0;
  font-family: var(--font-satoshi);
  background: var(--color-canvas-white);
  color: var(--color-midnight-ink);
}

a { color: inherit; }
button, input { font: inherit; }

.page-shell { min-height: 100vh; background: var(--color-canvas-white); }
.page-container { width: min(1120px, calc(100% - 32px)); margin: 0 auto; }
.section-stack { display: grid; gap: var(--spacing-24); }
.surface-card {
  background: var(--color-canvas-white);
  border: 1px solid var(--color-charcoal-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-24);
}
.surface-card--shadowed { box-shadow: var(--shadow-subtle-3); }
.ui-button {
  border: 1px solid var(--color-charcoal-border);
  border-radius: var(--radius-md);
  padding: 12px 24px;
  font-weight: 700;
  cursor: pointer;
}
.ui-button--primary {
  background: var(--color-accent-green);
  color: var(--color-midnight-ink);
  box-shadow: var(--shadow-subtle-3);
}
.ui-button--ghost {
  background: var(--color-canvas-white);
  color: var(--color-midnight-ink);
  box-shadow: var(--shadow-subtle-3);
}
.ui-input {
  width: 100%;
  border: 1px solid #737373;
  border-radius: var(--radius-md);
  padding: 12px;
  background: var(--color-canvas-white);
  color: var(--color-midnight-ink);
}
```

- [ ] **Step 4: Import the theme stylesheet at the application entry point**

```jsx
// frontend/src/main.jsx
import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import "./styles/theme.css";
```

- [ ] **Step 5: Run the layout test again to confirm the theme import did not break rendering**

Run: `npm run test -- --run frontend/src/components/layout/MainLayout.test.jsx`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add frontend/src/main.jsx frontend/src/styles/theme.css frontend/src/components/layout/MainLayout.test.jsx
git commit -m "feat: add frontend design theme foundation"
```

## Task 2: Build shared UI primitives and refactor the application shell

**Files:**
- Create: `frontend/src/components/ui/Button.jsx`
- Create: `frontend/src/components/ui/Card.jsx`
- Create: `frontend/src/components/ui/PageHeader.jsx`
- Create: `frontend/src/components/ui/Section.jsx`
- Create: `frontend/src/components/ui/FormField.jsx`
- Modify: `frontend/src/components/layout/MainLayout.jsx`
- Modify: `frontend/src/components/layout/MainLayout.test.jsx`
- Test: `frontend/src/components/layout/MainLayout.test.jsx`

- [ ] **Step 1: Extend the layout test to assert the new navigation copy still works after shell refactor**

```jsx
expect(screen.getByRole("link", { name: "Trang chủ" })).toBeInTheDocument();
expect(screen.getByRole("link", { name: "Khóa học" })).toBeInTheDocument();
expect(screen.getByText("Quản trị viên hệ thống")).toBeInTheDocument();
```

- [ ] **Step 2: Run the layout test before changing the shell**

Run: `npm run test -- --run frontend/src/components/layout/MainLayout.test.jsx`
Expected: PASS or current baseline PASS

- [ ] **Step 3: Add the reusable UI primitives**

```jsx
// frontend/src/components/ui/Button.jsx
export default function Button({ as: Component = "button", variant = "primary", className = "", ...props }) {
  return <Component className={`ui-button ui-button--${variant} ${className}`.trim()} {...props} />;
}

// frontend/src/components/ui/Card.jsx
export default function Card({ variant = "default", className = "", children }) {
  const variantClass = variant === "shadowed" ? "surface-card surface-card--shadowed" : "surface-card";
  return <div className={`${variantClass} ${className}`.trim()}>{children}</div>;
}

// frontend/src/components/ui/PageHeader.jsx
export default function PageHeader({ eyebrow, title, description, actions }) {
  return (
    <div className="page-header">
      {eyebrow ? <p className="page-eyebrow">{eyebrow}</p> : null}
      <div className="page-header__row">
        <div>
          <h1>{title}</h1>
          {description ? <p>{description}</p> : null}
        </div>
        {actions ? <div>{actions}</div> : null}
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Refactor `MainLayout.jsx` to use the light sticky shell and shared actions**

```jsx
import { Link, Outlet } from "react-router-dom";
import { useAuth } from "../../auth/useAuth";
import Button from "../ui/Button";

export default function MainLayout() {
  const { isAuthenticated, user, logout } = useAuth();
  const isAdmin = user?.role === "Admin";

  return (
    <div className="page-shell">
      <header className="app-header">
        <div className="page-container app-header__inner">
          <Link to="/" className="app-brand">VibeCourseAI</Link>
          <nav className="app-nav">
            <Link to="/">Trang chủ</Link>
            <Link to="/courses">Khóa học</Link>
            {isAuthenticated ? (
              <>
                {isAdmin ? <Link to="/dashboard">Dashboard</Link> : null}
                <span className="app-badge">{user?.fullName}</span>
                <Link to="/profile">Hồ sơ</Link>
                <Link to="/change-password">Đổi mật khẩu</Link>
                <Button onClick={logout} variant="ghost">Đăng xuất</Button>
              </>
            ) : (
              <>
                <Link to="/login">Đăng nhập</Link>
                <Button as={Link} to="/register">Đăng ký</Button>
              </>
            )}
          </nav>
        </div>
      </header>
      <main className="page-container app-main">
        <Outlet />
      </main>
    </div>
  );
}
```

- [ ] **Step 5: Run the layout test to verify the refactored shell still renders correctly**

Run: `npm run test -- --run frontend/src/components/layout/MainLayout.test.jsx`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/ui/Button.jsx frontend/src/components/ui/Card.jsx frontend/src/components/ui/PageHeader.jsx frontend/src/components/ui/Section.jsx frontend/src/components/ui/FormField.jsx frontend/src/components/layout/MainLayout.jsx frontend/src/components/layout/MainLayout.test.jsx frontend/src/styles/theme.css
git commit -m "feat: refactor frontend app shell and shared ui primitives"
```

## Task 3: Refactor the auth-facing pages to the shared system

**Files:**
- Create: `frontend/src/pages/LoginPage.test.jsx`
- Modify: `frontend/src/pages/LoginPage.jsx`
- Modify: `frontend/src/pages/RegisterPage.jsx`
- Modify: `frontend/src/pages/ChangePasswordPage.jsx`
- Test: `frontend/src/pages/LoginPage.test.jsx`

- [ ] **Step 1: Write a failing login page test for the new card-based auth layout**

```jsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import LoginPage from "./LoginPage";

vi.mock("../auth/useAuth", () => ({
  useAuth: () => ({ login: vi.fn() })
}));

describe("LoginPage", () => {
  it("renders the shared auth card with a primary submit action", () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: "Đăng nhập" })).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Nhập email của bạn")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Đăng nhập" })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the focused auth page test to confirm current behavior**

Run: `npm run test -- --run frontend/src/pages/LoginPage.test.jsx`
Expected: PASS or FAIL; use it as the refactor safety net.

- [ ] **Step 3: Refactor `LoginPage.jsx`, `RegisterPage.jsx`, and `ChangePasswordPage.jsx` to use the shared classes and primitives**

```jsx
// frontend/src/pages/LoginPage.jsx
import { Link, useNavigate } from "react-router-dom";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import PageHeader from "../components/ui/PageHeader";

return (
  <section className="auth-page">
    <Card variant="shadowed" className="auth-card">
      <PageHeader
        eyebrow="Tai khoan"
        title="Đăng nhập"
        description="Vui lòng nhập thông tin tài khoản để truy cập hệ thống học tập."
      />
      <form className="auth-form" onSubmit={handleSubmit}>
        <label className="form-field">
          <span>Email</span>
          <input className="ui-input" ... />
        </label>
        <label className="form-field">
          <span>Mật khẩu</span>
          <input className="ui-input" ... />
        </label>
        {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}
        <Button type="submit">{isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}</Button>
      </form>
      <p className="auth-footer">Bạn chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link></p>
    </Card>
  </section>
);
```

- [ ] **Step 4: Run the auth page test after the refactor**

Run: `npm run test -- --run frontend/src/pages/LoginPage.test.jsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/LoginPage.jsx frontend/src/pages/RegisterPage.jsx frontend/src/pages/ChangePasswordPage.jsx frontend/src/pages/LoginPage.test.jsx frontend/src/styles/theme.css
git commit -m "feat: restyle auth pages to match design system"
```

## Task 4: Refactor home, dashboard, courses, and profile pages

**Files:**
- Modify: `frontend/src/pages/HomePage.jsx`
- Modify: `frontend/src/pages/DashboardPage.jsx`
- Modify: `frontend/src/pages/CoursesPage.jsx`
- Modify: `frontend/src/pages/ProfilePage.jsx`
- Test: `frontend/src/components/layout/MainLayout.test.jsx`

- [ ] **Step 1: Add or reuse assertions that keep the content routes rendering inside the shared shell**

```jsx
expect(screen.getByText("Dashboard")).toBeInTheDocument();
expect(screen.getByText("Khóa học")).toBeInTheDocument();
```

- [ ] **Step 2: Run the relevant frontend tests before page refactors**

Run: `npm run test -- --run frontend/src/components/layout/MainLayout.test.jsx frontend/src/pages/LoginPage.test.jsx`
Expected: PASS

- [ ] **Step 3: Replace text-only content pages with structured sections and cards**

```jsx
// frontend/src/pages/CoursesPage.jsx
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";

const demoCourses = [
  { title: "Sample Course", status: "Draft", summary: "Khóa học mẫu để kiểm tra luồng hiển thị course từ API." },
  { title: "Frontend Design Foundations", status: "Coming soon", summary: "Placeholder UI cho danh sách khóa học trước khi nối dữ liệu thật." }
];

export default function CoursesPage() {
  return (
    <section className="section-stack">
      <PageHeader
        eyebrow="Khoa hoc"
        title="Khóa học"
        description="Khám phá danh sách khóa học hiện có trên hệ thống."
      />
      <div className="card-grid">
        {demoCourses.map((course) => (
          <Card key={course.title} variant="shadowed">
            <p className="app-badge">{course.status}</p>
            <h2>{course.title}</h2>
            <p>{course.summary}</p>
          </Card>
        ))}
      </div>
    </section>
  );
}
```

- [ ] **Step 4: Run the frontend tests after the content page refactor**

Run: `npm run test -- --run frontend/src/components/layout/MainLayout.test.jsx frontend/src/pages/LoginPage.test.jsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/HomePage.jsx frontend/src/pages/DashboardPage.jsx frontend/src/pages/CoursesPage.jsx frontend/src/pages/ProfilePage.jsx frontend/src/styles/theme.css
git commit -m "feat: refactor content pages to shared design language"
```

## Task 5: Final verification and cleanup

**Files:**
- Verify: `frontend/src/**/*`
- Test: `frontend/src/components/layout/MainLayout.test.jsx`
- Test: `frontend/src/pages/LoginPage.test.jsx`

- [ ] **Step 1: Run the complete frontend test suite**

Run: `npm run test -- --run`
Expected: PASS with all frontend tests green

- [ ] **Step 2: Run the production build**

Run: `npm run build`
Expected: Vite build completes successfully and emits `dist/` assets

- [ ] **Step 3: Manually review spec coverage against the updated UI files**

Checklist:
```text
- MainLayout uses light sticky shell
- Shared tokens exist in theme.css
- Buttons, cards, inputs, badges use DESIGN.md rules
- Login/Register/ChangePassword share the same auth card system
- Home/Dashboard/Courses/Profile are no longer text-only placeholders
- No page still uses Georgia or the old dark app shell
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src frontend/package.json
git commit -m "test: verify frontend design refactor"
```
