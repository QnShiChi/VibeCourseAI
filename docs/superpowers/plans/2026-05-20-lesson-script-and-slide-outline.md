# Lesson Script And Slide Outline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Them luong admin generate noi dung bai hoc o cap `Course`, de moi `Lesson` co `TeachingScript`, `SlideOutlineJson`, `VoiceoverPlanJson`, preview/edit duoc tren giao dien admin, va san sang cho TTS/video pipeline o buoc sau.

**Architecture:** Backend `ASP.NET Core Web API` se mo rong `Lessons`, `GenerationJobs`, va service OpenRouter de sinh content cho tung lesson trong mot course job. Frontend se mo rong trang `Course Structure` de admin bam generate, theo doi trang thai tung lesson, preview `Script / Slides / Voiceover`, va chinh tay noi dung generated. Khong goi TTS hay render media o vong nay.

**Tech Stack:** ASP.NET Core Web API, Entity Framework Core, xUnit, React, Axios, Vitest, Testing Library, OpenRouter Chat Completions JSON schema.

---

## File map

### Backend create
- `backend/CourseVideo.API/DTOs/Lessons/LessonGeneratedContentResponse.cs`
- `backend/CourseVideo.API/DTOs/Lessons/UpdateLessonGeneratedContentRequest.cs`
- `backend/CourseVideo.API/DTOs/Courses/GenerateLessonContentResponse.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonContentGenerationService.cs`
- `backend/CourseVideo.API/Services/LessonContentGenerationService.cs`
- `backend/CourseVideo.API/Services/OpenRouterLessonContentService.cs`
- `backend/CourseVideo.API/Models/OpenRouter/OpenRouterLessonContentResult.cs`

### Backend modify
- `backend/CourseVideo.API/Models/Lesson.cs`
- `backend/CourseVideo.API/Models/GenerationJob.cs`
- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Data/DbInitializer.cs`
- `backend/CourseVideo.API/Configuration/OpenRouterOptions.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonService.cs`
- `backend/CourseVideo.API/Services/CourseService.cs`
- `backend/CourseVideo.API/Services/LessonService.cs`
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/Controllers/CoursesController.cs`
- `backend/CourseVideo.API/Controllers/LessonsController.cs`
- `backend/CourseVideo.API.Tests/Services/OpenRouterCourseStructureServiceTests.cs` if reusable helpers exist
- `backend/CourseVideo.API.Tests/Services/LessonContentGenerationServiceTests.cs`
- `backend/CourseVideo.API.Tests/Services/LessonServiceTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/LessonsControllerTests.cs`

### Frontend create
- `frontend/src/api/lessonContentService.js`
- `frontend/src/components/course/LessonContentEditor.jsx`
- `frontend/src/components/course/LessonContentPreview.jsx`
- `frontend/src/components/course/LessonContentStatusBadge.jsx`
- `frontend/src/components/course/LessonContentEditor.test.jsx`

### Frontend modify
- `frontend/src/api/courseStructureService.js`
- `frontend/src/pages/CourseStructurePage.jsx`
- `frontend/src/pages/CourseStructurePage.test.jsx`
- `frontend/src/styles/theme.css`

### Docs modify
- `docs/project-function-checklist.md`
- `.env.example`

### Verification commands
- Backend clean copy: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
- Frontend clean copy: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`
- Runtime redeploy: `docker compose up -d --build backend frontend`

## Task 1: Backend RED tests for lesson-content generation flow

**Files:**
- Create: `backend/CourseVideo.API.Tests/Services/LessonContentGenerationServiceTests.cs`
- Modify: `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/LessonsControllerTests.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/LessonServiceTests.cs`

- [ ] **Step 1: Add failing service tests for whole-course generation outcomes**

Cover:
- course with ordered lessons generates content for every lesson
- one lesson failure yields `CompletedWithWarnings`
- all lesson failures yield `Failed`
- invalid OpenRouter schema stores `ContentGenerationError`

- [ ] **Step 2: Add failing controller tests for admin-only endpoints**

Cover:
- `POST /api/courses/{id}/generate-lesson-content` returns success payload for admin
- normal user is forbidden from whole-course content generation
- `GET /api/lessons/{id}/content` returns generated content for admin
- `PUT /api/lessons/{id}/content` updates content for admin

- [ ] **Step 3: Add failing lesson service tests for manual content update**

Cover:
- update script/slides/voiceover succeeds
- update trims fields and clears previous error if admin overwrites bad AI output

- [ ] **Step 4: Run targeted backend tests to verify RED state**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "LessonContentGenerationServiceTests|LessonsControllerTests|CoursesControllerTests|LessonServiceTests"`
Expected: FAIL because lesson content generation services and endpoints do not exist yet.

## Task 2: Backend model, repository, and DTO plumbing

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Lessons/LessonGeneratedContentResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Lessons/UpdateLessonGeneratedContentRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/GenerateLessonContentResponse.cs`
- Modify: `backend/CourseVideo.API/Models/Lesson.cs`
- Modify: `backend/CourseVideo.API/Models/GenerationJob.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/LessonRepository.cs`

- [ ] **Step 1: Extend `Lesson` model with generated-content fields**

Add:
- `string? TeachingScript`
- `string? SlideOutlineJson`
- `string? VoiceoverPlanJson`
- `string ContentGenerationStatus`
- `DateTime? ContentGeneratedAt`
- `string? ContentGenerationError`

Use simple string statuses first to avoid unnecessary enum churn if the repo is already string-oriented.

- [ ] **Step 2: Extend `GenerationJob` for warning-capable content jobs**

Add fields only if missing and necessary for this feature:
- `string? JobType`
- `Guid? CourseId`
- `int? TotalItems`
- `int? ProcessedItems`
- `int? FailedItems`
- `string? ProgressMessage`

- [ ] **Step 3: Add DTOs for response/update payloads**

DTOs should carry:
- lesson identity and title
- generated script
- parsed slide outline object/list
- parsed voiceover plan object
- generation status/error timestamps

- [ ] **Step 4: Extend repositories to fetch ordered lessons by course and update lesson content**

`ICourseRepository` should expose course-with-structure fetch suitable for whole-course generation.
`ILessonRepository` should expose lesson-by-id with module/course context for preview/update endpoints.

- [ ] **Step 5: Run targeted backend tests again**

Expected: compile moves forward, but service/controller behavior still fails until generation logic is implemented.

## Task 3: OpenRouter lesson-content generator and orchestration service

**Files:**
- Create: `backend/CourseVideo.API/Models/OpenRouter/OpenRouterLessonContentResult.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonContentGenerationService.cs`
- Create: `backend/CourseVideo.API/Services/LessonContentGenerationService.cs`
- Create: `backend/CourseVideo.API/Services/OpenRouterLessonContentService.cs`
- Modify: `backend/CourseVideo.API/Configuration/OpenRouterOptions.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/LessonContentGenerationServiceTests.cs`

- [ ] **Step 1: Define OpenRouter lesson-content schema models**

Represent:
- `lessonId`
- `lessonTitle`
- `teachingScript`
- `slideOutline[]` with `slideNumber`, `title`, `bulletPoints[]`, `speakerNotes`
- `voiceoverPlan` with `estimatedDurationMinutes`, `tone`, `pacing`, `targetAudience`, `pronunciationNotes`

- [ ] **Step 2: Implement OpenRouter service for one lesson request**

Responsibilities:
- build prompt from course/module/lesson context
- request structured JSON
- deserialize and validate required fields
- return normalized result or throw a domain-specific exception

- [ ] **Step 3: Implement orchestration service for whole-course generation**

Responsibilities:
- create `GenerationJob` of type `GenerateLessonContent`
- iterate lessons in order
- call OpenRouter per lesson
- save successful lesson content immediately
- record per-lesson errors without aborting the whole course unnecessarily
- finish with `Completed`, `CompletedWithWarnings`, or `Failed`

- [ ] **Step 4: Register DI and config hooks**

Wire new services in `Program.cs` and extend `.env.example` only if new values beyond existing OpenRouter config are required.

- [ ] **Step 5: Run targeted backend tests**

Expected: service tests for success/partial-failure/full-failure now pass.

## Task 4: Backend lesson preview/edit and course trigger endpoints

**Files:**
- Modify: `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ILessonService.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`
- Modify: `backend/CourseVideo.API/Services/LessonService.cs`
- Modify: `backend/CourseVideo.API/Controllers/CoursesController.cs`
- Modify: `backend/CourseVideo.API/Controllers/LessonsController.cs`
- Modify: `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`
- Modify: `backend/CourseVideo.API.Tests/Controllers/LessonsControllerTests.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/LessonServiceTests.cs`

- [ ] **Step 1: Add course-level trigger method to service/controller**

Add `POST /api/courses/{id}/generate-lesson-content` as admin-only.
Return a lightweight response with job id, status, totals, and summary message.

- [ ] **Step 2: Add lesson content read/update service methods**

Methods should include:
- `GetGeneratedContentAsync(Guid lessonId)`
- `UpdateGeneratedContentAsync(Guid lessonId, UpdateLessonGeneratedContentRequest request)`

- [ ] **Step 3: Add lesson content endpoints**

Add admin-only endpoints:
- `GET /api/lessons/{id}/content`
- `PUT /api/lessons/{id}/content`

- [ ] **Step 4: Normalize update writes**

On manual save:
- trim script and text fields
- serialize slide/voiceover payload back to JSON
- clear `ContentGenerationError`
- set `ContentGenerationStatus` to a stable success/manual-edited state

- [ ] **Step 5: Run targeted backend tests**

Expected: controller and lesson-service tests pass.

## Task 5: Frontend admin generation action and lesson content preview UI

**Files:**
- Create: `frontend/src/api/lessonContentService.js`
- Create: `frontend/src/components/course/LessonContentEditor.jsx`
- Create: `frontend/src/components/course/LessonContentPreview.jsx`
- Create: `frontend/src/components/course/LessonContentStatusBadge.jsx`
- Modify: `frontend/src/api/courseStructureService.js`
- Modify: `frontend/src/pages/CourseStructurePage.jsx`
- Modify: `frontend/src/styles/theme.css`
- Create: `frontend/src/components/course/LessonContentEditor.test.jsx`
- Modify: `frontend/src/pages/CourseStructurePage.test.jsx`

- [ ] **Step 1: Add API client calls**

Methods:
- `generateCourseLessonContent(courseId)`
- `getLessonGeneratedContent(lessonId)`
- `updateLessonGeneratedContent(lessonId, payload)`

- [ ] **Step 2: Add generate action to course structure page**

Behavior:
- button at course level
- disable while request is in flight
- show result/error banner
- refresh structure and selected lesson content after generation

- [ ] **Step 3: Add lesson content status badge and preview panel**

Preview should show:
- script block
- slide cards or ordered list
- voiceover metadata summary

- [ ] **Step 4: Add editor form for manual adjustments**

Support editing:
- full `TeachingScript`
- slide titles/bullets/speaker notes in a manageable textarea/json-assisted form
- voiceover plan text fields

Keep MVP pragmatic: if nested slide editing is too heavy, use a validated JSON textarea plus human-readable preview beside it.

- [ ] **Step 5: Add frontend tests**

Verify:
- generate button shows and calls API
- status badge renders from lesson state
- preview loads generated script/slides/voiceover
- manual save calls update endpoint and shows success

## Task 6: End-to-end verification, checklist update, and runtime redeploy

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
- lesson content generation
- OpenRouter instructional content generation
- admin preview/edit of generated lesson content
- prep contract for TTS/video pipeline

- [ ] **Step 4: Rebuild runtime stack**

Run: `docker compose up -d --build backend frontend`
Expected: admin can generate lesson content from course structure page and preview/edit it live.
