# Admin Negative Comment Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dashboard admin chi hien tong so comment tieu cuc, va cung cap trang moderation rieng de admin xem chi tiet, xoa binh luan, va khoa tai khoan hoc vien.

**Architecture:** Mo rong `GET /api/dashboard/stats` de tra ve `negativeCommentsCount`, them endpoint admin lay danh sach comment tieu cuc, va tai su dung hoac mo rong flow admin user-management/comment-management cho cac action moderation. Frontend tach ro dashboard summary va moderation detail page.

**Tech Stack:** ASP.NET Core Web API, Entity Framework Core, SQL Server, React, Vite, Vitest

---

## File Structure

- Modify: `backend/CourseVideo.API/Controllers/DashboardController.cs`
  Tra ve `negativeCommentsCount` trong dashboard stats.
- Create: `backend/CourseVideo.API/DTOs/Dashboard/DashboardStatsResponse.cs`
  DTO dashboard summary.
- Create: `backend/CourseVideo.API/DTOs/Comments/AdminNegativeCommentItemResponse.cs`
  DTO cho moderation list.
- Modify: `backend/CourseVideo.API/Controllers/UsersController.cs`
  Reuse hoac mo rong action khoa user neu can.
- Create or Modify: `backend/CourseVideo.API/Controllers/AdminCommentsController.cs`
  Them endpoint moderation list neu chua co controller phu hop.
- Test: `backend/CourseVideo.API.Tests/Controllers/DashboardControllerTests.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/AdminCommentsControllerTests.cs`
- Modify: `frontend/src/pages/DashboardPage.jsx`
  Chuyen moderation panel thanh summary card.
- Create: `frontend/src/pages/CommentModerationPage.jsx`
  Trang moderation chi tiet.
- Create: `frontend/src/pages/CommentModerationPage.test.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`
  Dang ky route `/admin/comment-moderation`.
- Modify: `frontend/src/components/layout/AdminLayout.jsx`
  Them nav item moderation neu can.
- Modify: `frontend/src/api/dashboardService.js`
- Create: `frontend/src/api/adminCommentModerationService.js`

## Task Breakdown

### Task 1: Update backend dashboard summary response

- [ ] Add or update `DashboardStatsResponse` to include `negativeCommentsCount` only.
- [ ] Update `DashboardController.GetStats()` to count only comments where `Sentiment == "negative"`, `IsHidden == false`, and `DeletedAt == null`.
- [ ] Keep existing aggregate stats unchanged.
- [ ] Verify backend project build: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`

### Task 2: Add backend moderation detail endpoint

- [ ] Create `AdminNegativeCommentItemResponse` DTO with comment, lesson, course, and author fields.
- [ ] Add admin-only endpoint for negative comment list, recommended path: `GET /api/admin/comments/negative`.
- [ ] Query only comments matching moderation criteria and order newest first.
- [ ] Verify backend build again: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`

### Task 3: Support account lock action for moderation flow

- [ ] Confirm existing admin user active/inactive flow can be reused from `UsersController`.
- [ ] If existing endpoint is sufficient, use it from frontend moderation page.
- [ ] If insufficient, add a thin admin action wrapper that sets `IsActive = false` and revokes refresh tokens.
- [ ] Verify backend build: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`

### Task 4: Convert dashboard UI from detail list to summary card

- [ ] Remove inline comment detail list from `DashboardPage.jsx`.
- [ ] Render only moderation summary card with count and CTA `Xem chi tiết`.
- [ ] CTA links to `/admin/comment-moderation`.
- [ ] Add or update dashboard page test to assert summary-only behavior.
- [ ] If frontend deps are installed, run: `npm test -- --run src/pages/DashboardPage.test.jsx`

### Task 5: Build dedicated admin moderation page

- [ ] Create `CommentModerationPage.jsx`.
- [ ] Load negative comments from the new admin moderation API.
- [ ] Render list with author, course, lesson, content, created time, and sentiment badge.
- [ ] Add actions:
  - `Xóa bình luận`
  - `Khóa tài khoản`
- [ ] Remove item from list after successful moderation action.
- [ ] Add empty state when queue is empty.
- [ ] Add tests in `CommentModerationPage.test.jsx`.

### Task 6: Wire routes and admin navigation

- [ ] Add route `/admin/comment-moderation` in `AppRoutes.jsx`.
- [ ] Add admin navigation entry if needed in `AdminLayout.jsx`.
- [ ] Ensure dashboard CTA and nav both reach the new page.

### Task 7: Final verification

- [ ] Backend build:
  - `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`
- [ ] Frontend build:
  - `npm run build`
- [ ] Health check:
  - `curl -sf http://localhost:5000/api/health`
- [ ] Manual browser verification:
  - dashboard shows only count + CTA
  - moderation page lists negative comments
  - deleting comment removes it from list
  - locking account removes it from list and prevents future login

## Self-Review

- Spec coverage:
  - dashboard summary only: covered
  - moderation page: covered
  - delete comment and lock account: covered
- Placeholder scan:
  - no `TODO` or `TBD`
- Type consistency:
  - `negativeCommentsCount` remains the dashboard summary field
  - moderation detail uses a separate DTO and page/service
