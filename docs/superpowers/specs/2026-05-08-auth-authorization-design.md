# Auth + Authorization Design

Date: 2026-05-08

## Goal

Add production-style authentication and role-based authorization to the current ASP.NET Core API so the project can support:

- self-registration for learners
- secure login with JWT
- refresh-token-based session continuation
- role-based access for `Admin` and `User`
- account deactivation and password change flows

This scope is intentionally the next MVP foundation before syllabus import, course generation, and admin-only course management.

## Current Context

The existing scaffold already provides:

- `User`, `Role`, and `Course` entities
- seeded roles `Admin` and `User`
- a placeholder `AuthService`
- SQL Server via EF Core
- layered structure: `Controller -> Service -> Repository -> DbContext`

The current auth flow is not real yet:

- login returns a fake token
- passwords are not hashed using a real auth flow
- there is no JWT middleware
- there are no protected endpoints
- there is no refresh token persistence

## Decisions

### Functional Scope

This auth iteration will include:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/logout-all`
- `GET /api/auth/me`
- `POST /api/auth/change-password`
- `GET /api/users`
- `PATCH /api/users/{id}/active`

This iteration will not include:

- forgot-password email flow
- external login providers
- MFA
- device fingerprinting
- admin-created users

### Registration Model

- Anyone can self-register.
- Self-registered accounts are always assigned the `User` role.
- Admin account creation is not exposed as an API in this iteration.

### Admin Bootstrap

- The first admin account is created from environment configuration during application startup.
- Admin seeding only runs if there is no existing admin account.
- Required environment-backed config:
  - `AdminSeed:FullName`
  - `AdminSeed:Email`
  - `AdminSeed:Password`

### Password Handling

- Password hashing uses `PasswordHasher<User>`.
- Plaintext passwords are never stored.
- Password verification also uses `PasswordHasher<User>`.

### Token Strategy

- Authentication uses JWT bearer tokens.
- The system issues:
  - short-lived `access token`
  - long-lived `refresh token`
- Refresh tokens are stored server-side in the database.
- Only the hash of the refresh token is stored.
- Refresh uses token rotation:
  - old refresh token is revoked
  - a new refresh token is issued

### Session Revocation

Supported logout behaviors:

- logout current session
- logout all sessions

When a user is deactivated:

- new login attempts are blocked
- all refresh tokens for that user are revoked
- already-issued access tokens remain valid until they expire naturally

When a user changes password:

- password hash is replaced
- all refresh tokens are revoked

### Language And Localization

- The product is intended for Vietnamese users.
- All user-facing website content in this project should default to Vietnamese with proper diacritics.
- In this auth iteration, that rule applies to:
  - login and register page labels
  - button text
  - empty-state or status text on auth-related screens
  - validation and error messages that are shown directly to end users
- Internal code identifiers, database schema, and developer-facing names remain in English.
- API field names remain stable and English-friendly for development, but messages meant for direct UI display should be written in Vietnamese.

## Architecture

The implementation should stay aligned with the existing layered structure.

### Models

Existing:

- `User`
- `Role`

New:

- `RefreshToken`

Recommended `RefreshToken` fields:

- `Id`
- `UserId`
- `TokenHash`
- `ExpiresAt`
- `CreatedAt`
- `RevokedAt`
- `ReplacedByTokenHash`
- `CreatedByIp`
- `RevokedByIp`

Purpose:

- support multiple concurrent sessions per user
- support per-session logout
- support global logout
- support rotation and revocation checks

### Data Layer

Update `AppDbContext` to include:

- `DbSet<RefreshToken>`

Configure:

- primary key
- foreign key to `User`
- index on `UserId`
- index or lookup path for `TokenHash`

Update `DbInitializer` to:

- seed roles as it does today
- create the first admin from environment configuration when missing

### Repositories

Add:

- `IUserRepository` / `UserRepository`
- `IRefreshTokenRepository` / `RefreshTokenRepository`

Responsibilities:

- `UserRepository`
  - fetch by id
  - fetch by email
  - list users
  - update active status
- `RefreshTokenRepository`
  - create token record
  - find active token by hash
  - revoke one token
  - revoke all tokens for user

### Services

Extend auth into focused units:

- `IAuthService` / `AuthService`
- `ITokenService` / `TokenService`

Responsibilities:

- `AuthService`
  - register
  - login
  - refresh
  - logout
  - logout all
  - me
  - change password
- `TokenService`
  - create access token
  - create refresh token
  - hash refresh token for persistence
  - read JWT claims consistently

### Controllers

#### `AuthController`

Owns:

- register
- login
- refresh
- logout
- logout-all
- me
- change-password

#### `UsersController`

Owns admin-only user management:

- list users
- activate/deactivate user

## API Contract

### `POST /api/auth/register`

Public endpoint.

Request:

- `fullName`
- `email`
- `password`

Behavior:

- validate uniqueness of email
- create user with role `User`
- hash password
- auto-login the new user

Response:

- `accessToken`
- `refreshToken`
- `user`

### `POST /api/auth/login`

Public endpoint.

Request:

- `email`
- `password`

Response:

- `accessToken`
- `refreshToken`
- `user`

### `POST /api/auth/refresh`

Public endpoint.

Request:

- `refreshToken`

Behavior:

- validate token hash
- ensure token exists, is not revoked, is not expired
- ensure user still exists and is active
- rotate refresh token

Response:

- new `accessToken`
- new `refreshToken`
- `user`

### `POST /api/auth/logout`

Authenticated endpoint.

Request:

- `refreshToken`

Behavior:

- revoke the provided session token if it belongs to the current user

### `POST /api/auth/logout-all`

Authenticated endpoint.

Behavior:

- revoke all refresh tokens for the current user

### `GET /api/auth/me`

Authenticated endpoint.

Response:

- current user id
- full name
- email
- role
- active status

### `POST /api/auth/change-password`

Authenticated endpoint.

Request:

- `currentPassword`
- `newPassword`

Behavior:

- verify current password
- update hash
- revoke all refresh tokens

### `GET /api/users`

Admin-only endpoint.

Behavior:

- list users for administration

### `PATCH /api/users/{id}/active`

Admin-only endpoint.

Request:

- `isActive`

Behavior:

- update target user's active status
- if changed to inactive, revoke all their refresh tokens

## Authorization Rules

- `register`, `login`, `refresh` are public
- `me`, `logout`, `logout-all`, `change-password` require authentication
- `GET /api/users` requires `Admin`
- `PATCH /api/users/{id}/active` requires `Admin`

JWT access token should include these claims at minimum:

- `sub` as user id
- `email`
- `role`
- `name`

This supports both:

- user identity lookup from claims
- `[Authorize(Roles = "Admin")]`

## Configuration

Add configuration for:

- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SecretKey`
- `Jwt:AccessTokenMinutes`
- `Jwt:RefreshTokenDays`
- `AdminSeed:FullName`
- `AdminSeed:Email`
- `AdminSeed:Password`

These values should work from `appsettings` and environment variables so Docker-based deployment remains straightforward.

## Error Handling

Use simple and consistent API responses for this iteration.

Recommended status behavior:

- duplicate email: `400 Bad Request`
- invalid login credentials: `401 Unauthorized`
- inactive user login attempt: `403 Forbidden`
- invalid or expired refresh token: `401 Unauthorized`
- wrong current password: `400 Bad Request`
- missing or invalid bearer token: `401 Unauthorized`
- authenticated non-admin hitting admin endpoint: `403 Forbidden`

The implementation does not need a full exception framework in this iteration. Clear, consistent controller/service responses are enough.

Where an error message is intended to be surfaced directly in the website UI, the message text should be Vietnamese with diacritics.

## Testing and Verification

Minimum verification scope:

- register succeeds for a new email
- register fails for duplicate email
- login succeeds with correct password
- login fails with incorrect password
- refresh token rotation works
- logout current session revokes only the current refresh token
- logout all sessions revokes all refresh tokens for the user
- change password revokes all refresh tokens
- inactive user cannot log in
- admin seed runs correctly from environment config
- admin endpoints reject non-admin users

Manual verification should include:

- Swagger checks for all auth endpoints
- database inspection of `RefreshTokens`
- restart of the stack without breaking existing auth records

## Implementation Boundaries

This spec is intentionally limited to the auth foundation needed before course import and generation modules.

It does not attempt to solve:

- email delivery
- frontend token persistence UX details beyond auth screens
- audit history beyond basic refresh-token metadata
- per-request active-user database checks

## Recommendation

Build this as a focused auth foundation now, then use it as the permission boundary for:

- admin-only syllabus import
- admin-only course generation
- publish/unpublish course controls
- learner course access and progress tracking
