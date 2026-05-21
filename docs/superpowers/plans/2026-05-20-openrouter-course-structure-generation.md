# OpenRouter Course Structure Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tich hop OpenRouter vao luong generate khoa hoc de backend `ASP.NET Core Web API` sinh `Course -> Module -> Lesson` bang AI theo JSON schema, co validate va fallback ky thuat ve parser noi bo.

**Architecture:** Backend C# them OpenRouter client + options + schema validation, sau do mo rong `CourseGenerationService` de dung AI la nguon sinh structure chinh. Frontend chi cap nhat nhe message/trang thai neu can, khong doi flow admin chinh. Rule-based parser hien co duoc giu lam fallback ky thuat.

**Tech Stack:** ASP.NET Core Web API, HttpClient, System.Text.Json, xUnit, Moq, React, Vitest.

---

## File map

### Backend create
- `backend/CourseVideo.API/Configuration/OpenRouterOptions.cs`
- `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterChatCompletionRequest.cs`
- `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterChatCompletionResponse.cs`
- `backend/CourseVideo.API/Services/Interfaces/IOpenRouterCourseStructureService.cs`
- `backend/CourseVideo.API/Services/OpenRouterCourseStructureService.cs`
- `backend/CourseVideo.API/Services/OpenRouterPromptFactory.cs`
- `backend/CourseVideo.API.Tests/Services/OpenRouterCourseStructureServiceTests.cs`

### Backend modify
- `backend/CourseVideo.API/appsettings.json`
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ICourseStructureParser.cs`
- `backend/CourseVideo.API.Tests/Services/CourseGenerationServiceTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/SyllabusesControllerTests.cs`
- `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
- `.env.example` if present

### Frontend modify
- `frontend/src/pages/SyllabusesPage.jsx`
- `frontend/src/pages/GenerationJobsPage.jsx`
- `frontend/src/pages/SyllabusesPage.test.jsx`
- `frontend/src/pages/GenerationJobsPage.test.jsx`

### Docs modify
- `docs/project-function-checklist.md`

### Verification commands
- Backend clean copy: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
- Frontend clean copy: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`
- Runtime redeploy: `docker compose up -d --build backend frontend`

## Task 1: OpenRouter contract and tests first

**Files:**
- Create: `backend/CourseVideo.API.Tests/Services/OpenRouterCourseStructureServiceTests.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/CourseGenerationServiceTests.cs`

- [ ] **Step 1: Write failing tests for valid AI JSON mapping**

Add tests that require an OpenRouter service to:
- call chat completions
- parse JSON schema payload
- return a validated course structure with modules and lessons

- [ ] **Step 2: Write failing tests for AI invalid schema and technical failure**

Cover at least:
- empty module list
- lesson missing contentSeed
- malformed JSON
- HTTP error / timeout equivalent

- [ ] **Step 3: Extend generation tests to require AI-first and fallback behavior**

Assert:
- `CourseGenerationService` prefers OpenRouter output when available
- if OpenRouter throws a technical exception, service falls back to local parser
- if OpenRouter returns schema-invalid output, service marks job failed or falls back according to the final chosen policy in code

- [ ] **Step 4: Run targeted backend tests to verify RED state**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "OpenRouterCourseStructureServiceTests|CourseGenerationServiceTests"`
Expected: FAIL because OpenRouter service and config do not exist yet.

## Task 2: Backend configuration and OpenRouter client

**Files:**
- Create: `backend/CourseVideo.API/Configuration/OpenRouterOptions.cs`
- Create: `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterChatCompletionRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterChatCompletionResponse.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/IOpenRouterCourseStructureService.cs`
- Create: `backend/CourseVideo.API/Services/OpenRouterCourseStructureService.cs`
- Create: `backend/CourseVideo.API/Services/OpenRouterPromptFactory.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `backend/CourseVideo.API/appsettings.json`

- [ ] **Step 1: Add strongly typed options for OpenRouter**

Fields should include:
- `ApiKey`
- `Model`
- `BaseUrl`
- `TimeoutSeconds`

- [ ] **Step 2: Define request/response DTOs for chat completions**

Keep DTOs minimal and focused on fields actually used by the project:
- `model`
- `messages`
- `temperature`
- `response_format`
- parsed assistant content

- [ ] **Step 3: Implement prompt factory and OpenRouter client**

The client should:
- send system + user messages
- request JSON schema output when supported
- deserialize assistant content
- validate business structure before returning it

- [ ] **Step 4: Register services and HttpClient in DI**

Configure:
- options binding
- typed/named HttpClient
- `IOpenRouterCourseStructureService`

- [ ] **Step 5: Run targeted backend tests again**

Expected: failures move from missing client/config to generation-flow integration or validation details.

## Task 3: Integrate AI-first generation flow with fallback

**Files:**
- Modify: `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ICourseStructureParser.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/CourseGenerationServiceTests.cs`

- [ ] **Step 1: Extend generation service dependencies**

Inject:
- `IOpenRouterCourseStructureService`
- existing `ICourseStructureParser` fallback

- [ ] **Step 2: Use OpenRouter as the primary structure source**

Flow:
- try AI generation first
- validate result
- persist course/modules/lessons from AI output

- [ ] **Step 3: Add technical fallback to local parser**

Fallback only for technical failure classes such as:
- timeout
- HTTP transport failure
- auth/rate limit if desired by final policy

Record enough detail in logs/job error trail to know fallback happened.

- [ ] **Step 4: Keep transaction and job status integrity**

Ensure:
- no half-saved structure
- `GenerationJob` is still `Completed` only after full persistence
- `Failed` path preserves useful error message

- [ ] **Step 5: Run targeted backend tests**

Expected: PASS for OpenRouter service tests and generation service tests.

## Task 4: Improve admin-facing error and status signals

**Files:**
- Modify: `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
- Modify: `frontend/src/pages/SyllabusesPage.jsx`
- Modify: `frontend/src/pages/GenerationJobsPage.jsx`
- Modify tests for both pages/controllers as needed

- [ ] **Step 1: Clarify controller responses for AI-related failures**

Return distinguishable messages for:
- missing OpenRouter config
- AI parsing failure
- fallback used but success achieved (if surfaced)

- [ ] **Step 2: Update admin UI messages**

Show better feedback when:
- generate failed because AI config is missing
- generate failed due to AI/provider error
- course was generated successfully

- [ ] **Step 3: Add/adjust frontend tests**

Verify message rendering without overfitting to implementation details.

## Task 5: Full verification and checklist update

**Files:**
- Modify: `docs/project-function-checklist.md`

- [ ] **Step 1: Run full backend test suite in clean copy**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
Expected: PASS.

- [ ] **Step 2: Run frontend test + build in clean copy**

Run: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`
Expected: PASS.

- [ ] **Step 3: Update checklist to reflect OpenRouter-powered generation**

Mark progress in sections for:
- generate course
- AI-assisted content pipeline groundwork
- testing

- [ ] **Step 4: Rebuild runtime stack**

Run: `docker compose up -d --build backend frontend`
Expected: admin can generate on current stack with OpenRouter config present.

## Task 6: Runtime smoke check with real syllabus

**Files:**
- No source changes required unless bug found

- [ ] **Step 1: Validate env config is present**

Check that runtime has:
- `OPENROUTER_API_KEY`
- `OPENROUTER_MODEL`

- [ ] **Step 2: Generate from a real imported syllabus**

Verify:
- job completes
- generated course structure is cleaner than rule-based fallback
- module/lesson naming is more realistic

- [ ] **Step 3: Record any follow-up gaps**

If output is still noisy, identify whether the next fix belongs to:
- prompt tuning
- text normalization before AI
- stricter schema validation
- post-processing cleanup
