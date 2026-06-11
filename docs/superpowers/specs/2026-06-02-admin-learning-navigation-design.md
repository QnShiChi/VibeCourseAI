# Admin Learning Navigation Design

## Goal

Allow administrators to move between the admin dashboard and the public learning experience without changing roles or losing access. Admins should be able to enter the learning area from the admin sidebar, and when they are in the learning area they should still have a clear menu-based path back to the dashboard.

## Current State

- Admin-only pages already sit behind `RequireAuth requiredRole="Admin"`.
- The public learning area already allows authenticated admins to access routes like `/`, `/courses`, and `/courses/:courseId/learn`.
- The missing piece is navigation clarity:
  - the admin sidebar has no link that sends admins back to the learning area,
  - the public layout only exposes dashboard access through the existing admin-only navigation group.

## Decision

Implement a minimal two-way navigation change without altering route guards:

1. Add a new sidebar item in `AdminLayout` labeled `Khu học tập` that links to `/`.
2. Keep the admin return path inside the existing dashboard dropdown/menu in `MainLayout`.
3. Do not add new standalone buttons, redirects, or role-switch state.

## Why This Approach

- It matches the requested behavior with the smallest surface-area change.
- It avoids touching working auth and route protection logic.
- It preserves the current UI pattern in the public area, where admin-only controls are already grouped under the dashboard menu.

## Implementation Scope

### Admin Layout

Update `frontend/src/components/layout/AdminLayout.jsx`:

- Add a navigation item `Khu học tập` in the sidebar navigation list.
- The item links to `/`.
- The item is always visible in the admin layout because only admins can render this layout.

### Main Layout

Update `frontend/src/components/layout/MainLayout.jsx` only if needed for clarity:

- Keep the admin-only `Dashboard` navigation group visible when `user.role === "Admin"`.
- Ensure the group continues to include a direct path back to `/dashboard`.
- No new standalone CTA is added to the header.

### Routing and Auth

No planned changes to:

- `frontend/src/routes/AppRoutes.jsx`
- `frontend/src/auth/RequireAuth.jsx`

Those files already support the intended access model.

## Testing

Add or update UI tests to cover:

1. `AdminLayout` renders a `Khu học tập` link pointing to `/`.
2. `MainLayout` still shows the admin dashboard navigation group for admins.
3. Non-admin users do not gain any new admin navigation affordance.

## Risks

- Very low risk. This is a presentational navigation change layered on top of existing route protection.
- The only meaningful regression risk is accidentally changing active nav styling or breaking existing layout tests.

## Out of Scope

- Automatic redirects between dashboard and learning area.
- A role-switch toggle.
- Any change to backend authorization.
