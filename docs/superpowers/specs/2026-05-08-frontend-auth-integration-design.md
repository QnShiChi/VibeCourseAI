# Frontend Auth Integration Design

Date: 2026-05-08

## Goal

Connect the React frontend to the completed backend auth APIs so users can register, log in, stay signed in across reloads, access protected learning pages, and see role-aware navigation in Vietnamese.

This iteration focuses on the frontend auth experience only. Backend auth is already available and should be consumed as-is.

## Current Context

The project already has:

- working backend auth endpoints for register, login, refresh, logout, logout-all, me, and change-password
- JWT access token plus database-backed refresh token
- React frontend with basic pages and routes
- Vietnamese auth page text on the current login and register screens

The frontend is still incomplete from an auth perspective:

- login and register forms are only static UI
- no token persistence exists
- no auth state exists
- no protected route exists
- navbar does not reflect auth state
- no frontend refresh-token handling exists

## Decisions

### Post-Login Redirect

- Both `Admin` and `User` should be redirected to the home page after successful login or registration.
- Admin-only behavior is exposed in navigation, not in initial redirect.

### Role-Aware Navigation

- All signed-in users should see account controls in the navbar.
- If the signed-in user has role `Admin`, the navbar should also show a `Dashboard` entry.
- Non-admin users should not see the `Dashboard` entry.

### Session Persistence

- Frontend auth state should persist in `localStorage`.
- Stored values should include:
  - `accessToken`
  - `refreshToken`
  - current user snapshot

### Token Renewal

- When an API request fails because the access token has expired, the frontend should automatically call the refresh endpoint and retry the failed request.
- If refresh also fails:
  - clear the local session
  - show a Vietnamese message explaining the session expired
  - redirect the user to the login page

### Protected Routes

- Home page remains public.
- Login and register pages remain public.
- Learning routes such as `Courses` require authentication.
- Admin UI visibility is role-based, but this iteration does not require a full admin route framework beyond exposing the `Dashboard` navbar entry and preventing normal users from using an admin-only route if one exists.

### Navbar UX

Navbar behavior should be:

- Signed out:
  - `Đăng nhập`
  - `Đăng ký`
- Signed in:
  - user full name
  - account dropdown with:
    - `Hồ sơ`
    - `Đổi mật khẩu`
    - `Đăng xuất`
- Signed in as `Admin`:
  - all signed-in items
  - plus `Dashboard`

### Language Requirement

- All user-facing text in the auth experience must be Vietnamese with proper diacritics.
- This includes:
  - labels
  - buttons
  - helper text
  - validation text
  - status messages
  - auth-related session-expiration notices
- Internal code identifiers and API field names remain in English.

## Architecture

The frontend auth integration should introduce a focused auth layer instead of scattering token logic across pages.

### Recommended Structure

Add a frontend auth unit under `src/auth/`:

- `authStorage.js`
- `authService.js`
- `AuthContext.jsx`
- `useAuth.js`
- `RequireAuth.jsx`

Existing frontend files to extend:

- `src/api/axiosClient.js`
- `src/routes/AppRoutes.jsx`
- `src/components/layout/MainLayout.jsx`
- `src/pages/LoginPage.jsx`
- `src/pages/RegisterPage.jsx`

New pages to add:

- `src/pages/ProfilePage.jsx`
- `src/pages/ChangePasswordPage.jsx`

### State Model

Frontend auth state should track:

- `user`
- `accessToken`
- `refreshToken`
- `isAuthenticated`
- `isBootstrapping`

### Auth Context Responsibility

`AuthContext` should own:

- loading persisted session from `localStorage`
- calling `me` on app startup when tokens exist
- exposing `login`
- exposing `register`
- exposing `logout`
- exposing `changePassword`
- clearing auth state when refresh fails

### Axios Responsibility

`axiosClient.js` should own:

- adding bearer token headers
- detecting an auth failure that should trigger refresh
- serializing refresh attempts so multiple requests do not all refresh at once
- retrying the original request after successful refresh

## Data Flow

### App Startup

1. Read persisted auth data from `localStorage`.
2. If no session exists:
   - app stays signed out.
3. If a session exists:
   - set temporary auth state
   - call `GET /api/auth/me`
4. If `me` succeeds:
   - keep session
   - sync user state from the server
5. If `me` fails:
   - clear local session
   - remain signed out

### Login

1. User submits email and password.
2. Frontend calls `POST /api/auth/login`.
3. On success:
   - save tokens and user in `localStorage`
   - update auth context
   - redirect to home page
4. On failure:
   - show a Vietnamese error message

### Register

1. User submits name, email, and password.
2. Frontend calls `POST /api/auth/register`.
3. On success:
   - save tokens and user in `localStorage`
   - update auth context
   - redirect to home page
4. On failure:
   - show a Vietnamese error message

### Protected Request

1. Frontend sends request with bearer token.
2. If backend returns success:
   - continue normally.
3. If backend returns a token-expired auth failure:
   - call `POST /api/auth/refresh`
   - save the new tokens
   - retry the original request
4. If refresh fails:
   - clear session
   - show `Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.`
   - redirect to login

### Logout

1. User clicks `Đăng xuất`.
2. Frontend calls `POST /api/auth/logout` with the current refresh token.
3. Whether the API succeeds or not:
   - clear local session
   - clear auth context
   - redirect to home page

### Change Password

1. User opens `Đổi mật khẩu`.
2. Frontend calls `POST /api/auth/change-password`.
3. On success:
   - show a Vietnamese success message
   - clear current session
   - redirect to login

## UX

### Login Page

Fields:

- `Email`
- `Mật khẩu`

Primary action:

- `Đăng nhập`

Common messages:

- `Email hoặc mật khẩu không đúng.`
- `Tài khoản đã bị khóa.`
- `Không thể kết nối đến máy chủ. Vui lòng thử lại.`

### Register Page

Fields:

- `Họ và tên`
- `Email`
- `Mật khẩu`

Primary action:

- `Tạo tài khoản`

Common messages:

- `Email đã tồn tại.`
- `Vui lòng nhập đầy đủ thông tin.`
- `Không thể kết nối đến máy chủ. Vui lòng thử lại.`

### Protected Navigation

If a signed-out user tries to open `Courses`:

- redirect to login
- show `Bạn cần đăng nhập để tiếp tục.`

### Account Dropdown

Show:

- current user full name
- `Hồ sơ`
- `Đổi mật khẩu`
- `Đăng xuất`

### Admin Visibility

If the user role is `Admin`, navbar also shows:

- `Dashboard`

## API Usage

The frontend should consume these backend endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/auth/change-password`

This iteration does not need frontend support for:

- `logout-all`
- admin user management screens

## Error Handling

Error handling should be simple and predictable.

Frontend should translate backend failures into concise Vietnamese messages appropriate for direct display.

Minimum mapping:

- invalid credentials -> `Email hoặc mật khẩu không đúng.`
- inactive account -> `Tài khoản đã bị khóa.`
- duplicate email -> `Email đã tồn tại.`
- expired session -> `Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.`
- network failure -> `Không thể kết nối đến máy chủ. Vui lòng thử lại.`

## Testing And Verification

Minimum verification scope:

- login page submits real API call
- register page submits real API call
- tokens persist after reload
- `me` restores session on startup
- `Courses` redirects to login when signed out
- request retry works after refresh
- refresh failure clears session and redirects to login
- admin user sees `Dashboard` in navbar
- normal user does not see `Dashboard`
- account dropdown shows Vietnamese labels

Manual verification should include:

- register through the UI
- login through the UI
- reload browser and confirm session persists
- logout through the UI
- visit protected route while signed out
- sign in as admin and confirm navbar changes

## Implementation Boundaries

This spec does not include:

- full profile editing
- avatar upload
- forgot-password email flow
- full admin dashboard implementation
- large notification/toast framework

## Recommendation

Implement frontend auth as a single cohesive layer now, then build future course and learning features on top of that shared session model.
