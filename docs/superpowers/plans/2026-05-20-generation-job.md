# Generation Job + Generate Course Trigger Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Them luong admin generate course tu syllabus da import, tao `GenerationJob`, tao `Course` skeleton, va hien thi duoc danh sach/chi tiet job tren giao dien admin.

**Architecture:** Backend `ASP.NET Core Web API` giu vai tro dieu phoi trung tam: validate syllabus, chan generate trung, tao `GenerationJob`, tao `Course` skeleton, va tra ket qua cho frontend. Frontend admin mo rong man `SyllabusesPage` de bam generate va them man `GenerationJobsPage` de theo doi job theo design system hien tai.

**Tech Stack:** ASP.NET Core Web API, Entity Framework Core, xUnit, React, React Router, Axios, Vitest, Testing Library.

---

## File map

### Backend create
- `backend/CourseVideo.API/Models/GenerationJob.cs`
- `backend/CourseVideo.API/DTOs/GenerationJobs/GenerationJobListItemResponse.cs`
- `backend/CourseVideo.API/DTOs/GenerationJobs/GenerationJobDetailResponse.cs`
- `backend/CourseVideo.API/DTOs/GenerationJobs/GenerateCourseResponse.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IGenerationJobRepository.cs`
- `backend/CourseVideo.API/Repositories/GenerationJobRepository.cs`
- `backend/CourseVideo.API/Services/Interfaces/ICourseGenerationService.cs`
- `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- `backend/CourseVideo.API/Controllers/GenerationJobsController.cs`
- `backend/CourseVideo.API.Tests/Services/CourseGenerationServiceTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/GenerationJobsControllerTests.cs`

### Backend modify
- `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
- `backend/CourseVideo.API/Models/Course.cs`
- `backend/CourseVideo.API/Models/Syllabus.cs`
- `backend/CourseVideo.API/Models/User.cs`
- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Data/DbInitializer.cs`
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/Services/Interfaces/ISyllabusService.cs`
- `backend/CourseVideo.API/Services/SyllabusService.cs`

### Frontend create
- `frontend/src/api/generationJobService.js`
- `frontend/src/pages/GenerationJobsPage.jsx`
- `frontend/src/pages/GenerationJobsPage.test.jsx`

### Frontend modify
- `frontend/src/api/syllabusService.js`
- `frontend/src/pages/SyllabusesPage.jsx`
- `frontend/src/routes/AppRoutes.jsx`
- `frontend/src/components/layout/MainLayout.jsx`
- `frontend/src/styles/theme.css`
- `frontend/src/pages/SyllabusesPage.test.jsx`

### Docs modify
- `docs/project-function-checklist.md`

### Verification commands
- Backend clean copy: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
- Frontend clean copy: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm run test -- --run && npm run build`

### Task 1: Backend tests for generation flow

**Files:**
- Create: `backend/CourseVideo.API.Tests/Services/CourseGenerationServiceTests.cs`
- Modify: `backend/CourseVideo.API.Tests/Controllers/SyllabusesControllerTests.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/GenerationJobsControllerTests.cs`

- [ ] **Step 1: Write failing service tests for happy path and duplicate-running-job guard**

Add tests covering:

```csharp
[Fact]
public async Task GenerateFromSyllabusAsync_CreatesJobAndCourse_WhenSyllabusIsValid()
{
    // Arrange syllabus with ExtractedText and admin user id.
    // Assert returned job status is Completed and CourseId is populated.
}

[Fact]
public async Task GenerateFromSyllabusAsync_Throws_WhenSyllabusHasRunningJob()
{
    // Arrange existing job with Pending or Processing.
    // Assert InvalidOperationException is thrown.
}
```

- [ ] **Step 2: Write failing service tests for not-found and failed-job persistence**

Add tests covering:

```csharp
[Fact]
public async Task GenerateFromSyllabusAsync_ThrowsKeyNotFound_WhenSyllabusDoesNotExist()
{
    // Assert KeyNotFoundException.
}

[Fact]
public async Task GenerateFromSyllabusAsync_MarksJobFailed_WhenCourseCreationThrows()
{
    // Arrange repository/save path to throw during course creation.
    // Assert job persisted with Status == "Failed" and ErrorMessage not empty.
}
```

- [ ] **Step 3: Write failing controller tests for generate endpoint and jobs endpoint**

Add tests covering:

```csharp
[Fact]
public async Task Generate_ReturnsOk_WhenGenerationSucceeds()
{
    // Assert 200 with GenerateCourseResponse.
}

[Fact]
public async Task Generate_ReturnsConflict_WhenRunningJobExists()
{
    // Assert 409.
}

[Fact]
public async Task GetAll_ReturnsOk_WithJobs()
{
    // Assert 200 with list payload.
}
```

- [ ] **Step 4: Run backend tests to verify they fail**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "CourseGenerationServiceTests|SyllabusesControllerTests|GenerationJobsControllerTests"`
Expected: FAIL because generation job model/service/controller do not exist yet.

- [ ] **Step 5: Commit test scaffolding checkpoint**

```bash
git add backend/CourseVideo.API.Tests
git commit -m "test: add failing generation job tests"
```

### Task 2: Backend domain model and persistence

**Files:**
- Create: `backend/CourseVideo.API/Models/GenerationJob.cs`
- Modify: `backend/CourseVideo.API/Models/Course.cs`
- Modify: `backend/CourseVideo.API/Models/Syllabus.cs`
- Modify: `backend/CourseVideo.API/Models/User.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`

- [ ] **Step 1: Add `GenerationJob` entity and navigation properties**

Create model with concrete properties:

```csharp
public class GenerationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SyllabusId { get; set; }
    public Guid? CourseId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Syllabus? Syllabus { get; set; }
    public Course? Course { get; set; }
    public User? CreatedByUser { get; set; }
}
```

- [ ] **Step 2: Extend existing models with source links and collections**

Add fields equivalent to:

```csharp
public Guid? SourceSyllabusId { get; set; }
public Syllabus? SourceSyllabus { get; set; }
public ICollection<GenerationJob> GenerationJobs { get; set; } = new List<GenerationJob>();
```

Apply where appropriate in `Course`, `Syllabus`, and `User`.

- [ ] **Step 3: Register entity mappings in `AppDbContext`**

Add:

```csharp
public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();
```

and `OnModelCreating` mappings for required relationships, max lengths, and delete behavior that avoids cascade cycles.

- [ ] **Step 4: Ensure table creation in `DbInitializer`**

Follow existing raw-SQL helper pattern to create `GenerationJobs` columns and add any missing columns for `Courses` source linkage if the project does schema patching that way.

- [ ] **Step 5: Run backend tests to confirm model compile issues are resolved**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "CourseGenerationServiceTests|GenerationJobsControllerTests"`
Expected: FAIL moves from missing-model errors to missing service/repository/controller behavior.

### Task 3: Backend repositories, service contracts, and generation implementation

**Files:**
- Create: `backend/CourseVideo.API/Repositories/Interfaces/IGenerationJobRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/GenerationJobRepository.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ICourseGenerationService.cs`
- Create: `backend/CourseVideo.API/Services/CourseGenerationService.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ISyllabusService.cs`
- Modify: `backend/CourseVideo.API/Services/SyllabusService.cs`
- Modify: `backend/CourseVideo.API/Program.cs`

- [ ] **Step 1: Add repository contract for generation jobs**

Create interface with exact behaviors needed:

```csharp
public interface IGenerationJobRepository
{
    Task AddAsync(GenerationJob job);
    Task<GenerationJob?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<GenerationJob>> GetAllAsync();
    Task<bool> HasRunningJobForSyllabusAsync(Guid syllabusId);
    Task SaveChangesAsync();
}
```

- [ ] **Step 2: Implement repository with eager loading for syllabus/course/user**

Use EF Core queries such as:

```csharp
return await _dbContext.GenerationJobs
    .Include(job => job.Syllabus)
    .Include(job => job.Course)
    .Include(job => job.CreatedByUser)
    .OrderByDescending(job => job.CreatedAt)
    .ToListAsync();
```

and running-job guard:

```csharp
return await _dbContext.GenerationJobs.AnyAsync(job =>
    job.SyllabusId == syllabusId &&
    (job.Status == "Pending" || job.Status == "Processing"));
```

- [ ] **Step 3: Define generation service contract and DTO expectations**

Create interface:

```csharp
public interface ICourseGenerationService
{
    Task<GenerateCourseResponse> GenerateFromSyllabusAsync(Guid syllabusId, Guid createdByUserId, string createdByName);
    Task<IReadOnlyList<GenerationJobListItemResponse>> GetAllJobsAsync();
    Task<GenerationJobDetailResponse?> GetJobByIdAsync(Guid id);
}
```

- [ ] **Step 4: Implement `CourseGenerationService` with job lifecycle and course creation**

Implement concrete flow:

```csharp
var syllabus = await _syllabusRepository.GetEntityByIdAsync(syllabusId)
    ?? throw new KeyNotFoundException("Khong tim thay de cuong.");

if (string.IsNullOrWhiteSpace(syllabus.ExtractedText))
    throw new InvalidOperationException("De cuong chua co noi dung de generate khoa hoc.");

if (await _generationJobRepository.HasRunningJobForSyllabusAsync(syllabusId))
    throw new InvalidOperationException("De cuong nay dang co job generate dang chay.");

var job = new GenerationJob { ... Status = "Pending" ... };
await _generationJobRepository.AddAsync(job);
await _generationJobRepository.SaveChangesAsync();

try
{
    job.Status = "Processing";
    job.StartedAt = DateTime.UtcNow;

    var course = new Course
    {
        Title = syllabus.Title,
        Description = !string.IsNullOrWhiteSpace(syllabus.Description)
            ? syllabus.Description
            : syllabus.ExtractedText[..Math.Min(280, syllabus.ExtractedText.Length)],
        SourceSyllabusId = syllabus.Id,
        IsPublished = false
    };

    _dbContext.Courses.Add(course);
    await _dbContext.SaveChangesAsync();

    job.CourseId = course.Id;
    job.Status = "Completed";
    job.CompletedAt = DateTime.UtcNow;
    job.UpdatedAt = DateTime.UtcNow;
    await _generationJobRepository.SaveChangesAsync();
}
catch (Exception ex)
{
    job.Status = "Failed";
    job.ErrorMessage = ex.Message;
    job.CompletedAt = DateTime.UtcNow;
    job.UpdatedAt = DateTime.UtcNow;
    await _generationJobRepository.SaveChangesAsync();
    throw;
}
```

Adjust for exact `Course` model fields that already exist in the repo.

- [ ] **Step 5: Register repositories and services in `Program.cs` and expose syllabus entity lookup if needed**

Add service registrations for `IGenerationJobRepository` and `ICourseGenerationService`, and if needed extend syllabus repository/service with an entity fetch used internally by generation.

- [ ] **Step 6: Run focused backend tests**

Run: `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "CourseGenerationServiceTests"`
Expected: PASS for service tests, controller tests may still fail until endpoints exist.

### Task 4: Backend DTOs and controllers

**Files:**
- Create: `backend/CourseVideo.API/DTOs/GenerationJobs/GenerationJobListItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/GenerationJobs/GenerationJobDetailResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/GenerationJobs/GenerateCourseResponse.cs`
- Modify: `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
- Create: `backend/CourseVideo.API/Controllers/GenerationJobsController.cs`

- [ ] **Step 1: Create response DTOs for generate result, list, and detail**

Include fields such as:

```csharp
public class GenerateCourseResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid SyllabusId { get; set; }
    public Guid? CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

and list/detail DTOs with `SyllabusTitle`, `CourseTitle`, `ErrorMessage`, `StartedAt`, `CompletedAt`.

- [ ] **Step 2: Add generate endpoint to `SyllabusesController`**

Add action shape:

```csharp
[HttpPost("{id:guid}/generate")]
public async Task<IActionResult> Generate(Guid id)
{
    try
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
        var currentUserName = User.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? "Admin";

        var result = await _courseGenerationService.GenerateFromSyllabusAsync(id, currentUserId, currentUserName);
        return Ok(result);
    }
    catch (KeyNotFoundException exception)
    {
        return NotFound(new { message = exception.Message });
    }
    catch (InvalidOperationException exception) when (exception.Message.Contains("dang chay"))
    {
        return Conflict(new { message = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return BadRequest(new { message = exception.Message });
    }
}
```

- [ ] **Step 3: Add `GenerationJobsController` list/detail endpoints**

Create controller:

```csharp
[ApiController]
[Route("api/generation-jobs")]
[Authorize(Roles = "Admin")]
public class GenerationJobsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GenerationJobListItemResponse>>> GetAll() => ...

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) => ...
}
```

- [ ] **Step 4: Run backend controller tests and then the full backend suite**

Run:
- `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests --filter "SyllabusesControllerTests|GenerationJobsControllerTests"`
- `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
Expected: PASS.

### Task 5: Frontend tests and API wiring

**Files:**
- Create: `frontend/src/api/generationJobService.js`
- Modify: `frontend/src/api/syllabusService.js`
- Modify: `frontend/src/pages/SyllabusesPage.test.jsx`
- Create: `frontend/src/pages/GenerationJobsPage.test.jsx`

- [ ] **Step 1: Write failing frontend test for syllabus generate action**

Add assertions like:

```jsx
it("calls generate API and shows success message", async () => {
  // render page with selected syllabus
  // click "Generate khoa hoc"
  // expect generateSyllabusCourse called with syllabus id
  // expect success alert rendered
});
```

- [ ] **Step 2: Write failing frontend test for generation jobs page list rendering**

Add assertions like:

```jsx
it("renders generation jobs with status and course title", async () => {
  // mock list API
  // expect syllabus title, status badge, and course title in UI
});
```

- [ ] **Step 3: Implement API helpers**

Create `generationJobService.js` with:

```javascript
import api from "./client";

export async function getGenerationJobs() {
  const response = await api.get("/generation-jobs");
  return response.data;
}

export async function getGenerationJobDetail(id) {
  const response = await api.get(`/generation-jobs/${id}`);
  return response.data;
}
```

and extend `syllabusService.js`:

```javascript
export async function generateSyllabusCourse(id) {
  const response = await api.post(`/syllabuses/${id}/generate`);
  return response.data;
}
```

- [ ] **Step 4: Run frontend tests to verify API layer expectations pass and UI tests still fail**

Run: `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm run test -- --run SyllabusesPage.test.jsx GenerationJobsPage.test.jsx`
Expected: partial progress, still FAIL until pages are implemented.

### Task 6: Frontend pages, routing, and design-system UI

**Files:**
- Modify: `frontend/src/pages/SyllabusesPage.jsx`
- Create: `frontend/src/pages/GenerationJobsPage.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`
- Modify: `frontend/src/components/layout/MainLayout.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Add generate button and state handling to `SyllabusesPage.jsx`**

Integrate behaviors:

```jsx
const [isGenerating, setIsGenerating] = useState(false);

async function handleGenerate() {
  if (!selected) return;
  setMessage("");
  setErrorMessage("");
  setIsGenerating(true);
  try {
    const result = await generateSyllabusCourse(selected.id);
    setMessage(`Da tao job generate va course draft: ${result.courseTitle}.`);
  } catch (error) {
    setErrorMessage(error?.response?.data?.message ?? "Khong the generate khoa hoc tu de cuong.");
  } finally {
    setIsGenerating(false);
  }
}
```

Render a second action button in the detail header.

- [ ] **Step 2: Create `GenerationJobsPage.jsx` with list/detail admin view**

Build page using existing primitives `Section`, `PageHeader`, `Card`, `Button`, and a list/detail split similar to syllabuses. Show status badge, syllabus title, course title, timestamps, and error message.

- [ ] **Step 3: Register route and admin navigation item**

Add protected route:

```jsx
<Route
  path="/admin/generation-jobs"
  element={
    <RequireAuth requiredRole="Admin">
      <GenerationJobsPage />
    </RequireAuth>
  }
/>
```

and add nav item label like `Jobs` or `Tiến trình` under admin navigation.

- [ ] **Step 4: Add minimal CSS tokens/classes for job list and status badges**

Extend `theme.css` with classes for `.status-badge`, `.status-badge--pending`, `.status-badge--processing`, `.status-badge--completed`, `.status-badge--failed`, and reuse current light-theme card patterns.

- [ ] **Step 5: Run frontend focused tests and then full frontend verification**

Run:
- `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm run test -- --run SyllabusesPage.test.jsx GenerationJobsPage.test.jsx`
- `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm run test -- --run && npm run build`
Expected: PASS.

### Task 7: Checklist update and final verification

**Files:**
- Modify: `docs/project-function-checklist.md`

- [ ] **Step 1: Update checklist statuses for generation job feature**

Mark complete if implemented and verified:
- `API generate course tu syllabusId`
- `Tao generation job khi admin bam generate`
- `Thiet ke bang hoac co che luu job xu ly nen`
- `Admin xem danh sach job tao khoa hoc`
- `Admin xem chi tiet tung job`
- `Nut generate khoa hoc tu de cuong`
- relevant backend/frontend test items that now have evidence

- [ ] **Step 2: Run final backend and frontend verification commands**

Run:
- `TMP_DIR=/tmp/vibecourseai-backend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r backend "$TMP_DIR" && cd "$TMP_DIR/backend" && dotnet test CourseVideo.API.Tests`
- `TMP_DIR=/tmp/vibecourseai-frontend-verify && rm -rf "$TMP_DIR" && mkdir -p "$TMP_DIR" && cp -r frontend "$TMP_DIR" && cd "$TMP_DIR/frontend" && npm run test -- --run && npm run build`
Expected: PASS on both stacks.

- [ ] **Step 3: Commit final feature**

```bash
git add backend frontend docs/project-function-checklist.md
git commit -m "feat: add generation job flow for syllabus courses"
```
