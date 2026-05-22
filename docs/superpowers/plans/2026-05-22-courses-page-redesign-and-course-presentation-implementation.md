# Courses Page Redesign And Course Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign `/courses` into a searchable, filterable discovery page backed by real `category` and `thumbnailUrl` data, and let admins manage thumbnail/category from `/admin/courses/:courseId`.

**Architecture:** Keep course discovery client-side for this iteration: fetch one course list, then apply category + search filters in React. Extend the backend `Course` contract with a real category value and reusable thumbnail upload flow, expose both in course DTOs, and serve thumbnail files from backend storage under `/storage/course-thumbnails`.

**Tech Stack:** React + Vite + Vitest, Axios, CSS Modules, ASP.NET Core Web API, EF Core, SQL Server bootstrap via `DbInitializer`, xUnit + Moq + FluentAssertions

---

## File Map

- `backend/CourseVideo.API/Models/Course.cs`
  Add `Category` beside existing `ThumbnailUrl`.
- `backend/CourseVideo.API/Models/CourseCategory.cs`
  New enum for backend-safe category values.
- `backend/CourseVideo.API/Data/AppDbContext.cs`
  Configure enum/string persistence and column constraints.
- `backend/CourseVideo.API/Data/DbInitializer.cs`
  Add SQL bootstrap for `Category` and `ThumbnailUrl` defaults/column existence.
- `backend/CourseVideo.API/DTOs/Courses/AdminCourseListItemResponse.cs`
  Return `thumbnailUrl` and `category`.
- `backend/CourseVideo.API/DTOs/Courses/PublishedCourseListItemResponse.cs`
  Return `thumbnailUrl` and `category`.
- `backend/CourseVideo.API/DTOs/Courses/CourseStructureResponse.cs`
  Return `thumbnailUrl` and `category` to admin page.
- `backend/CourseVideo.API/DTOs/Courses/UpdateCourseCategoryRequest.cs`
  New JSON payload for category updates.
- `backend/CourseVideo.API/DTOs/Courses/UploadCourseThumbnailRequest.cs`
  New multipart payload for thumbnail uploads.
- `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
  Add course presentation update methods.
- `backend/CourseVideo.API/Services/CourseService.cs`
  Map new DTO fields, validate category, store thumbnail files, update course metadata.
- `backend/CourseVideo.API/Controllers/CoursesController.cs`
  Add admin endpoints for thumbnail upload and category update.
- `backend/CourseVideo.API/Program.cs`
  Serve `/storage` thumbnails as static files.
- `backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs`
  Add service tests for DTO mapping, category update, thumbnail upload validation.
- `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`
  Add controller tests for new admin endpoints and bad-request paths.
- `frontend/src/api/courseStructureService.js`
  Add `updateCourseCategory` and `uploadCourseThumbnail`.
- `frontend/src/constants/coursePresentation.js`
  One source of truth for category labels/chip metadata in React.
- `frontend/src/pages/CoursesPage.jsx`
  Replace the old simple grid with hero, search, chips, featured card, responsive card grid, empty state.
- `frontend/src/styles/CoursesPage.module.css`
  New page-specific styles matching the provided design direction.
- `frontend/src/pages/CoursesPage.test.jsx`
  Add search/filter/featured/admin-action assertions.
- `frontend/src/pages/CourseStructurePage.jsx`
  Add thumbnail preview/upload and category select block without disturbing existing generation flows.
- `frontend/src/pages/CourseStructurePage.test.jsx`
  Add admin presentation management tests.

## Task 1: Add Backend Category + DTO Contract

**Files:**
- Create: `backend/CourseVideo.API/Models/CourseCategory.cs`
- Create: `backend/CourseVideo.API/DTOs/Courses/UpdateCourseCategoryRequest.cs`
- Modify: `backend/CourseVideo.API/Models/Course.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Modify: `backend/CourseVideo.API/DTOs/Courses/AdminCourseListItemResponse.cs`
- Modify: `backend/CourseVideo.API/DTOs/Courses/PublishedCourseListItemResponse.cs`
- Modify: `backend/CourseVideo.API/DTOs/Courses/CourseStructureResponse.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`
- Test: `backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs`

- [ ] **Step 1: Write the failing service tests for category/thumbnail DTO mapping**

```csharp
[Fact]
public async Task GetPublishedCoursesAsync_MapsCategoryAndThumbnailUrl()
{
    var repository = new Mock<ICourseRepository>();
    repository.Setup(x => x.GetPublishedAsync()).ReturnsAsync(new List<Course>
    {
        new()
        {
            Id = Guid.NewGuid(),
            Title = "AI Prompting",
            Description = "Desc",
            IsPublished = true,
            Category = CourseCategory.AiAndData,
            ThumbnailUrl = "/storage/course-thumbnails/ai.png"
        }
    });

    var service = CreateCourseService(repository);

    var result = await service.GetPublishedCoursesAsync();

    result.Should().ContainSingle();
    result[0].Category.Should().Be("AiAndData");
    result[0].ThumbnailUrl.Should().Be("/storage/course-thumbnails/ai.png");
}

[Fact]
public async Task GetStructureAsync_MapsCategoryAndThumbnailUrl()
{
    var repository = new Mock<ICourseRepository>();
    var courseId = Guid.NewGuid();
    repository.Setup(x => x.GetByIdWithStructureAsync(courseId)).ReturnsAsync(new Course
    {
        Id = courseId,
        Title = "UI Systems",
        Description = "Desc",
        Category = CourseCategory.UiUxDesign,
        ThumbnailUrl = "/storage/course-thumbnails/ui.png"
    });

    var service = CreateCourseService(repository);

    var result = await service.GetStructureAsync(courseId);

    result.Should().NotBeNull();
    result!.Category.Should().Be("UiUxDesign");
    result.ThumbnailUrl.Should().Be("/storage/course-thumbnails/ui.png");
}

private static CourseService CreateCourseService(Mock<ICourseRepository> repository)
{
    return new CourseService(
        repository.Object,
        Mock.Of<ILessonContentGenerationService>(),
        Mock.Of<ILessonAudioGenerationService>(),
        Mock.Of<ILessonVideoGenerationService>());
}
```

- [ ] **Step 2: Run the backend service tests and verify they fail**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~CourseServiceTests"`

Expected: FAIL because `Course.Category`, DTO properties, and mapping code do not exist yet.

- [ ] **Step 3: Add the minimal backend contract**

```csharp
// backend/CourseVideo.API/Models/CourseCategory.cs
namespace CourseVideo.API.Models;

public enum CourseCategory
{
    UiUxDesign = 1,
    AiAndData = 2,
    Development = 3
}
```

```csharp
// backend/CourseVideo.API/Models/Course.cs
public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public CourseCategory Category { get; set; } = CourseCategory.UiUxDesign;
    public bool IsPublished { get; set; }
    // ...
}
```

```csharp
// backend/CourseVideo.API/Data/AppDbContext.cs
modelBuilder.Entity<Course>(entity =>
{
    entity.HasKey(course => course.Id);
    entity.Property(course => course.Title).HasMaxLength(200).IsRequired();
    entity.Property(course => course.Description).HasMaxLength(2000).IsRequired();
    entity.Property(course => course.ThumbnailUrl).HasMaxLength(1000);
    entity.Property(course => course.Category)
        .HasConversion<string>()
        .HasMaxLength(50)
        .HasDefaultValue(CourseCategory.UiUxDesign)
        .IsRequired();
    // ...
});
```

```sql
-- backend/CourseVideo.API/Data/DbInitializer.cs
IF COL_LENGTH('Courses', 'ThumbnailUrl') IS NULL
BEGIN
    ALTER TABLE [Courses] ADD [ThumbnailUrl] nvarchar(1000) NULL;
END

IF COL_LENGTH('Courses', 'Category') IS NULL
BEGIN
    ALTER TABLE [Courses] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_Courses_Category] DEFAULT N'UiUxDesign';
END
```

```csharp
// DTOs + mapping
public string? ThumbnailUrl { get; set; }
public string Category { get; set; } = string.Empty;

private static AdminCourseListItemResponse MapAdminListItem(Course course)
{
    return new AdminCourseListItemResponse
    {
        Id = course.Id,
        Title = course.Title,
        Description = course.Description,
        ThumbnailUrl = course.ThumbnailUrl,
        Category = course.Category.ToString(),
        // ...
    };
}
```

- [ ] **Step 4: Run the backend service tests and verify they pass**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~CourseServiceTests"`

Expected: PASS for the new mapping coverage and existing course service tests remain green.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Models/Course.cs \
  backend/CourseVideo.API/Models/CourseCategory.cs \
  backend/CourseVideo.API/Data/AppDbContext.cs \
  backend/CourseVideo.API/Data/DbInitializer.cs \
  backend/CourseVideo.API/DTOs/Courses/AdminCourseListItemResponse.cs \
  backend/CourseVideo.API/DTOs/Courses/PublishedCourseListItemResponse.cs \
  backend/CourseVideo.API/DTOs/Courses/CourseStructureResponse.cs \
  backend/CourseVideo.API/DTOs/Courses/UpdateCourseCategoryRequest.cs \
  backend/CourseVideo.API/Services/Interfaces/ICourseService.cs \
  backend/CourseVideo.API/Services/CourseService.cs \
  backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs
git commit -m "feat: add course presentation contract"
```

## Task 2: Add Backend Admin Presentation Endpoints

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Courses/UploadCourseThumbnailRequest.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`
- Modify: `backend/CourseVideo.API/Controllers/CoursesController.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs`

- [ ] **Step 1: Write failing tests for category update and thumbnail upload endpoints**

```csharp
[Fact]
public async Task UpdateCategoryAsync_PersistsParsedCategory()
{
    var repository = new Mock<ICourseRepository>();
    var course = new Course { Id = Guid.NewGuid(), Category = CourseCategory.UiUxDesign };
    repository.Setup(x => x.GetByIdAsync(course.Id)).ReturnsAsync(course);
    repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

    var service = CreateCourseService(repository);

    var result = await service.UpdateCategoryAsync(course.Id, "Development");

    result.Should().NotBeNull();
    course.Category.Should().Be(CourseCategory.Development);
}

[Fact]
public async Task UploadThumbnail_ReturnsBadRequest_WhenFileMissing()
{
    var courseService = new Mock<ICourseService>();
    var controller = CreateAdminController(courseService);

    var result = await controller.UploadThumbnail(Guid.NewGuid(), new UploadCourseThumbnailRequest(), CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
}
```

- [ ] **Step 2: Run backend tests to verify failure**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~CourseServiceTests|FullyQualifiedName~CoursesControllerTests"`

Expected: FAIL because `UpdateCategoryAsync`, `UploadThumbnailAsync`, `UploadThumbnail`, and request DTOs do not exist yet.

- [ ] **Step 3: Implement minimal admin presentation APIs**

```csharp
// backend/CourseVideo.API/DTOs/Courses/UploadCourseThumbnailRequest.cs
namespace CourseVideo.API.DTOs.Courses;

public class UploadCourseThumbnailRequest
{
    public IFormFile? File { get; set; }
}
```

```csharp
// backend/CourseVideo.API/Services/Interfaces/ICourseService.cs
Task<CourseStructureResponse?> UpdateCategoryAsync(Guid id, string category);
Task<CourseStructureResponse?> UploadThumbnailAsync(Guid id, IFormFile file, CancellationToken cancellationToken = default);
```

```csharp
// backend/CourseVideo.API/Services/CourseService.cs
private static readonly HashSet<string> AllowedThumbnailExtensions = new(StringComparer.OrdinalIgnoreCase)
{
    ".png", ".jpg", ".jpeg", ".webp"
};

public async Task<CourseStructureResponse?> UpdateCategoryAsync(Guid id, string category)
{
    var course = await _courseRepository.GetByIdAsync(id);
    if (course is null)
    {
        return null;
    }

    if (!Enum.TryParse<CourseCategory>(category, ignoreCase: true, out var parsedCategory))
    {
        throw new InvalidOperationException("Category khóa học không hợp lệ.");
    }

    course.Category = parsedCategory;
    course.UpdatedAt = DateTime.UtcNow;
    await _courseRepository.SaveChangesAsync();
    return await GetStructureAsync(id);
}
```

```csharp
// backend/CourseVideo.API/Controllers/CoursesController.cs
[HttpPut("{id:guid}/category")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCourseCategoryRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Category))
    {
        return BadRequest(new { message = "Category khóa học là bắt buộc." });
    }

    try
    {
        var updated = await _courseService.UpdateCategoryAsync(id, request.Category);
        return updated is null ? NotFound() : Ok(updated);
    }
    catch (InvalidOperationException exception)
    {
        return BadRequest(new { message = exception.Message });
    }
}

[HttpPost("{id:guid}/thumbnail")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> UploadThumbnail(Guid id, [FromForm] UploadCourseThumbnailRequest request, CancellationToken cancellationToken)
{
    if (request.File is null || request.File.Length == 0)
    {
        return BadRequest(new { message = "Vui lòng chọn ảnh thumbnail hợp lệ." });
    }

    try
    {
        var updated = await _courseService.UploadThumbnailAsync(id, request.File, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }
    catch (InvalidOperationException exception)
    {
        return BadRequest(new { message = exception.Message });
    }
}
```

```csharp
// backend/CourseVideo.API/Program.cs
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "storage")),
    RequestPath = "/storage"
});
```

- [ ] **Step 4: Run backend controller/service tests and verify they pass**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~CourseServiceTests|FullyQualifiedName~CoursesControllerTests"`

Expected: PASS, including new validation/error tests.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/DTOs/Courses/UploadCourseThumbnailRequest.cs \
  backend/CourseVideo.API/Services/Interfaces/ICourseService.cs \
  backend/CourseVideo.API/Services/CourseService.cs \
  backend/CourseVideo.API/Controllers/CoursesController.cs \
  backend/CourseVideo.API/Program.cs \
  backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs \
  backend/CourseVideo.API.Tests/Controllers/CoursesControllerTests.cs
git commit -m "feat: add course presentation admin endpoints"
```

## Task 3: Add Admin Thumbnail + Category Controls

**Files:**
- Create: `frontend/src/constants/coursePresentation.js`
- Modify: `frontend/src/api/courseStructureService.js`
- Modify: `frontend/src/pages/CourseStructurePage.jsx`
- Test: `frontend/src/pages/CourseStructurePage.test.jsx`

- [ ] **Step 1: Write the failing admin page tests**

```jsx
const mockUploadCourseThumbnail = vi.fn();
const mockUpdateCourseCategory = vi.fn();

vi.mock("../api/courseStructureService", () => ({
  getCourseStructure: (...args) => mockGetCourseStructure(...args),
  updateModule: (...args) => mockUpdateModule(...args),
  updateLesson: (...args) => mockUpdateLesson(...args),
  uploadCourseThumbnail: (...args) => mockUploadCourseThumbnail(...args),
  updateCourseCategory: (...args) => mockUpdateCourseCategory(...args)
}));

it("renders course thumbnail preview and selected category", async () => {
  mockGetCourseStructure.mockResolvedValue({
    ...baseCourse,
    category: "AiAndData",
    thumbnailUrl: "/storage/course-thumbnails/ai.png"
  });

  renderPage();

  expect(await screen.findByAltText("Thumbnail khóa học OOP")).toHaveAttribute("src", "/storage/course-thumbnails/ai.png");
  expect(screen.getByLabelText("Danh mục khóa học")).toHaveValue("AiAndData");
});

it("uploads a new thumbnail", async () => {
  mockGetCourseStructure.mockResolvedValue({ ...baseCourse, category: "UiUxDesign", thumbnailUrl: "" });
  mockUploadCourseThumbnail.mockResolvedValue({
    ...baseCourse,
    category: "UiUxDesign",
    thumbnailUrl: "/storage/course-thumbnails/new-thumb.png"
  });

  renderPage();

  const file = new File(["thumb"], "thumb.png", { type: "image/png" });
  fireEvent.change(await screen.findByLabelText("Ảnh thumbnail"), { target: { files: [file] } });
  fireEvent.click(screen.getByRole("button", { name: "Upload thumbnail" }));

  await waitFor(() => expect(mockUploadCourseThumbnail).toHaveBeenCalledWith("course-1", file));
});
```

- [ ] **Step 2: Run the targeted frontend test and verify failure**

Run: `npx vitest run src/pages/CourseStructurePage.test.jsx`

Expected: FAIL because the page has no category select, no thumbnail upload action, and the mocked API functions do not exist.

- [ ] **Step 3: Implement the minimal admin presentation block**

```js
// frontend/src/constants/coursePresentation.js
export const COURSE_CATEGORY_OPTIONS = [
  { value: "UiUxDesign", label: "UI/UX Design" },
  { value: "AiAndData", label: "AI & Data" },
  { value: "Development", label: "Development" }
];
```

```js
// frontend/src/api/courseStructureService.js
export async function updateCourseCategory(courseId, category) {
  const { data } = await axiosClient.put(`/courses/${courseId}/category`, { category });
  return data;
}

export async function uploadCourseThumbnail(courseId, file) {
  const formData = new FormData();
  formData.append("file", file);
  const { data } = await axiosClient.post(`/courses/${courseId}/thumbnail`, formData);
  return data;
}
```

```jsx
// frontend/src/pages/CourseStructurePage.jsx
const [selectedCategory, setSelectedCategory] = useState("UiUxDesign");
const [thumbnailFile, setThumbnailFile] = useState(null);
const [isSavingPresentation, setIsSavingPresentation] = useState(false);

useEffect(() => {
  if (course?.category) {
    setSelectedCategory(course.category);
  }
}, [course]);

async function handleSaveCategory() {
  setIsSavingPresentation(true);
  try {
    const updatedCourse = await updateCourseCategory(courseId, selectedCategory);
    setCourse(updatedCourse);
    setMessage("Đã cập nhật category khóa học.");
  } finally {
    setIsSavingPresentation(false);
  }
}
```

```jsx
<Card variant="shadowed">
  <h2>Thumbnail & category</h2>
  {course?.thumbnailUrl ? (
    <img src={course.thumbnailUrl} alt={`Thumbnail khóa học ${course.title}`} />
  ) : (
    <div>Chưa có thumbnail.</div>
  )}
  <FormField label="Danh mục khóa học">
    <select value={selectedCategory} onChange={(event) => setSelectedCategory(event.target.value)}>
      {COURSE_CATEGORY_OPTIONS.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  </FormField>
  <FormField label="Ảnh thumbnail">
    <input type="file" accept="image/png,image/jpeg,image/webp" onChange={(event) => setThumbnailFile(event.target.files?.[0] ?? null)} />
  </FormField>
</Card>
```

- [ ] **Step 4: Run the admin page test and verify it passes**

Run: `npx vitest run src/pages/CourseStructurePage.test.jsx`

Expected: PASS with existing generation/edit tests still green.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/constants/coursePresentation.js \
  frontend/src/api/courseStructureService.js \
  frontend/src/pages/CourseStructurePage.jsx \
  frontend/src/pages/CourseStructurePage.test.jsx
git commit -m "feat: add course presentation admin controls"
```

## Task 4: Redesign `/courses` With Real Search + Category Filters

**Files:**
- Modify: `frontend/src/pages/CoursesPage.jsx`
- Create: `frontend/src/styles/CoursesPage.module.css`
- Test: `frontend/src/pages/CoursesPage.test.jsx`
- Reference: `frontend/src/constants/coursePresentation.js`

- [ ] **Step 1: Write the failing discovery page tests**

```jsx
it("filters courses by search term and category", async () => {
  mockUseAuth.mockReturnValue({ user: { role: "User" } });
  mockGetPublishedCourses.mockResolvedValue([
    {
      id: "course-1",
      title: "Advanced UI Systems",
      description: "Design systems for product teams",
      category: "UiUxDesign",
      thumbnailUrl: "/storage/course-thumbnails/ui.png",
      isPublished: true,
      moduleCount: 12,
      lessonCount: 24
    },
    {
      id: "course-2",
      title: "Prompt Engineering",
      description: "AI workflows",
      category: "AiAndData",
      thumbnailUrl: "/storage/course-thumbnails/ai.png",
      isPublished: true,
      moduleCount: 8,
      lessonCount: 10
    }
  ]);

  renderCoursesPage();

  fireEvent.click(await screen.findByRole("button", { name: "AI & Data" }));
  fireEvent.change(screen.getByLabelText("Tìm khóa học"), { target: { value: "prompt" } });

  expect(await screen.findByText("Prompt Engineering")).toBeInTheDocument();
  expect(screen.queryByText("Advanced UI Systems")).not.toBeInTheDocument();
});

it("renders the first filtered course as the featured card", async () => {
  mockUseAuth.mockReturnValue({ user: { role: "User" } });
  mockGetPublishedCourses.mockResolvedValue([
    { id: "course-1", title: "Advanced UI Systems", description: "Desc", category: "UiUxDesign", thumbnailUrl: "/storage/course-thumbnails/ui.png", isPublished: true, moduleCount: 12, lessonCount: 24 },
    { id: "course-2", title: "Prompt Engineering", description: "Desc", category: "AiAndData", thumbnailUrl: "/storage/course-thumbnails/ai.png", isPublished: true, moduleCount: 8, lessonCount: 10 }
  ]);

  renderCoursesPage();

  expect(await screen.findByRole("heading", { name: "Advanced UI Systems" })).toBeInTheDocument();
  expect(screen.getAllByText("Prompt Engineering")).toHaveLength(1);
});

function renderCoursesPage() {
  render(
    <MemoryRouter>
      <CoursesPage />
    </MemoryRouter>
  );
}
```

- [ ] **Step 2: Run the courses page test and verify failure**

Run: `npx vitest run src/pages/CoursesPage.test.jsx`

Expected: FAIL because the page still renders the old generic grid and has no search/filter/featured layout.

- [ ] **Step 3: Implement the redesigned discovery page**

```jsx
// frontend/src/pages/CoursesPage.jsx
const [searchTerm, setSearchTerm] = useState("");
const [activeCategory, setActiveCategory] = useState("All");

const filteredCourses = courses.filter((course) => {
  const matchesCategory = activeCategory === "All" || course.category === activeCategory;
  const haystack = `${course.title} ${course.description}`.toLowerCase();
  const matchesSearch = haystack.includes(searchTerm.trim().toLowerCase());
  return matchesCategory && matchesSearch;
});

const [featuredCourse, ...gridCourses] = filteredCourses;
```

```jsx
<section className={styles.discoveryHero}>
  <p className={styles.eyebrow}>Course discovery</p>
  <h1 className={styles.title}>
    Master the Future of <span>Creative Tech</span>
  </h1>
  <p className={styles.description}>
    Khám phá lộ trình học thật từ UI/UX, AI đến development trong một bề mặt tìm kiếm gọn hơn.
  </p>
  <label className={styles.searchBar}>
    <span className="sr-only">Tìm khóa học</span>
    <input
      aria-label="Tìm khóa học"
      value={searchTerm}
      onChange={(event) => setSearchTerm(event.target.value)}
      placeholder="Search for courses, tools, or instructors..."
    />
  </label>
  <div className={styles.chipRow}>
    <button type="button" onClick={() => setActiveCategory("All")}>All Courses</button>
    {COURSE_CATEGORY_OPTIONS.map((option) => (
      <button key={option.value} type="button" onClick={() => setActiveCategory(option.value)}>
        {option.label}
      </button>
    ))}
  </div>
</section>
```

```css
/* frontend/src/styles/CoursesPage.module.css */
.page {
  display: grid;
  gap: 40px;
  padding: clamp(32px, 5vw, 56px) 0 64px;
}

.searchBar {
  max-width: 860px;
  margin: 0 auto;
  border: 2px solid var(--color-midnight-ink);
  border-radius: 24px;
  background: var(--color-canvas-white);
  box-shadow: 6px 6px 0 var(--color-midnight-ink);
}

.featuredCard {
  display: grid;
  grid-template-columns: minmax(280px, 420px) 1fr;
  gap: 28px;
}
```

- [ ] **Step 4: Run the courses page test and build verification**

Run: `npx vitest run src/pages/CoursesPage.test.jsx`

Expected: PASS for search, category filter, featured course, empty state, and admin publish button behavior.

Run: `npm run build`

Expected: PASS with no Vite/CSS module errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/CoursesPage.jsx \
  frontend/src/styles/CoursesPage.module.css \
  frontend/src/pages/CoursesPage.test.jsx \
  frontend/src/constants/coursePresentation.js
git commit -m "feat: redesign courses discovery page"
```

## Task 5: End-to-End Verification And Integration

**Files:**
- Verify only: no new source files required unless a regression appears

- [ ] **Step 1: Run the focused backend test suite**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "FullyQualifiedName~CourseServiceTests|FullyQualifiedName~CoursesControllerTests"`

Expected: PASS

- [ ] **Step 2: Run the focused frontend test suite**

Run: `npx vitest run src/pages/CoursesPage.test.jsx src/pages/CourseStructurePage.test.jsx`

Expected: PASS

- [ ] **Step 3: Run frontend production build**

Run: `npm run build`

Expected: PASS with generated assets in `frontend/dist`

- [ ] **Step 4: Rebuild the app containers for manual QA**

Run: `docker compose up -d --build frontend backend`

Expected: both `course_frontend` and `course_backend` restart cleanly with no build errors

- [ ] **Step 5: Manual QA checklist**

```text
1. Open /courses as a learner:
   - hero heading, search bar, chips, featured card, and grid match the new layout
   - search narrows results by title/description
   - category chips filter correctly
   - first filtered course is featured

2. Open /admin/courses/:courseId as an admin:
   - current category is selected
   - current thumbnail previews if present
   - category update shows success message
   - thumbnail upload updates preview after success

3. Refresh /courses after admin changes:
   - updated thumbnail renders from /storage/course-thumbnails/...
   - updated category affects chip filtering immediately after reload
```

- [ ] **Step 6: Commit final integration fixes if needed**

```bash
git add backend/CourseVideo.API/Controllers/CoursesController.cs \
  backend/CourseVideo.API/Services/CourseService.cs \
  backend/CourseVideo.API/Program.cs \
  frontend/src/pages/CoursesPage.jsx \
  frontend/src/pages/CourseStructurePage.jsx \
  frontend/src/pages/CoursesPage.test.jsx \
  frontend/src/pages/CourseStructurePage.test.jsx
git commit -m "test: verify courses presentation redesign"
```

## Self-Review

- Spec coverage:
  - `/courses` redesign: Task 4
  - real search/filter behavior: Task 4
  - featured course + grid from real data: Task 4
  - admin category + thumbnail management: Task 3
  - backend DTO/API changes: Tasks 1-2
  - thumbnail storage/public serving: Task 2
  - backend/frontend tests: Tasks 1-5
- Placeholder scan:
  - no `TODO`, `TBD`, or “similar to above” placeholders remain
  - every task includes exact file paths, concrete code targets, and run commands
- Type consistency:
  - backend enum/value name is `CourseCategory`
  - API string payload/response field is `category`
  - image URL field is consistently `thumbnailUrl`
  - admin endpoints are `/api/courses/{id}/category` and `/api/courses/{id}/thumbnail`
