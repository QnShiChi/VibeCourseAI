# Google Login Design

## Overview

Add Google login to the existing VibeCourseAI authentication flow using a backend-managed OAuth redirect flow. The backend remains the source of truth for issuing the platform's JWT access token and refresh token. The frontend receives the result on a dedicated callback route and completes session bootstrap through the existing `AuthContext`.

This design keeps the current email/password login intact, avoids introducing a second auth model, and fits the repo's current JWT-based API architecture better than ASP.NET Core cookie-first external auth middleware.

## Goals

- Add a working `Đăng nhập với Google` flow from the login page.
- Allow new Google users to be auto-created with the default `User` role.
- Allow existing users with the same email to log in through Google.
- Reuse the current JWT + refresh-token session model.
- Route OAuth completion through a dedicated frontend callback page.

## Non-Goals

- Facebook login
- Multi-provider account linking UI
- Admin creation through Google
- Google signup for non-`User` roles
- Converting the app to cookie-based authentication

## User Experience

### Login page

- The current Google button becomes active.
- Clicking it sends the browser to `GET /api/auth/google/login`.
- The backend redirects the browser to Google's OAuth consent screen.

### Google callback

- After successful Google auth, Google redirects to the backend callback endpoint.
- The backend completes token exchange, resolves or creates the local user, issues the app's JWT and refresh token, then redirects to:
  - `http://localhost:3000/auth/google/callback` in local development
  - a configured frontend callback URL in other environments

### Frontend callback page

- The frontend callback page reads the OAuth result returned by the backend.
- On success, it stores the session through the same mechanism used by password login.
- It redirects:
  - admins to `/dashboard`
  - normal users to `/`
- On failure, it redirects back to `/login` with a user-facing error message.

## Backend Design

### Configuration

Add a new `GoogleAuth` configuration section:

- `ClientId`
- `ClientSecret`
- `FrontendCallbackUrl`
- `AuthorizationEndpoint` optional, defaults to Google
- `TokenEndpoint` optional, defaults to Google
- `UserInfoEndpoint` optional, defaults to Google

Expected `.env` variables:

- `GoogleAuth__ClientId`
- `GoogleAuth__ClientSecret`
- `GoogleAuth__FrontendCallbackUrl`

The Google Console redirect URI registered with Google should point to the backend callback endpoint, not the frontend route.

Example local redirect URI:

- Backend callback registered in Google Console: `http://localhost:5000/api/auth/google/callback`
- Frontend callback page used by the app: `http://localhost:3000/auth/google/callback`

### New auth endpoints

Add two endpoints under `AuthController`.

#### `GET /api/auth/google/login`

Responsibilities:

- Generate a CSRF-resistant `state` value.
- Persist the `state` server-side with short expiry.
- Build the Google authorization URL with:
  - `client_id`
  - `redirect_uri`
  - `response_type=code`
  - `scope=openid email profile`
  - `state`
- Redirect the browser to Google.

#### `GET /api/auth/google/callback`

Responsibilities:

- Validate callback `state`.
- Reject if Google returns OAuth error parameters.
- Exchange `code` for Google tokens.
- Retrieve Google user info.
- Require:
  - verified email
  - non-empty email
- Resolve local user by email.
- If user does not exist:
  - create a new user with:
    - `RoleId = 2`
    - `IsActive = true`
    - `FullName` from Google name
    - `Email` from Google email
    - `AvatarUrl` from Google picture if available
    - a placeholder `PasswordHash`
- If user exists and `IsActive = false`:
  - reject login
- If user exists and active:
  - update `AvatarUrl` only if we explicitly choose to sync it during implementation
- Issue local `accessToken` and `refreshToken` using the existing `TokenService`
- Redirect to frontend callback route with OAuth result

### Redirect payload strategy

The backend should not put raw tokens in query parameters. Use a short-lived one-time exchange token instead.

Recommended flow:

1. Backend callback creates app session tokens.
2. Backend stores them temporarily in a short-lived server-side cache keyed by a random exchange token.
3. Backend redirects to frontend:
   - `/auth/google/callback?exchangeToken=...`
4. Frontend calls a new backend endpoint:
   - `POST /api/auth/google/exchange`
5. Backend returns:
   - `accessToken`
   - `refreshToken`
   - `user`

This avoids leaking JWT and refresh token through browser history, logs, and referrer chains.

### `POST /api/auth/google/exchange`

Responsibilities:

- Accept the short-lived exchange token.
- Validate and consume it exactly once.
- Return the normal `AuthResponse`.

### Backend services

Add a dedicated service boundary, for example `IGoogleAuthService`, responsible for:

- generating authorization URL
- validating state
- exchanging auth code for Google tokens
- fetching Google profile
- resolving or creating local user
- issuing local auth response or exchange token

This keeps `AuthService` focused on core local auth concerns while reusing its token-issuance logic where appropriate.

### Data model

No new database table is required for the first version.

User matching rules:

- Primary key for Google identity mapping is `Email`
- If the Google email matches an existing account, log into that account
- If it does not exist, create a new `User` role account

Notes:

- This is intentionally simple and matches the repo's current user model.
- If later we need multi-provider linking or provider-specific account management, a separate external identity table can be added then.

### Password handling for Google-created users

The `User.PasswordHash` column is currently required. For Google-created accounts:

- store a placeholder hash value generated through the existing `PasswordHasher<User>`
- do not allow password login unless the user later sets a real password through a dedicated flow

Implementation can choose one of two safe patterns:

1. Hash a random generated secret and store it as `PasswordHash`
2. Add an explicit flag in a later refactor to distinguish local-password accounts from external-auth-only accounts

For this feature, option 1 is sufficient and avoids schema change.

## Frontend Design

### Login page integration

- Replace the inert Google button with a click handler or link that navigates the browser to `/api/auth/google/login`
- Facebook remains unchanged and inactive unless explicitly implemented later

### Callback page

Add a new page:

- `frontend/src/pages/GoogleAuthCallbackPage.jsx`

Responsibilities:

- Read `exchangeToken` from query string
- Call `POST /api/auth/google/exchange`
- Store the returned session using the existing auth session mechanism
- Redirect to:
  - `/dashboard` for admin
  - `/` for user
- Show loading state while exchanging
- Show friendly error state if exchange fails

### Auth context integration

Add a new helper in `authService.js` and `AuthContext` to support completing a social login from `exchangeToken`, while preserving the same session object shape:

- `accessToken`
- `refreshToken`
- `user`

The rest of the app should remain unaware of whether the session came from password login or Google login.

## Error Handling

### Backend errors

The backend should redirect to the frontend callback route with an error code when:

- OAuth `state` is invalid or expired
- Google returns an error
- token exchange fails
- Google email is missing
- Google email is not verified
- matched local account is inactive
- internal token issuance fails

Recommended error codes:

- `google_auth_failed`
- `google_state_invalid`
- `google_email_unverified`
- `account_locked`

### Frontend error mapping

The frontend callback page should translate backend error codes into Vietnamese UI copy, for example:

- `google_auth_failed` -> `Đăng nhập Google thất bại. Vui lòng thử lại.`
- `google_state_invalid` -> `Phiên đăng nhập Google không hợp lệ hoặc đã hết hạn.`
- `google_email_unverified` -> `Tài khoản Google chưa xác minh email.`
- `account_locked` -> `Tài khoản đã bị khóa.`

## Security Notes

- Validate OAuth `state` on callback.
- Use a short expiry for the state record.
- Use a one-time exchange token instead of redirecting raw app tokens.
- Make the exchange token single-use.
- Require `email_verified = true`.
- Do not auto-assign admin role from Google claims.
- Do not trust frontend-supplied identity data.

## Testing Strategy

### Backend

Add tests for:

- new user created from verified Google email
- existing active user logs in via Google
- inactive matched user is rejected
- invalid state is rejected
- exchange token can only be used once

Google API interaction should be abstracted behind a service or HTTP client boundary so tests can mock it cleanly.

### Frontend

Add tests for:

- login page sends the browser to Google auth start endpoint
- callback page exchanges token and stores session
- admin redirects to `/dashboard`
- normal user redirects to `/`
- callback failure shows friendly error message

## Rollout Notes

- Add the new Google auth config to `.env.example`
- Register the backend callback URI in Google Cloud Console
- Set the authorized JavaScript origin and authorized redirect URIs correctly for local and deployed environments
- Keep email/password login unchanged during rollout

## Implementation Summary

This feature will add Google login without replacing the existing auth system. The backend remains responsible for OAuth, local user resolution, and JWT issuance. The frontend only starts the flow and completes session bootstrap from a dedicated callback route. New Google users are auto-created as `User`, while existing email matches log into the same local account. Locked accounts remain blocked.
