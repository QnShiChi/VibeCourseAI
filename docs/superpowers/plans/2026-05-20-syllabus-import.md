# Syllabus Import Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an admin-only syllabus import flow that uploads `pdf/docx/txt`, extracts text in `ASP.NET Core Web API`, stores the file and metadata, and exposes admin UI inside the dashboard.

**Architecture:** Extend the existing C# backend using the current `Controller -> Service -> Repository -> DbContext` pattern with a new `Syllabus` entity, file storage helper behavior inside the service layer, and text extraction strategies for `txt`, `docx`, and `pdf`. Then add a frontend admin route and dashboard-facing UI to upload, list, inspect, and delete syllabuses using the shared design system already introduced in the frontend.

**Tech Stack:** ASP.NET Core Web API, Entity Framework Core, SQL Server, React 18, React Router 6, Axios, plain CSS shared UI system, Docker Compose

---

## File Structure

### Files to create
- `backend/CourseVideo.API/Models/Syllabus.cs` — entity for imported syllabuses and extracted text
- `backend/CourseVideo.API/DTOs/Syllabuses/ImportSyllabusRequest.cs` — upload request DTO metadata fields
- `backend/CourseVideo.API/DTOs/Syllabuses/ImportSyllabusResponse.cs` — response after successful upload
- `backend/CourseVideo.API/DTOs/Syllabuses/SyllabusListItemResponse.cs` — admin list item response
- `backend/CourseVideo.API/DTOs/Syllabuses/SyllabusDetailResponse.cs` — admin detail response
- `backend/CourseVideo.API/Services/Interfaces/ISyllabusService.cs` — service contract for syllabus operations
- `backend/CourseVideo.API/Services/SyllabusService.cs` — upload, extraction, storage, delete logic
- `backend/CourseVideo.API/Repositories/Interfaces/ISyllabusRepository.cs` — repository contract
- `backend/CourseVideo.API/Repositories/SyllabusRepository.cs` — EF-backed repository implementation
- `backend/CourseVideo.API/Controllers/SyllabusesController.cs` — admin-only API endpoints
- `backend/CourseVideo.API.Tests/Services/SyllabusServiceTests.cs` — unit tests for upload/extraction validation
- `backend/CourseVideo.API.Tests/Controllers/SyllabusesControllerTests.cs` — controller tests for admin flow
- `frontend/src/pages/SyllabusesPage.jsx` — admin UI for upload, list, and detail preview
- `frontend/src/api/syllabusService.js` — axios wrappers for syllabus endpoints
- `frontend/src/pages/SyllabusesPage.test.jsx` — UI regression test for the admin syllabus screen

### Files to modify
- `backend/CourseVideo.API/CourseVideo.API.csproj` — add document extraction packages
- `backend/CourseVideo.API/Data/AppDbContext.cs` — register `DbSet<Syllabus>` and entity mapping
- `backend/CourseVideo.API/Program.cs` — register syllabus repository/service and configure request size if needed
- `backend/CourseVideo.API/Models/User.cs` — add navigation collection to `Syllabuses`
- `backend/CourseVideo.API/appsettings.json` — add syllabus upload settings if needed
- `backend/CourseVideo.API/appsettings.Development.json` — mirror upload settings if needed
- `frontend/src/routes/AppRoutes.jsx` — register admin syllabus route
- `frontend/src/components/layout/MainLayout.jsx` — add nav entry to the admin syllabus screen
- `frontend/src/pages/DashboardPage.jsx` — add quick action link to syllabus import screen
- `frontend/src/styles/theme.css` — add any small supporting classes for upload/list/detail layout if current utilities are insufficient

### Files to verify but not change unless required
- `backend/CourseVideo.API/Controllers/AuthController.cs`
- `backend/CourseVideo.API/Repositories/UserRepository.cs`
- `frontend/src/auth/RequireAuth.jsx`
- `frontend/src/api/axiosClient.js`

## Task 1: Add the syllabus domain model and persistence layer

**Files:**
- Create: `backend/CourseVideo.API/Models/Syllabus.cs`
- Create: `backend/CourseVideo.API/Repositories/Interfaces/ISyllabusRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/SyllabusRepository.cs`
- Modify: `backend/CourseVideo.API/Models/User.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/SyllabusesControllerTests.cs`

- [ ] **Step 1: Write a failing controller test that expects an empty syllabus list response for an admin**

```csharp
[Fact]
public async Task GetAll_ShouldReturnOkWithEmptyList_WhenNoSyllabusesExist()
{
    var service = new Mock<ISyllabusService>();
    service.Setup(x => x.GetAllAsync()).ReturnsAsync(Array.Empty<SyllabusListItemResponse>());

    var controller = new SyllabusesController(service.Object);

    var result = await controller.GetAll();

    var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().BeEquivalentTo(Array.Empty<SyllabusListItemResponse>());
}
```

- [ ] **Step 2: Run the focused controller test to confirm it fails before the controller exists**

Run: `dotnet test backend/CourseVideo.API.Tests --filter SyllabusesControllerTests.GetAll_ShouldReturnOkWithEmptyList_WhenNoSyllabusesExist`
Expected: FAIL because `SyllabusesController` and related types do not exist yet.

- [ ] **Step 3: Add the `Syllabus` entity and wire it into the EF model**

```csharp
// backend/CourseVideo.API/Models/Syllabus.cs
namespace CourseVideo.API.Models;

public class Syllabus : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
}
```

```csharp
// backend/CourseVideo.API/Models/User.cs
public ICollection<Syllabus> Syllabuses { get; set; } = new List<Syllabus>();
```

```csharp
// backend/CourseVideo.API/Data/AppDbContext.cs
public DbSet<Syllabus> Syllabuses => Set<Syllabus>();

modelBuilder.Entity<Syllabus>(entity =>
{
    entity.HasKey(syllabus => syllabus.Id);
    entity.Property(syllabus => syllabus.Title).HasMaxLength(255).IsRequired();
    entity.Property(syllabus => syllabus.Description).HasMaxLength(2000).IsRequired();
    entity.Property(syllabus => syllabus.OriginalFileName).HasMaxLength(255).IsRequired();
    entity.Property(syllabus => syllabus.StoredFileName).HasMaxLength(255).IsRequired();
    entity.Property(syllabus => syllabus.FilePath).HasMaxLength(500).IsRequired();
    entity.Property(syllabus => syllabus.FileType).HasMaxLength(50).IsRequired();
    entity.Property(syllabus => syllabus.ExtractedText).IsRequired();
    entity.HasIndex(syllabus => syllabus.CreatedAt);
    entity.HasOne(syllabus => syllabus.UploadedByUser)
        .WithMany(user => user.Syllabuses)
        .HasForeignKey(syllabus => syllabus.UploadedByUserId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

- [ ] **Step 4: Add the repository contract and implementation**

```csharp
// backend/CourseVideo.API/Repositories/Interfaces/ISyllabusRepository.cs
using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface ISyllabusRepository
{
    Task AddAsync(Syllabus syllabus);
    Task<IReadOnlyList<Syllabus>> GetAllAsync();
    Task<Syllabus?> GetByIdAsync(Guid id);
    Task DeleteAsync(Syllabus syllabus);
    Task SaveChangesAsync();
}
```

```csharp
// backend/CourseVideo.API/Repositories/SyllabusRepository.cs
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class SyllabusRepository : ISyllabusRepository
{
    private readonly AppDbContext _dbContext;

    public SyllabusRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Syllabus syllabus)
    {
        return _dbContext.Syllabuses.AddAsync(syllabus).AsTask();
    }

    public async Task<IReadOnlyList<Syllabus>> GetAllAsync()
    {
        return await _dbContext.Syllabuses
            .Include(s => s.UploadedByUser)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public Task<Syllabus?> GetByIdAsync(Guid id)
    {
        return _dbContext.Syllabuses
            .Include(s => s.UploadedByUser)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public Task DeleteAsync(Syllabus syllabus)
    {
        _dbContext.Syllabuses.Remove(syllabus);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Run the backend test project to confirm the new model/repository wiring compiles**

Run: `dotnet test backend/CourseVideo.API.Tests`
Expected: FAIL only because syllabus controller/service types still do not exist, but the entity/repository layer compiles cleanly.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Models/Syllabus.cs backend/CourseVideo.API/Models/User.cs backend/CourseVideo.API/Data/AppDbContext.cs backend/CourseVideo.API/Repositories/Interfaces/ISyllabusRepository.cs backend/CourseVideo.API/Repositories/SyllabusRepository.cs
git commit -m "feat: add syllabus persistence model"
```

## Task 2: Add DTOs, service interface, and document extraction service

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Syllabuses/ImportSyllabusRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Syllabuses/ImportSyllabusResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Syllabuses/SyllabusListItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Syllabuses/SyllabusDetailResponse.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ISyllabusService.cs`
- Create: `backend/CourseVideo.API/Services/SyllabusService.cs`
- Modify: `backend/CourseVideo.API/CourseVideo.API.csproj`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/SyllabusServiceTests.cs`

- [ ] **Step 1: Write a failing service test that uploads a `.txt` file and expects extracted text to be returned**

```csharp
[Fact]
public async Task ImportAsync_ShouldStoreTxtFileAndExtractText()
{
    var repository = new Mock<ISyllabusRepository>();
    repository.Setup(x => x.AddAsync(It.IsAny<Syllabus>())).Returns(Task.CompletedTask);
    repository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

    var env = new Mock<IWebHostEnvironment>();
    env.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

    var service = new SyllabusService(repository.Object, env.Object);
    var bytes = Encoding.UTF8.GetBytes("De cuong mon hoc");
    using var stream = new MemoryStream(bytes);
    IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "syllabus.txt");

    var response = await service.ImportAsync(
        new ImportSyllabusRequest { Title = "Web", Description = "Mo ta", File = file },
        Guid.NewGuid(),
        "Admin User");

    response.ExtractedText.Should().Contain("De cuong mon hoc");
}
```

- [ ] **Step 2: Run the focused service test to confirm it fails before the service exists**

Run: `dotnet test backend/CourseVideo.API.Tests --filter SyllabusServiceTests.ImportAsync_ShouldStoreTxtFileAndExtractText`
Expected: FAIL because syllabus DTOs/service do not exist yet.

- [ ] **Step 3: Add document extraction packages and register the service**

```xml
<!-- backend/CourseVideo.API/CourseVideo.API.csproj -->
<ItemGroup>
  <PackageReference Include="DocumentFormat.OpenXml" Version="3.1.0" />
  <PackageReference Include="UglyToad.PdfPig" Version="0.1.9" />
</ItemGroup>
```

```csharp
// backend/CourseVideo.API/Program.cs
builder.Services.AddScoped<ISyllabusRepository, SyllabusRepository>();
builder.Services.AddScoped<ISyllabusService, SyllabusService>();
```

- [ ] **Step 4: Add DTOs and the service contract**

```csharp
// backend/CourseVideo.API/DTOs/Syllabuses/ImportSyllabusRequest.cs
using Microsoft.AspNetCore.Http;

namespace CourseVideo.API.DTOs.Syllabuses;

public class ImportSyllabusRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}
```

```csharp
// backend/CourseVideo.API/Services/Interfaces/ISyllabusService.cs
using CourseVideo.API.DTOs.Syllabuses;

namespace CourseVideo.API.Services.Interfaces;

public interface ISyllabusService
{
    Task<ImportSyllabusResponse> ImportAsync(ImportSyllabusRequest request, Guid uploadedByUserId, string uploadedByName);
    Task<IReadOnlyList<SyllabusListItemResponse>> GetAllAsync();
    Task<SyllabusDetailResponse?> GetByIdAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
```

- [ ] **Step 5: Implement `SyllabusService` with `txt`, `docx`, and `pdf` extraction inside C#**

```csharp
private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
{
    ".txt", ".docx", ".pdf"
};

private async Task<string> ExtractTextAsync(string filePath, string extension)
{
    return extension.ToLowerInvariant() switch
    {
        ".txt" => await File.ReadAllTextAsync(filePath),
        ".docx" => ExtractDocxText(filePath),
        ".pdf" => ExtractPdfText(filePath),
        _ => throw new InvalidOperationException("Định dạng file không được hỗ trợ.")
    };
}
```

```csharp
private static string ExtractDocxText(string filePath)
{
    using var document = WordprocessingDocument.Open(filePath, false);
    return document.MainDocumentPart?.Document.Body?.InnerText?.Trim()
        ?? throw new InvalidOperationException("Không thể trích xuất nội dung từ file DOCX.");
}
```

```csharp
private static string ExtractPdfText(string filePath)
{
    using var document = PdfDocument.Open(filePath);
    var text = string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
    return string.IsNullOrWhiteSpace(text)
        ? throw new InvalidOperationException("Không thể trích xuất nội dung từ file PDF.")
        : text.Trim();
}
```

- [ ] **Step 6: Run the syllabus service tests**

Run: `dotnet test backend/CourseVideo.API.Tests --filter SyllabusServiceTests`
Expected: PASS for the `.txt` happy path test, with failures remaining only for controller tests not implemented yet.

- [ ] **Step 7: Commit**

```bash
git add backend/CourseVideo.API/CourseVideo.API.csproj backend/CourseVideo.API/Program.cs backend/CourseVideo.API/DTOs/Syllabuses backend/CourseVideo.API/Services/Interfaces/ISyllabusService.cs backend/CourseVideo.API/Services/SyllabusService.cs backend/CourseVideo.API.Tests/Services/SyllabusServiceTests.cs
git commit -m "feat: add syllabus import service and extraction"
```

## Task 3: Add admin-only syllabus API endpoints

**Files:**
- Create: `backend/CourseVideo.API/Controllers/SyllabusesController.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/SyllabusesControllerTests.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/SyllabusesControllerTests.cs`

- [ ] **Step 1: Expand the failing controller test suite for import and detail lookup**

```csharp
[Fact]
public async Task Import_ShouldReturnBadRequest_WhenFileMissing()
{
    var service = new Mock<ISyllabusService>();
    var controller = new SyllabusesController(service.Object)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = TestHttpContextFactory.CreateAdminContext()
        }
    };

    var result = await controller.Import(new ImportSyllabusRequest
    {
        Title = "Web",
        Description = "Mo ta",
        File = null
    });

    result.Should().BeOfType<BadRequestObjectResult>();
}
```

- [ ] **Step 2: Run the focused controller tests to confirm the controller is still missing**

Run: `dotnet test backend/CourseVideo.API.Tests --filter SyllabusesControllerTests`
Expected: FAIL before the controller is implemented.

- [ ] **Step 3: Implement the admin-only controller endpoints**

```csharp
[ApiController]
[Route("api/syllabuses")]
[Authorize(Roles = "Admin")]
public class SyllabusesController : ControllerBase
{
    private readonly ISyllabusService _syllabusService;

    public SyllabusesController(ISyllabusService syllabusService)
    {
        _syllabusService = syllabusService;
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromForm] ImportSyllabusRequest request)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "Vui lòng chọn file đề cương." });
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
        var userName = User.Identity?.Name ?? User.FindFirst("email")?.Value ?? "Admin";
        var result = await _syllabusService.ImportAsync(request, userId, userName);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SyllabusListItemResponse>>> GetAll()
    {
        return Ok(await _syllabusService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var syllabus = await _syllabusService.GetByIdAsync(id);
        return syllabus is null ? NotFound() : Ok(syllabus);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _syllabusService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 4: Run the controller tests again**

Run: `dotnet test backend/CourseVideo.API.Tests --filter SyllabusesControllerTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Controllers/SyllabusesController.cs backend/CourseVideo.API.Tests/Controllers/SyllabusesControllerTests.cs
git commit -m "feat: add admin syllabus api endpoints"
```

## Task 4: Add admin syllabus UI in the frontend

**Files:**
- Create: `frontend/src/api/syllabusService.js`
- Create: `frontend/src/pages/SyllabusesPage.jsx`
- Create: `frontend/src/pages/SyllabusesPage.test.jsx`
- Modify: `frontend/src/routes/AppRoutes.jsx`
- Modify: `frontend/src/components/layout/MainLayout.jsx`
- Modify: `frontend/src/pages/DashboardPage.jsx`
- Modify: `frontend/src/styles/theme.css`
- Test: `frontend/src/pages/SyllabusesPage.test.jsx`

- [ ] **Step 1: Write a failing frontend test for the admin syllabus screen**

```jsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import SyllabusesPage from "./SyllabusesPage";

vi.mock("../auth/useAuth", () => ({
  useAuth: () => ({ user: { role: "Admin" } })
}));

describe("SyllabusesPage", () => {
  it("renders the upload form and admin syllabus heading", () => {
    render(
      <MemoryRouter>
        <SyllabusesPage />
      </MemoryRouter>
    );

    expect(screen.getByRole("heading", { name: "Đề cương" })).toBeInTheDocument();
    expect(screen.getByLabelText("Tiêu đề")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Import đề cương" })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the focused frontend test to confirm the screen does not exist yet**

Run: `npm run test -- --run src/pages/SyllabusesPage.test.jsx`
Expected: FAIL because the syllabus page has not been created yet.

- [ ] **Step 3: Add frontend API wrappers and the admin syllabus page**

```jsx
// frontend/src/api/syllabusService.js
import { axiosClient } from "./axiosClient";

export async function importSyllabus(formData) {
  const { data } = await axiosClient.post("/syllabuses/import", formData, {
    headers: { "Content-Type": "multipart/form-data" }
  });
  return data;
}

export async function getSyllabuses() {
  const { data } = await axiosClient.get("/syllabuses");
  return data;
}

export async function getSyllabusDetail(id) {
  const { data } = await axiosClient.get(`/syllabuses/${id}`);
  return data;
}

export async function deleteSyllabus(id) {
  await axiosClient.delete(`/syllabuses/${id}`);
}
```

```jsx
// frontend/src/pages/SyllabusesPage.jsx
import { useEffect, useState } from "react";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";
import { deleteSyllabus, getSyllabusDetail, getSyllabuses, importSyllabus } from "../api/syllabusService";

export default function SyllabusesPage() {
  const [formData, setFormData] = useState({ title: "", description: "", file: null });
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(null);
  const [message, setMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    loadItems();
  }, []);

  async function loadItems() {
    const syllabuses = await getSyllabuses();
    setItems(syllabuses);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    const payload = new FormData();
    payload.append("title", formData.title);
    payload.append("description", formData.description);
    payload.append("file", formData.file);
    await importSyllabus(payload);
    await loadItems();
    setMessage("Import đề cương thành công.");
  }

  return (
    <Section className="section-stack">
      <PageHeader eyebrow="Admin" title="Đề cương" description="Import và quản lý đề cương để chuẩn bị cho bước sinh khóa học." />
      <Card variant="shadowed">
        <form className="auth-form" onSubmit={handleSubmit}>
          <FormField id="syllabus-title" label="Tiêu đề">
            <input className="ui-input" id="syllabus-title" />
          </FormField>
          <Button type="submit">Import đề cương</Button>
        </form>
      </Card>
    </Section>
  );
}
```

- [ ] **Step 4: Add the route and admin navigation entry**

```jsx
// frontend/src/routes/AppRoutes.jsx
<Route
  path="/admin/syllabuses"
  element={
    <RequireAuth>
      <SyllabusesPage />
    </RequireAuth>
  }
/>
```

```jsx
// frontend/src/components/layout/MainLayout.jsx
{isAdmin ? (
  <NavLink className={getNavLinkClassName} to="/admin/syllabuses">
    Đề cương
  </NavLink>
) : null}
```

- [ ] **Step 5: Run the frontend syllabus page test**

Run: `npm run test -- --run src/pages/SyllabusesPage.test.jsx`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/syllabusService.js frontend/src/pages/SyllabusesPage.jsx frontend/src/pages/SyllabusesPage.test.jsx frontend/src/routes/AppRoutes.jsx frontend/src/components/layout/MainLayout.jsx frontend/src/pages/DashboardPage.jsx frontend/src/styles/theme.css
git commit -m "feat: add admin syllabus import ui"
```

## Task 5: End-to-end verification and checklist update

**Files:**
- Modify: `docs/project-function-checklist.md`
- Test: `backend/CourseVideo.API.Tests`
- Test: `frontend` tests and build

- [ ] **Step 1: Run backend tests covering syllabus functionality**

Run: `dotnet test backend/CourseVideo.API.Tests`
Expected: PASS with new syllabus service/controller tests included.

- [ ] **Step 2: Run frontend tests**

Run: `npm run test -- --run`
Expected: PASS for existing auth/layout tests and the new syllabus page test.

- [ ] **Step 3: Run frontend build**

Run: `npm run build`
Expected: Vite production build succeeds.

- [ ] **Step 4: Manually verify the admin-only flow**

Checklist:
```text
- Login as admin
- Open admin syllabus page from navigation/dashboard
- Upload one .txt file and confirm extracted text is visible
- Upload one .docx file and confirm extracted text is visible
- Upload one .pdf file with text layer and confirm extracted text is visible
- Delete a syllabus and confirm it disappears from list and storage
- Confirm non-admin users cannot access the admin syllabus route/API
```

- [ ] **Step 5: Update the project checklist to reflect completed syllabus import work**

```text
Mark as complete or in-progress:
- Syllabuses table
- Upload API
- Supported file types pdf/docx/txt
- ExtractedText persistence
- Admin syllabus list/detail/delete
- Admin frontend import screen
```

- [ ] **Step 6: Commit**

```bash
git add docs/project-function-checklist.md
git commit -m "test: verify syllabus import module"
```
