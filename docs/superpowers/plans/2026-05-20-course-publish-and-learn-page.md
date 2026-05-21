# Course Publish And Learn Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Them luong `publish/unpublish` cho course, hien thi danh sach khoa hoc that o trang `Khóa học`, va them learn page voi panel trai + sidebar module/lesson collapse o ben phai.

**Architecture:** Backend `ASP.NET Core Web API` se mo rong `CourseService` va `CoursesController` de quan ly publish state, lay danh sach course theo role, va tra learn DTO. Frontend se thay `CoursesPage` demo bang du lieu that va them `CourseLearnPage` theo bo cuc hoc tap. Chua tich hop video that o vong nay.

**Tech Stack:** ASP.NET Core Web API, Entity Framework Core, xUnit, React, React Router, Axios, Vitest, Testing Library.

---

## File map

### Backend create
- `backend/CourseVideo.API/DTOs/Courses/AdminCourseListItemResponse.cs`
- `backend/CourseVideo.API/DTOs/Courses/PublishedCourseListItemResponse.cs`
- `backend/CourseVideo.API/DTOs/Courses/CourseLearnResponse.cs`
- `backend/CourseVideo.API/DTOs/Courses/CourseLearnModuleResponse.cs`
- `backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs`

### Backend modify
- `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- `backend/CourseVideo.API/Services/CourseService.cs`
- `backend/CourseVideo.API/Controllers/CoursesController.cs`
- `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`
- `backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs` if needed

### Frontend create
- `frontend/src/api/courseService.js`
- `frontend/src/pages/CourseLearnPage.jsx`
- `frontend/src/pages/CourseLearnPage.test.jsx`

### Frontend modify
- `frontend/src/pages/CoursesPage.jsx`
- `frontend/src/routes/AppRoutes.jsx`
- `frontend/src/styles/theme.css`
- `frontend/src/components/layout/MainLayout.jsx` if nav/admin shortcuts need updates
- `frontend/src/pages/CoursesPage.test.jsx` if added

### Docs modify
- `docs/project-function-checklist.md`

### Verification commands
- Backend clean copy: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
- Frontend clean copy: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`
- Runtime redeploy: `docker compose up -d --build backend frontend`

## Task 1: Backend tests for publish and learn permissions first

**Files:**
- Modify: `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`
- Create/Modify: `backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs`

- [ ] **Step 1: Add failing tests for publish/unpublish service behavior**

Cover:
- publish sets `IsPublished = true`
- unpublish sets `IsPublished = false`
- not found returns null/false depending on service contract

- [ ] **Step 2: Add failing tests for course list by audience**

Cover:
- admin list returns draft + published
- published list returns only published courses

- [ ] **Step 3: Add failing tests for learn page permissions**

Cover:
- admin can get learn payload for draft course
- user/public flow cannot get draft course learn payload
- published course returns modules + lessons + selected lesson

- [ ] **Step 4: Run targeted backend tests to verify RED state**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "CoursesControllerTests|CourseServiceTests"`
Expected: FAIL because publish/list/learn APIs do not exist yet.

## Task 2: Backend repository, DTOs, and service implementation

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Courses/AdminCourseListItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/PublishedCourseListItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/CourseLearnResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/CourseLearnModuleResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`

- [ ] **Step 1: Extend repository contract**

Add methods for:
- get all admin courses with structure counts
- get published courses
- get course by id with structure and permission filtering
- save changes after publish/unpublish

- [ ] **Step 2: Add DTOs for admin list, published list, and learn payload**

Keep payloads lean and purpose-specific.

- [ ] **Step 3: Implement service methods**

Methods should include:
- `GetAdminCoursesAsync()`
- `GetPublishedCoursesAsync()`
- `PublishAsync(Guid id)`
- `UnpublishAsync(Guid id)`
- `GetLearnPayloadAsync(Guid id, bool canPreviewDraft)`

- [ ] **Step 4: Ensure selected lesson default is deterministic**

Default to first lesson of first ordered module.

- [ ] **Step 5: Run targeted backend tests again**

Expected: service logic passes, controller tests may still fail until endpoints are added.

## Task 3: Backend controller endpoints and authorization behavior

**Files:**
- Modify: `backend/CourseVideo.API/Controllers/CoursesController.cs`
- Modify: `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`

- [ ] **Step 1: Add admin endpoints**

Add:
- `GET /api/courses/admin`
- `PUT /api/courses/{id}/publish`
- `PUT /api/courses/{id}/unpublish`

- [ ] **Step 2: Add published list and learn endpoints**

Add:
- `GET /api/courses/published`
- `GET /api/courses/{id}/learn`

- [ ] **Step 3: Handle draft access rules explicitly**

Behavior:
- admin can preview draft learn page
- normal user hitting draft learn route gets `404`

- [ ] **Step 4: Run targeted backend tests**

Expected: `CoursesControllerTests` and `CourseServiceTests` pass.

## Task 4: Frontend course list from real data

**Files:**
- Create: `frontend/src/api/courseService.js`
- Modify: `frontend/src/pages/CoursesPage.jsx`
- Create/Modify tests as needed

- [ ] **Step 1: Add API client methods**

Methods:
- `getPublishedCourses()`
- `getAdminCourses()`
- `publishCourse(id)`
- `unpublishCourse(id)`
- `getCourseLearnPayload(id)`

- [ ] **Step 2: Replace demo course cards with backend data**

Behavior:
- if admin, load admin list
- otherwise load published list
- render status badge for admin draft/published

- [ ] **Step 3: Add admin publish/unpublish actions in list UI**

Prefer simple inline actions on cards or header area.

- [ ] **Step 4: Add/adjust frontend tests for course list**

Verify:
- cards render from API
- admin sees status badge
- publish/unpublish action wiring works

## Task 5: Frontend learn page implementation

**Files:**
- Create: `frontend/src/pages/CourseLearnPage.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`
- Modify: `frontend/src/styles/theme.css`
- Create: `frontend/src/pages/CourseLearnPage.test.jsx`

- [ ] **Step 1: Add route for learn page**

Use something like:
- `/courses/:courseId/learn`

Ensure authenticated access.

- [ ] **Step 2: Build two-column learn layout**

Left panel:
- course title / lesson title
- lesson description
- placeholder player frame
- content preview

Right sidebar:
- modules ordered
- collapse/expand state
- lesson buttons with active highlight

- [ ] **Step 3: Add default selection and collapse logic**

On first load:
- select first lesson of first module
- open containing module

- [ ] **Step 4: Add empty/error states**

Handle:
- no lesson available
- load failure
- course missing or forbidden

- [ ] **Step 5: Add frontend tests**

Verify:
- default selected lesson renders
- clicking lesson updates left panel
- collapsing modules works

## Task 6: Verification, checklist update, and runtime redeploy

**Files:**
- Modify: `docs/project-function-checklist.md`

- [ ] **Step 1: Run full backend tests in clean copy**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
Expected: PASS.

- [ ] **Step 2: Run frontend tests and build in clean copy**

Run: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`
Expected: PASS.

- [ ] **Step 3: Update checklist**

Mark progress in sections for:
- publish/unpublish
- frontend admin course management
- frontend user learning page

- [ ] **Step 4: Rebuild runtime stack**

Run: `docker compose up -d --build backend frontend`
Expected: published course appears in live UI and learn page works.
