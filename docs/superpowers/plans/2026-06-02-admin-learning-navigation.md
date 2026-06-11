# Admin Learning Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let admins move from the admin dashboard to the learning area and back again through existing navigation patterns.

**Architecture:** Keep route protection unchanged and implement the feature entirely in frontend layout/navigation components. Add one admin-only sidebar link in the admin shell and verify the public shell still exposes the existing admin dashboard menu for admin users.

**Tech Stack:** React, React Router, Vitest, Testing Library

---

### Task 1: Add Regression Tests For Admin Navigation

**Files:**
- Modify: `frontend/src/components/layout/AdminLayout.test.jsx`
- Modify: `frontend/src/components/layout/MainLayout.test.jsx`

- [ ] **Step 1: Write the failing admin sidebar test**

Add an assertion to `frontend/src/components/layout/AdminLayout.test.jsx` that expects a visible `Khu học tập` link with `href="/"`.

- [ ] **Step 2: Run the targeted admin layout test to verify it fails**

Run: `npm test -- --run frontend/src/components/layout/AdminLayout.test.jsx`
Expected: FAIL because the `Khu học tập` link does not exist yet.

- [ ] **Step 3: Write the failing main layout admin-menu test if coverage is missing**

Add a test in `frontend/src/components/layout/MainLayout.test.jsx` that renders the main layout with an admin user and expects the `Dashboard` navigation group to be visible.

- [ ] **Step 4: Run the targeted main layout test to verify it fails or proves current coverage**

Run: `npm test -- --run frontend/src/components/layout/MainLayout.test.jsx`
Expected: Either FAIL because the admin dashboard menu is not asserted yet, or PASS and confirm current coverage is already sufficient.

- [ ] **Step 5: Commit the red test state only if the repo workflow allows it**

Skip commit if working in a single continuous task without intermediate commits.

### Task 2: Implement Admin Two-Way Navigation

**Files:**
- Modify: `frontend/src/components/layout/AdminLayout.jsx`
- Modify: `frontend/src/components/layout/MainLayout.jsx`

- [ ] **Step 1: Add the admin-to-learning navigation item**

In `frontend/src/components/layout/AdminLayout.jsx`, add a new `AdminNavItem` labeled `Khu học tập` pointing to `/` inside the main sidebar navigation list.

- [ ] **Step 2: Preserve the learning-to-admin dashboard path**

In `frontend/src/components/layout/MainLayout.jsx`, keep the admin-only `Dashboard` menu visible for admins and ensure it still includes an entry to `/dashboard`. Only adjust wording/order if needed for clarity; do not add a standalone button.

- [ ] **Step 3: Run the targeted tests to verify the implementation passes**

Run: `npm test -- --run frontend/src/components/layout/AdminLayout.test.jsx frontend/src/components/layout/MainLayout.test.jsx`
Expected: PASS

- [ ] **Step 4: Do a quick route smoke-check in the browser build**

Run: `npm test -- --run frontend/src/auth/RequireAuth.test.jsx`
Expected: PASS, confirming route protection behavior was not regressed by navigation-only changes.

- [ ] **Step 5: Commit the implementation**

```bash
git add frontend/src/components/layout/AdminLayout.jsx frontend/src/components/layout/MainLayout.jsx frontend/src/components/layout/AdminLayout.test.jsx frontend/src/components/layout/MainLayout.test.jsx
git commit -m "feat: add admin learning navigation"
```

### Task 3: Verify End-To-End Behavior

**Files:**
- No code changes required unless verification exposes a bug

- [ ] **Step 1: Run the full targeted frontend verification**

Run: `npm test -- --run frontend/src/components/layout/AdminLayout.test.jsx frontend/src/components/layout/MainLayout.test.jsx frontend/src/auth/RequireAuth.test.jsx`
Expected: PASS

- [ ] **Step 2: Rebuild or refresh the frontend if needed for manual validation**

Run: `docker compose up -d frontend`
Expected: Frontend container remains up.

- [ ] **Step 3: Manually validate the two navigation paths**

Check:
- admin dashboard sidebar shows `Khu học tập`
- clicking it reaches `/`
- admin user still sees the `Dashboard` menu in the public layout
- the dashboard menu still contains a path back to `/dashboard`

- [ ] **Step 4: Report any remaining gaps**

If manual validation reveals a styling or active-state issue, fix it and rerun the tests above before completion.
