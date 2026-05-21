# Course Structure Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mo rong luong generate course hien tai de tao day du `Course -> Module -> Lesson` skeleton tu `Syllabus` va bo sung admin UI xem/sua structure.

**Architecture:** Backend `ASP.NET Core Web API` se them `Module` va `Lesson`, parser rule-based, va transaction cho generate flow. Frontend admin se them man course structure de xem va sua metadata co ban cua module/lesson, khong goi AI hay media worker o vong nay.

**Tech Stack:** ASP.NET Core Web API, Entity Framework Core, xUnit, React, React Router, Axios, Vitest, Testing Library.

---

## File map

### Backend create
- `backend/CourseVideo.API/Models/Module.cs`
- `backend/CourseVideo.API/Models/Lesson.cs`
- `backend/CourseVideo.API/DTOs/Courses/CourseStructureResponse.cs`
- `backend/CourseVideo.API/DTOs/Courses/ModuleStructureResponse.cs`
- `backend/CourseVideo.API/DTOs/Courses/LessonStructureResponse.cs`
- `backend/CourseVideo.API/DTOs/Modules/UpdateModuleRequest.cs`
- `backend/CourseVideo.API/DTOs/Lessons/UpdateLessonRequest.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IModuleRepository.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- `backend/CourseVideo.API/Repositories/ModuleRepository.cs`
- `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- `backend/CourseVideo.API/Services/Interfaces/ICourseStructureParser.cs`
- `backend/CourseVideo.API/Services/CourseStructureParser.cs`
- `backend/CourseVideo.API/Services/Interfaces/IModuleService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonService.cs`
- `backend/CourseVideo.API/Services/ModuleService.cs`
- `backend/CourseVideo.API/Services/LessonService.cs`
- `backend/CourseVideo.API.Tests/Services/CourseStructureParserTests.cs`
- `backend/CourseVideo.API.Tests/Services/ModuleServiceTests.cs`
- `backend/CourseVideo.API.Tests/Services/LessonServiceTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`

### Backend modify
- `backend/CourseVideo.API/Models/Course.cs`
- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Data/DbInitializer.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- `backend/CourseVideo.API/Controllers/CoursesController.cs`
- `backend/CourseVideo.API/Program.cs`

### Frontend create
- `frontend/src/api/courseStructureService.js`
- `frontend/src/pages/CourseStructurePage.jsx`
- `frontend/src/pages/CourseStructurePage.test.jsx`

### Frontend modify
- `frontend/src/routes/AppRoutes.jsx`
- `frontend/src/components/layout/MainLayout.jsx`
- `frontend/src/pages/GenerationJobsPage.jsx`
- `frontend/src/styles/theme.css`

### Docs modify
- `docs/project-function-checklist.md`

### Verification commands
- Backend clean copy: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
- Frontend clean copy: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`

### Task 1: Backend parser and domain tests first

**Files:**
- Create: `backend/CourseVideo.API.Tests/Services/CourseStructureParserTests.cs`
- Modify: `backend/CourseVideo.API.Tests/Services/CourseGenerationServiceTests.cs`

- [ ] **Step 1: Write failing parser test for heading-based structure generation**

Add a parser test equivalent to:

```csharp
[Fact]
public void Parse_ShouldCreateModulesAndLessons_WhenHeadingsExist()
{
    var text = "Chuong 1: Tong quan\nBai 1: Gioi thieu\nNoi dung...\nBai 2: Nen tang\nNoi dung...";

    var result = parser.Parse(text);

    result.Modules.Should().HaveCount(1);
    result.Modules[0].Lessons.Should().HaveCount(2);
    result.Modules[0].Lessons[0].ContentSeed.Should().NotBeNullOrWhiteSpace();
}
```

- [ ] **Step 2: Write failing parser test for fallback behavior**

Add a test equivalent to:

```csharp
[Fact]
public void Parse_ShouldFallbackToDefaultModule_WhenNoHeadingsExist()
{
    var text = "Khoi kien thuc 1\nDoan noi dung A\n\nDoan noi dung B";

    var result = parser.Parse(text);

    result.Modules.Should().ContainSingle(module => module.Title == "Tong quan khoa hoc");
    result.Modules[0].Lessons.Should().NotBeEmpty();
}
```

- [ ] **Step 3: Extend course generation tests to require module/lesson creation and completed-job rollback safety**

Add assertions equivalent to:

```csharp
capturedModules.Should().NotBeEmpty();
capturedLessons.Should().NotBeEmpty();
capturedLessons.Should().OnlyContain(lesson => !string.IsNullOrWhiteSpace(lesson.ContentSeed));
```

and one failure-path assertion that if module/lesson save throws, job is marked `Failed`.

- [ ] **Step 4: Run targeted backend tests to verify they fail**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "CourseStructureParserTests|CourseGenerationServiceTests"`
Expected: FAIL because parser/models/repos do not exist yet.

### Task 2: Backend models, DbContext, and repositories

**Files:**
- Create: `backend/CourseVideo.API/Models/Module.cs`
- Create: `backend/CourseVideo.API/Models/Lesson.cs`
- Modify: `backend/CourseVideo.API/Models/Course.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Create: `backend/CourseVideo.API/Repositories/Interfaces/IModuleRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/ModuleRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/CourseRepository.cs`

- [ ] **Step 1: Add `Module` and `Lesson` entities with navigation and ordering fields**

Create concrete shapes:

```csharp
public class Module : BaseEntity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public Course? Course { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

public class Lesson : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string ContentSeed { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? AudioUrl { get; set; }
    public int? Duration { get; set; }
    public Module? Module { get; set; }
}
```

- [ ] **Step 2: Extend `Course` with `Modules` collection**

Add:

```csharp
public ICollection<Module> Modules { get; set; } = new List<Module>();
```

- [ ] **Step 3: Register `DbSet`s and EF mappings**

Add `DbSet<Module>` and `DbSet<Lesson>` plus mappings for required strings, `OrderIndex`, `ContentSeed`, and relationships:

```csharp
entity.HasOne(module => module.Course)
    .WithMany(course => course.Modules)
    .HasForeignKey(module => module.CourseId);
```

and

```csharp
entity.HasOne(lesson => lesson.Module)
    .WithMany(module => module.Lessons)
    .HasForeignKey(lesson => lesson.ModuleId);
```

- [ ] **Step 4: Extend SQL Server initializer helpers**

Add raw-SQL schema patch helpers to create `Modules` and `Lessons` tables if missing, with indexes on foreign keys and order fields.

- [ ] **Step 5: Add repositories for module and lesson writes/queries**

Repository contracts should include `AddRangeAsync`, update, get-by-id, and save methods needed by generation and admin edits.

- [ ] **Step 6: Run targeted backend tests again**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "CourseStructureParserTests|CourseGenerationServiceTests"`
Expected: FAIL moves to missing parser/service implementation instead of missing models.

### Task 3: Backend parser and generation flow implementation

**Files:**
- Create: `backend/CourseVideo.API/Services/Interfaces/ICourseStructureParser.cs`
- Create: `backend/CourseVideo.API/Services/CourseStructureParser.cs`
- Modify: `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- Modify: `backend/CourseVideo.API/Program.cs`

- [ ] **Step 1: Define parser contract and in-memory result shape**

Create a small contract like:

```csharp
public interface ICourseStructureParser
{
    ParsedCourseStructure Parse(string extractedText);
}
```

with parser result types that expose modules and lessons before persistence.

- [ ] **Step 2: Implement heading-based parsing**

Implement recognition for normalized lines beginning with `Chuong`, `Phan`, `Module`, `Unit`, `Bai`, or `Lesson`. Create modules for large headings and lessons for small headings under the current module.

- [ ] **Step 3: Implement fallback parser**

If no valid headings are detected:
- create one module titled `Tong quan khoa hoc`
- split text into meaningful blocks
- map each block to one lesson with summary `Description` and full `ContentSeed`

- [ ] **Step 4: Extend `CourseGenerationService` to persist structure transactionally**

After creating the course, parse syllabus text, create modules/lessons with `OrderIndex`, and save them before marking job completed. Use an EF Core transaction so that if any save fails, course/modules/lessons rollback together.

- [ ] **Step 5: Run parser and generation tests**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "CourseStructureParserTests|CourseGenerationServiceTests"`
Expected: PASS.

### Task 4: Backend structure query and edit APIs

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Courses/CourseStructureResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/ModuleStructureResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/LessonStructureResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Modules/UpdateModuleRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Lessons/UpdateLessonRequest.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/IModuleService.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonService.cs`
- Create: `backend/CourseVideo.API/Services/ModuleService.cs`
- Create: `backend/CourseVideo.API/Services/LessonService.cs`
- Modify: `backend/CourseVideo.API/Controllers/CoursesController.cs`
- Create or modify controllers for modules/lessons as needed
- Create: `backend/CourseVideo.API.Tests/Services/ModuleServiceTests.cs`
- Create: `backend/CourseVideo.API.Tests/Services/LessonServiceTests.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`

- [ ] **Step 1: Write failing tests for course structure detail and edit flows**

Add tests that require:
- `GET /api/courses/{id}/structure` returns nested modules/lessons
- `PUT /api/modules/{id}` updates `Title`, `Description`, `OrderIndex`
- `PUT /api/lessons/{id}` updates `Title`, `Description`, `OrderIndex`

- [ ] **Step 2: Create DTOs for structure and update requests**

Structure DTOs should expose nested arrays and metadata needed by admin UI.

- [ ] **Step 3: Implement module and lesson services**

Services should validate non-empty titles, normalize order values, update `UpdatedAt`, and save changes.

- [ ] **Step 4: Expose admin-only APIs**

Add endpoints:
- `GET /api/courses/{id}/structure`
- `PUT /api/modules/{id}`
- `PUT /api/lessons/{id}`

- [ ] **Step 5: Run backend full suite**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
Expected: PASS.

### Task 5: Frontend tests and structure page wiring

**Files:**
- Create: `frontend/src/api/courseStructureService.js`
- Create: `frontend/src/pages/CourseStructurePage.test.jsx`
- Modify: `frontend/src/pages/GenerationJobsPage.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`
- Modify: `frontend/src/components/layout/MainLayout.jsx`

- [ ] **Step 1: Write failing frontend test for structure page rendering**

Add a test that expects:
- course title visible
- module cards visible
- lesson rows visible
- edit controls for title/description/order

- [ ] **Step 2: Implement API client for structure fetch and updates**

Create helpers:

```javascript
export async function getCourseStructure(id) { ... }
export async function updateModule(id, payload) { ... }
export async function updateLesson(id, payload) { ... }
```

- [ ] **Step 3: Link from generation jobs page to course structure page**

If a job has `courseId`, render a button or link to `/admin/courses/:id/structure`.

- [ ] **Step 4: Register admin route**

Add protected route for `CourseStructurePage` under admin auth.

- [ ] **Step 5: Run targeted frontend tests to verify red-to-green progress**

Run: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run src/pages/CourseStructurePage.test.jsx`
Expected: FAIL until page implementation exists.

### Task 6: Frontend structure page implementation

**Files:**
- Create: `frontend/src/pages/CourseStructurePage.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Build read/edit structure UI with current design system primitives**

Render:
- course header
- module cards stacked vertically
- lessons inside each module
- inline form controls for `Title`, `Description`, `OrderIndex`

- [ ] **Step 2: Implement save actions for module and lesson edits**

Use optimistic local state or reload after save. Show `ui-alert` success/error messages.

- [ ] **Step 3: Add empty and loading states**

Handle cases where course has no modules or API load fails.

- [ ] **Step 4: Add small CSS helpers**

Extend theme with classes for structure grid, lesson rows, inline edit groups, and admin action bars while preserving the current visual language.

- [ ] **Step 5: Run full frontend verification**

Run: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`
Expected: PASS.

### Task 7: Checklist update and final verification

**Files:**
- Modify: `docs/project-function-checklist.md`

- [ ] **Step 1: Update checklist statuses**

Mark complete where evidence exists:
- `Thiet ke bang Modules`
- `Thiet ke bang Lessons`
- `Tao tu dong cau truc Course -> Module -> Lesson`
- `API xem chi tiet khoa hoc`
- admin structure page items

- [ ] **Step 2: Run final verification commands**

Run:
- `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
- `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm install && npm run test -- --run && npm run build`
Expected: PASS.
