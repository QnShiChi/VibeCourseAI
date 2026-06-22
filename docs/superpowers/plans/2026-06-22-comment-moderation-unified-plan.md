# Comment Moderation Unified Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hợp nhất trang điều phối bình luận để hỗ trợ xem comment tiêu cực và tích cực, lọc theo tên người bình luận, và cho phép đẩy comment tích cực lên đầu danh sách.

**Architecture:** Backend mở rộng admin comments API thành endpoint danh sách chung có filter `sentiment` và `authorName`, đồng thời thêm action `pin` cho comment tích cực bằng cột `PinnedAt`. Frontend giữ nguyên route hiện tại nhưng đổi page sang tab `tiêu cực/tích cực`, filter tên người bình luận, và action theo sentiment.

**Tech Stack:** ASP.NET Core, Entity Framework Core, React, Vitest, xUnit

---

### Task 1: Backend moderation query and pin action

**Files:**
- Modify: `backend/CourseVideo.API/Models/LessonComment.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Modify: `backend/CourseVideo.API/DTOs/Comments/AdminNegativeCommentItemResponse.cs`
- Modify: `backend/CourseVideo.API/Controllers/AdminCommentsController.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/AdminCommentsControllerTests.cs`

- [ ] Write failing backend tests for list filtering and positive pin ordering.
- [ ] Run the targeted backend tests and verify they fail for missing API/field behavior.
- [ ] Add `PinnedAt` support plus unified admin comments query and pin endpoint.
- [ ] Re-run the targeted backend tests until they pass.

### Task 2: Frontend moderation page

**Files:**
- Modify: `frontend/src/api/adminCommentModerationService.js`
- Modify: `frontend/src/pages/CommentModerationPage.jsx`
- Modify: `frontend/src/pages/CommentModerationPage.test.jsx`

- [ ] Write failing frontend tests for sentiment tabs, author filter, and positive pin action.
- [ ] Run the targeted frontend tests and verify they fail.
- [ ] Implement the minimal UI/service changes to satisfy the new behavior.
- [ ] Re-run the targeted frontend tests until they pass.

### Task 3: Regression verification

**Files:**
- No code changes expected.

- [ ] Run backend and frontend targeted suites for the moderation feature.
- [ ] Run a quick production build for the frontend if tests pass.
- [ ] Summarize any remaining risk if full-stack manual verification is not executed.
