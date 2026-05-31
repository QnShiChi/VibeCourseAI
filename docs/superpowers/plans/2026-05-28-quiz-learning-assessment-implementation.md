# Quiz Learning Assessment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build lesson quiz and final course quiz assessment flows with unlimited learner attempts, server-side scoring, AI-generated Vietnamese multiple-choice questions, and learner-facing quiz UI while keeping ASP.NET Core as the core orchestration layer.

**Architecture:** Add a dedicated `Quiz` subsystem inside `backend/CourseVideo.API` for entities, validation, generation orchestration, scoring, and APIs. Reuse the existing OpenRouter integration pattern directly from ASP.NET Core for quiz generation, keep all quiz runtime logic in `.NET`, and expose React UI through the existing learner page without adding new Python quiz services.

**Tech Stack:** ASP.NET Core 8, Entity Framework Core, SQL Server, OpenRouter HTTP integration from `.NET`, xUnit + Moq + FluentAssertions, React 18, Vite, Vitest, Testing Library

---

## File Structure

### Backend files to create

- `backend/CourseVideo.API/Models/Quiz.cs`
- `backend/CourseVideo.API/Models/QuizQuestion.cs`
- `backend/CourseVideo.API/Models/QuizOption.cs`
- `backend/CourseVideo.API/Models/QuizAttempt.cs`
- `backend/CourseVideo.API/Models/QuizAttemptAnswer.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/IQuizRepository.cs`
- `backend/CourseVideo.API/Repositories/QuizRepository.cs`
- `backend/CourseVideo.API/Services/Interfaces/IOpenRouterQuizGenerationService.cs`
- `backend/CourseVideo.API/Services/Interfaces/IQuizGenerationService.cs`
- `backend/CourseVideo.API/Services/Interfaces/IQuizService.cs`
- `backend/CourseVideo.API/Services/OpenRouterQuizGenerationService.cs`
- `backend/CourseVideo.API/Services/QuizGenerationService.cs`
- `backend/CourseVideo.API/Services/QuizService.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/QuizQuestionResponse.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/QuizOptionResponse.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/QuizResponse.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/QuizSummaryResponse.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/CreateQuizAttemptResponse.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/SubmitQuizAttemptRequest.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/SubmitQuizAttemptAnswerRequest.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/SubmitQuizAttemptResponse.cs`
- `backend/CourseVideo.API/DTOs/Quizzes/QuizAttemptHistoryItemResponse.cs`
- `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterQuizGenerationResult.cs`
- `backend/CourseVideo.API/Controllers/QuizzesController.cs`
- `backend/CourseVideo.API/Controllers/AdminQuizzesController.cs`
- `backend/CourseVideo.API.Tests/Services/OpenRouterQuizGenerationServiceTests.cs`
- `backend/CourseVideo.API.Tests/Services/QuizGenerationServiceTests.cs`
- `backend/CourseVideo.API.Tests/Services/QuizServiceTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/QuizzesControllerTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/AdminQuizzesControllerTests.cs`

### Backend files to modify

- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Data/DbInitializer.cs`
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/Services/CourseService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`
- `backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs`
- `backend/CourseVideo.API/DTOs/Courses/CourseLearnResponse.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ICourseRepository.cs`
- `backend/CourseVideo.API/Repositories/CourseRepository.cs`
- `backend/CourseVideo.API/Services/OpenRouterPromptFactory.cs` if shared prompt helpers become worthwhile, otherwise leave untouched
- `backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs`

### Frontend files to create

- `frontend/src/api/quizService.js`
- `frontend/src/components/course/LessonQuizPanel.jsx`
- `frontend/src/components/course/FinalQuizCard.jsx`
- `frontend/src/components/course/QuizAttemptResult.jsx`
- `frontend/src/components/course/LessonQuizPanel.test.jsx`
- `frontend/src/components/course/FinalQuizCard.test.jsx`

### Frontend files to modify

- `frontend/src/pages/CourseLearnPage.jsx`
- `frontend/src/pages/CourseLearnPage.test.jsx`
- `frontend/src/styles/theme.css`

### Explicit non-goals for implementation

- Do not add quiz endpoints or quiz orchestration to `ai-worker/app/main.py`.
- Do not move scoring or validation to frontend.
- Do not add admin manual quiz editing in this implementation.

---

### Task 1: Add Quiz Data Model and Persistence

**Files:**
- Create: `backend/CourseVideo.API/Models/Quiz.cs`
- Create: `backend/CourseVideo.API/Models/QuizQuestion.cs`
- Create: `backend/CourseVideo.API/Models/QuizOption.cs`
- Create: `backend/CourseVideo.API/Models/QuizAttempt.cs`
- Create: `backend/CourseVideo.API/Models/QuizAttemptAnswer.cs`
- Create: `backend/CourseVideo.API/Repositories/Interfaces/IQuizRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/QuizRepository.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`

- [ ] **Step 1: Write the failing repository/model tests in service layer**

Add a new test class file `backend/CourseVideo.API.Tests/Services/QuizServiceTests.cs` with the first two shape tests:

```csharp
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using CourseVideo.API.Repositories.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class QuizServiceTests
{
    [Fact]
    public async Task GetLessonQuizAsync_ReturnsNull_WhenQuizDoesNotExist()
    {
        var repository = new Mock<IQuizRepository>();
        repository.Setup(x => x.GetLessonQuizAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Quiz?)null);

        var service = new QuizService(repository.Object);

        var result = await service.GetLessonQuizAsync(Guid.NewGuid(), Guid.NewGuid(), false);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLessonQuizAsync_ReturnsReadyQuizSummary_WhenQuizExists()
    {
        var lessonId = Guid.NewGuid();
        var repository = new Mock<IQuizRepository>();
        repository.Setup(x => x.GetLessonQuizAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Quiz
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                Type = "Lesson",
                Status = "Ready",
                Title = "Kiem tra nhanh",
                Questions =
                [
                    new QuizQuestion
                    {
                        Id = Guid.NewGuid(),
                        QuestionText = "AI la gi?",
                        OrderIndex = 1,
                        Explanation = "Giai thich",
                        Options =
                        [
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "A", OrderIndex = 1, IsCorrect = true },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "B", OrderIndex = 2, IsCorrect = false },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "C", OrderIndex = 3, IsCorrect = false },
                            new QuizOption { Id = Guid.NewGuid(), OptionText = "D", OrderIndex = 4, IsCorrect = false }
                        ]
                    }
                ]
            });

        var service = new QuizService(repository.Object);

        var result = await service.GetLessonQuizAsync(lessonId, Guid.NewGuid(), false);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Ready");
        result.QuestionCount.Should().Be(1);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter QuizServiceTests
```

Expected: FAIL with missing `Quiz`, `IQuizRepository`, and `QuizService` types.

- [ ] **Step 3: Add quiz entity models and DbContext registrations**

Create the model files with the following minimal implementations:

`backend/CourseVideo.API/Models/Quiz.cs`
```csharp
namespace CourseVideo.API.Models;

public class Quiz : BaseEntity
{
    public Guid? LessonId { get; set; }
    public Guid? CourseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SourceContentVersion { get; set; }
    public int QuestionCount { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public string? GenerationError { get; set; }
    public Lesson? Lesson { get; set; }
    public Course? Course { get; set; }
    public ICollection<QuizQuestion> Questions { get; set; } = [];
    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}
```

`backend/CourseVideo.API/Models/QuizQuestion.cs`
```csharp
namespace CourseVideo.API.Models;

public class QuizQuestion : BaseEntity
{
    public Guid QuizId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public Quiz? Quiz { get; set; }
    public ICollection<QuizOption> Options { get; set; } = [];
}
```

`backend/CourseVideo.API/Models/QuizOption.cs`
```csharp
namespace CourseVideo.API.Models;

public class QuizOption : BaseEntity
{
    public Guid QuizQuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsCorrect { get; set; }
    public QuizQuestion? QuizQuestion { get; set; }
}
```

`backend/CourseVideo.API/Models/QuizAttempt.cs`
```csharp
namespace CourseVideo.API.Models;

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal Score { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public Quiz? Quiz { get; set; }
    public User? User { get; set; }
    public ICollection<QuizAttemptAnswer> Answers { get; set; } = [];
}
```

`backend/CourseVideo.API/Models/QuizAttemptAnswer.cs`
```csharp
namespace CourseVideo.API.Models;

public class QuizAttemptAnswer : BaseEntity
{
    public Guid QuizAttemptId { get; set; }
    public Guid QuizQuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public QuizAttempt? QuizAttempt { get; set; }
    public QuizQuestion? QuizQuestion { get; set; }
}
```

Update `backend/CourseVideo.API/Data/AppDbContext.cs` by adding:

```csharp
public DbSet<Quiz> Quizzes => Set<Quiz>();
public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
```

and the entity configuration block:

```csharp
modelBuilder.Entity<Quiz>(entity =>
{
    entity.HasKey(quiz => quiz.Id);
    entity.Property(quiz => quiz.Type).HasMaxLength(30).IsRequired();
    entity.Property(quiz => quiz.Status).HasMaxLength(30).IsRequired();
    entity.Property(quiz => quiz.Title).HasMaxLength(300).IsRequired();
    entity.Property(quiz => quiz.SourceContentVersion).HasMaxLength(100);
    entity.Property(quiz => quiz.GenerationError).HasMaxLength(2000);
    entity.HasIndex(quiz => quiz.LessonId).IsUnique().HasFilter("[LessonId] IS NOT NULL");
    entity.HasIndex(quiz => quiz.CourseId).IsUnique().HasFilter("[CourseId] IS NOT NULL");
    entity.HasOne(quiz => quiz.Lesson).WithMany().HasForeignKey(quiz => quiz.LessonId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(quiz => quiz.Course).WithMany().HasForeignKey(quiz => quiz.CourseId).OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<QuizQuestion>(entity =>
{
    entity.HasKey(question => question.Id);
    entity.Property(question => question.QuestionText).HasMaxLength(2000).IsRequired();
    entity.Property(question => question.Explanation).HasMaxLength(2000).IsRequired();
    entity.HasIndex(question => new { question.QuizId, question.OrderIndex });
    entity.HasOne(question => question.Quiz).WithMany(quiz => quiz.Questions).HasForeignKey(question => question.QuizId).OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<QuizOption>(entity =>
{
    entity.HasKey(option => option.Id);
    entity.Property(option => option.OptionText).HasMaxLength(1000).IsRequired();
    entity.HasIndex(option => new { option.QuizQuestionId, option.OrderIndex });
    entity.HasOne(option => option.QuizQuestion).WithMany(question => question.Options).HasForeignKey(option => option.QuizQuestionId).OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<QuizAttempt>(entity =>
{
    entity.HasKey(attempt => attempt.Id);
    entity.Property(attempt => attempt.Score).HasPrecision(5, 2);
    entity.HasIndex(attempt => new { attempt.QuizId, attempt.UserId, attempt.StartedAt });
    entity.HasOne(attempt => attempt.Quiz).WithMany(quiz => quiz.Attempts).HasForeignKey(attempt => attempt.QuizId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(attempt => attempt.User).WithMany().HasForeignKey(attempt => attempt.UserId).OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<QuizAttemptAnswer>(entity =>
{
    entity.HasKey(answer => answer.Id);
    entity.HasIndex(answer => new { answer.QuizAttemptId, answer.QuizQuestionId }).IsUnique();
    entity.HasOne(answer => answer.QuizAttempt).WithMany(attempt => attempt.Answers).HasForeignKey(answer => answer.QuizAttemptId).OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(answer => answer.QuizQuestion).WithMany().HasForeignKey(answer => answer.QuizQuestionId).OnDelete(DeleteBehavior.Restrict);
});
```

- [ ] **Step 4: Add SQL bootstrap and repository surface**

Create `backend/CourseVideo.API/Repositories/Interfaces/IQuizRepository.cs`:

```csharp
using CourseVideo.API.Models;

namespace CourseVideo.API.Repositories.Interfaces;

public interface IQuizRepository
{
    Task<Quiz?> GetLessonQuizAsync(Guid lessonId, CancellationToken cancellationToken = default);
    Task<Quiz?> GetCourseFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<Quiz?> GetByIdAsync(Guid quizId, CancellationToken cancellationToken = default);
    Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default);
    Task AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Create `backend/CourseVideo.API/Repositories/QuizRepository.cs`:

```csharp
using CourseVideo.API.Data;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CourseVideo.API.Repositories;

public class QuizRepository : IQuizRepository
{
    private readonly AppDbContext _dbContext;

    public QuizRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Quiz?> GetLessonQuizAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(x => x.LessonId == lessonId, cancellationToken);

    public Task<Quiz?> GetCourseFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(x => x.CourseId == courseId && x.Type == "Final", cancellationToken);

    public Task<Quiz?> GetByIdAsync(Guid quizId, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes
            .Include(x => x.Questions.OrderBy(q => q.OrderIndex))
            .ThenInclude(x => x.Options.OrderBy(o => o.OrderIndex))
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == quizId, cancellationToken);

    public Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default) =>
        _dbContext.Quizzes.AddAsync(quiz, cancellationToken).AsTask();

    public Task AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default) =>
        _dbContext.QuizAttempts.AddAsync(attempt, cancellationToken).AsTask();

    public async Task<IReadOnlyList<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default) =>
        await _dbContext.QuizAttempts
            .Include(x => x.Answers)
            .Where(x => x.QuizId == quizId && x.UserId == userId)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
```

Add a new bootstrap helper `EnsureQuizTablesExist(dbContext)` inside `backend/CourseVideo.API/Data/DbInitializer.cs` and call it from `Initialize(...)` after `EnsureLessonVoiceTutorTablesExist(dbContext);`:

```csharp
private static void EnsureQuizTablesExist(AppDbContext dbContext)
{
    if (!dbContext.Database.IsSqlServer())
    {
        return;
    }

    dbContext.Database.ExecuteSqlRaw(
        """
        IF OBJECT_ID(N'[Quizzes]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Quizzes] (
                [Id] uniqueidentifier NOT NULL,
                [LessonId] uniqueidentifier NULL,
                [CourseId] uniqueidentifier NULL,
                [Type] nvarchar(30) NOT NULL,
                [Status] nvarchar(30) NOT NULL,
                [Title] nvarchar(300) NOT NULL,
                [SourceContentVersion] nvarchar(100) NULL,
                [QuestionCount] int NOT NULL,
                [LastGeneratedAt] datetime2 NULL,
                [GenerationError] nvarchar(2000) NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_Quizzes] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Quizzes_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [Lessons]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_Quizzes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX [IX_Quizzes_LessonId] ON [Quizzes]([LessonId]) WHERE [LessonId] IS NOT NULL;
            CREATE UNIQUE INDEX [IX_Quizzes_CourseId] ON [Quizzes]([CourseId]) WHERE [CourseId] IS NOT NULL;
        END

        IF OBJECT_ID(N'[QuizQuestions]', N'U') IS NULL
        BEGIN
            CREATE TABLE [QuizQuestions] (
                [Id] uniqueidentifier NOT NULL,
                [QuizId] uniqueidentifier NOT NULL,
                [QuestionText] nvarchar(2000) NOT NULL,
                [Explanation] nvarchar(2000) NOT NULL,
                [OrderIndex] int NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_QuizQuestions] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_QuizQuestions_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes]([Id]) ON DELETE CASCADE
            );
        END

        IF OBJECT_ID(N'[QuizOptions]', N'U') IS NULL
        BEGIN
            CREATE TABLE [QuizOptions] (
                [Id] uniqueidentifier NOT NULL,
                [QuizQuestionId] uniqueidentifier NOT NULL,
                [OptionText] nvarchar(1000) NOT NULL,
                [OrderIndex] int NOT NULL,
                [IsCorrect] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_QuizOptions] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_QuizOptions_QuizQuestions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [QuizQuestions]([Id]) ON DELETE CASCADE
            );
        END

        IF OBJECT_ID(N'[QuizAttempts]', N'U') IS NULL
        BEGIN
            CREATE TABLE [QuizAttempts] (
                [Id] uniqueidentifier NOT NULL,
                [QuizId] uniqueidentifier NOT NULL,
                [UserId] uniqueidentifier NOT NULL,
                [StartedAt] datetime2 NOT NULL,
                [SubmittedAt] datetime2 NULL,
                [Score] decimal(5,2) NOT NULL,
                [CorrectCount] int NOT NULL,
                [TotalQuestions] int NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_QuizAttempts] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_QuizAttempts_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_QuizAttempts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
            );
        END

        IF OBJECT_ID(N'[QuizAttemptAnswers]', N'U') IS NULL
        BEGIN
            CREATE TABLE [QuizAttemptAnswers] (
                [Id] uniqueidentifier NOT NULL,
                [QuizAttemptId] uniqueidentifier NOT NULL,
                [QuizQuestionId] uniqueidentifier NOT NULL,
                [SelectedOptionId] uniqueidentifier NOT NULL,
                [IsCorrect] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_QuizAttemptAnswers] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_QuizAttemptAnswers_QuizAttempts_QuizAttemptId] FOREIGN KEY ([QuizAttemptId]) REFERENCES [QuizAttempts]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_QuizAttemptAnswers_QuizQuestions_QuizQuestionId] FOREIGN KEY ([QuizQuestionId]) REFERENCES [QuizQuestions]([Id]),
                CONSTRAINT [UQ_QuizAttemptAnswers_Attempt_Question] UNIQUE ([QuizAttemptId], [QuizQuestionId])
            );
        END
        """);
}
```

- [ ] **Step 5: Run tests to verify Task 1 passes**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter QuizServiceTests
```

Expected: PASS for the initial `QuizServiceTests` after adding a minimal `QuizService` stub in Task 4.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Data/AppDbContext.cs backend/CourseVideo.API/Data/DbInitializer.cs backend/CourseVideo.API/Models backend/CourseVideo.API/Repositories backend/CourseVideo.API.Tests/Services/QuizServiceTests.cs
git commit -m "feat: add quiz persistence model"
```

### Task 2: Add OpenRouter Quiz Generation in ASP.NET Core

**Files:**
- Create: `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterQuizGenerationResult.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/IOpenRouterQuizGenerationService.cs`
- Create: `backend/CourseVideo.API/Services/OpenRouterQuizGenerationService.cs`
- Create: `backend/CourseVideo.API.Tests/Services/OpenRouterQuizGenerationServiceTests.cs`
- Modify: `backend/CourseVideo.API/Program.cs`

- [ ] **Step 1: Write the failing OpenRouter generation tests**

Create `backend/CourseVideo.API.Tests/Services/OpenRouterQuizGenerationServiceTests.cs`:

```csharp
using System.Net;
using System.Net.Http;
using System.Text;
using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using CourseVideo.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class OpenRouterQuizGenerationServiceTests
{
    [Fact]
    public async Task GenerateLessonQuizAsync_ReturnsParsedQuiz_WhenPayloadIsValid()
    {
        var handler = new StubHttpMessageHandler("""
        {
          "choices": [
            {
              "message": {
                "content": "{\"title\":\"Kiem tra nhanh\",\"questions\":[{\"questionText\":\"Khai niem AI tap trung vao dieu gi?\",\"explanation\":\"AI tap trung vao kha nang mo phong tri tue.\",\"options\":[{\"optionText\":\"Mo phong tri tue\",\"isCorrect\":true},{\"optionText\":\"In tai lieu\",\"isCorrect\":false},{\"optionText\":\"Luu anh\",\"isCorrect\":false},{\"optionText\":\"Mo nhac\",\"isCorrect\":false}]}]}"
              }
            }
          ]
        }
        """, HttpStatusCode.OK);

        var client = new HttpClient(handler);
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            BaseUrl = "https://openrouter.ai/api/v1",
            TimeoutSeconds = 30
        });

        var service = new OpenRouterQuizGenerationService(client, options, NullLogger<OpenRouterQuizGenerationService>.Instance);

        var result = await service.GenerateLessonQuizAsync(
            new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" },
            new Module { Id = Guid.NewGuid(), Title = "M1", Description = "Desc" },
            new Lesson { Id = Guid.NewGuid(), Title = "L1", Description = "Desc", ContentSeed = "Noi dung bai hoc ve khai niem AI" });

        result.Title.Should().Be("Kiem tra nhanh");
        result.Questions.Should().HaveCount(1);
        result.Questions[0].Options.Should().HaveCount(4);
        result.Questions[0].Options.Should().ContainSingle(x => x.IsCorrect);
    }

    [Fact]
    public async Task GenerateLessonQuizAsync_Throws_WhenPayloadIsNotVietnameseEnough()
    {
        var handler = new StubHttpMessageHandler("""
        {
          "choices": [
            {
              "message": {
                "content": "{\"title\":\"Quick quiz\",\"questions\":[{\"questionText\":\"What is AI?\",\"explanation\":\"Because yes.\",\"options\":[{\"optionText\":\"A\",\"isCorrect\":true},{\"optionText\":\"B\",\"isCorrect\":false},{\"optionText\":\"C\",\"isCorrect\":false},{\"optionText\":\"D\",\"isCorrect\":false}]}]}"
              }
            }
          ]
        }
        """, HttpStatusCode.OK);

        var client = new HttpClient(handler);
        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            Model = "test-model",
            BaseUrl = "https://openrouter.ai/api/v1",
            TimeoutSeconds = 30
        });

        var service = new OpenRouterQuizGenerationService(client, options, NullLogger<OpenRouterQuizGenerationService>.Instance);

        var action = async () => await service.GenerateLessonQuizAsync(
            new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" },
            new Module { Id = Guid.NewGuid(), Title = "M1", Description = "Desc" },
            new Lesson { Id = Guid.NewGuid(), Title = "L1", Description = "Desc", ContentSeed = "Noi dung bai hoc ve khai niem AI" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tieng Viet co dau*");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter OpenRouterQuizGenerationServiceTests
```

Expected: FAIL with missing `OpenRouterQuizGenerationService` and DTOs.

- [ ] **Step 3: Implement the OpenRouter quiz contract and validation**

Create `backend/CourseVideo.API/DTOs/OpenRouter/OpenRouterQuizGenerationResult.cs`:

```csharp
namespace CourseVideo.API.DTOs.OpenRouter;

public class OpenRouterQuizGenerationResult
{
    public string Title { get; set; } = string.Empty;
    public List<OpenRouterQuizQuestionResult> Questions { get; set; } = [];
}

public class OpenRouterQuizQuestionResult
{
    public string QuestionText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<OpenRouterQuizOptionResult> Options { get; set; } = [];
}

public class OpenRouterQuizOptionResult
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
```

Create `backend/CourseVideo.API/Services/Interfaces/IOpenRouterQuizGenerationService.cs`:

```csharp
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;

namespace CourseVideo.API.Services.Interfaces;

public interface IOpenRouterQuizGenerationService
{
    Task<OpenRouterQuizGenerationResult> GenerateLessonQuizAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken = default);
    Task<OpenRouterQuizGenerationResult> GenerateFinalQuizAsync(Course course, IReadOnlyList<Lesson> lessons, CancellationToken cancellationToken = default);
}
```

Create `backend/CourseVideo.API/Services/OpenRouterQuizGenerationService.cs` with these critical behaviors:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CourseVideo.API.Configuration;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;
using CourseVideo.API.Models.OpenRouter;
using CourseVideo.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services;

public class OpenRouterQuizGenerationService : IOpenRouterQuizGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterQuizGenerationService> _logger;

    public OpenRouterQuizGenerationService(HttpClient httpClient, IOptions<OpenRouterOptions> options, ILogger<OpenRouterQuizGenerationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<OpenRouterQuizGenerationResult> GenerateLessonQuizAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken = default) =>
        GenerateAsync(BuildLessonPrompt(course, module, lesson, CalculateLessonQuestionCount(lesson.ContentSeed)), cancellationToken);

    public Task<OpenRouterQuizGenerationResult> GenerateFinalQuizAsync(Course course, IReadOnlyList<Lesson> lessons, CancellationToken cancellationToken = default) =>
        GenerateAsync(BuildFinalPrompt(course, lessons, CalculateFinalQuestionCount(lessons)), cancellationToken);

    private async Task<OpenRouterQuizGenerationResult> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Thiếu cấu hình OPENROUTER_API_KEY.");
        }

        var requestBody = new OpenRouterChatCompletionRequest
        {
            Model = _options.Model,
            Temperature = 0.1,
            Messages =
            [
                new OpenRouterMessage
                {
                    Role = "system",
                    Content = "Ban la nguoi tao quiz hoc tap bang tieng Viet co dau. Chi tra ve JSON hop le."
                },
                new OpenRouterMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ]
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenRouter quiz generation failed with HTTP {(int)response.StatusCode}.");
        }

        var envelope = JsonSerializer.Deserialize<OpenRouterChatCompletionResponse>(payload, JsonOptions)
            ?? throw new InvalidOperationException("OpenRouter quiz response is empty.");

        var content = envelope.Choices.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException("OpenRouter quiz content is empty.");

        var result = JsonSerializer.Deserialize<OpenRouterQuizGenerationResult>(content, JsonOptions)
            ?? throw new InvalidOperationException("OpenRouter quiz JSON is invalid.");

        Validate(result);
        return result;
    }

    private static void Validate(OpenRouterQuizGenerationResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Title) || result.Questions.Count == 0)
        {
            throw new InvalidOperationException("OpenRouter quiz JSON thieu title hoac questions.");
        }

        foreach (var question in result.Questions)
        {
            if (string.IsNullOrWhiteSpace(question.QuestionText) ||
                string.IsNullOrWhiteSpace(question.Explanation) ||
                question.Options.Count != 4 ||
                question.Options.Count(x => x.IsCorrect) != 1)
            {
                throw new InvalidOperationException("OpenRouter quiz JSON khong dung schema nghiep vu.");
            }

            if (!ContainsVietnameseDiacritics(question.QuestionText) ||
                !ContainsVietnameseDiacritics(question.Explanation) ||
                question.Options.Any(x => string.IsNullOrWhiteSpace(x.OptionText)))
            {
                throw new InvalidOperationException("Quiz phai dung tieng Viet co dau va co noi dung hop le.");
            }
        }
    }

    private static bool ContainsVietnameseDiacritics(string value) =>
        value.Any(ch => "ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ".Contains(char.ToLowerInvariant(ch)));
}
```

- [ ] **Step 4: Register the service in ASP.NET Core only**

Modify `backend/CourseVideo.API/Program.cs` by adding:

```csharp
builder.Services.AddHttpClient<IOpenRouterQuizGenerationService, OpenRouterQuizGenerationService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
});
```

Do not modify `ai-worker/app/main.py`.

- [ ] **Step 5: Run tests to verify Task 2 passes**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter OpenRouterQuizGenerationServiceTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/DTOs/OpenRouter backend/CourseVideo.API/Services/Interfaces/IOpenRouterQuizGenerationService.cs backend/CourseVideo.API/Services/OpenRouterQuizGenerationService.cs backend/CourseVideo.API/Program.cs backend/CourseVideo.API.Tests/Services/OpenRouterQuizGenerationServiceTests.cs
git commit -m "feat: add dotnet openrouter quiz generation"
```

### Task 3: Add Quiz Generation Orchestration and Status Management

**Files:**
- Create: `backend/CourseVideo.API/Services/Interfaces/IQuizGenerationService.cs`
- Create: `backend/CourseVideo.API/Services/QuizGenerationService.cs`
- Create: `backend/CourseVideo.API.Tests/Services/QuizGenerationServiceTests.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs`

- [ ] **Step 1: Write the failing orchestration tests**

Create `backend/CourseVideo.API.Tests/Services/QuizGenerationServiceTests.cs`:

```csharp
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class QuizGenerationServiceTests
{
    [Fact]
    public async Task GenerateLessonQuizAsync_CreatesReadyQuiz_WhenOpenRouterReturnsValidPayload()
    {
        var course = new Course { Id = Guid.NewGuid(), Title = "AI", Description = "Desc" };
        var module = new Module { Id = Guid.NewGuid(), Title = "Module", Description = "Desc", Course = course, CourseId = course.Id };
        var lesson = new Lesson { Id = Guid.NewGuid(), Title = "Lesson", Description = "Desc", ContentSeed = "Noi dung ve tri tue nhan tao", Module = module, ModuleId = module.Id };
        module.Lessons = [lesson];
        course.Modules = [module];

        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdWithStructureAsync(course.Id)).ReturnsAsync(course);

        var quizRepository = new Mock<IQuizRepository>();
        quizRepository.Setup(x => x.GetLessonQuizAsync(lesson.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Quiz?)null);

        var openRouter = new Mock<IOpenRouterQuizGenerationService>();
        openRouter.Setup(x => x.GenerateLessonQuizAsync(course, module, lesson, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenRouterQuizGenerationResult
            {
                Title = "Quiz bai hoc",
                Questions =
                [
                    new OpenRouterQuizQuestionResult
                    {
                        QuestionText = "AI mo phong dieu gi?",
                        Explanation = "AI mo phong tri tue con nguoi.",
                        Options =
                        [
                            new OpenRouterQuizOptionResult { OptionText = "Tri tue con nguoi", IsCorrect = true },
                            new OpenRouterQuizOptionResult { OptionText = "May in", IsCorrect = false },
                            new OpenRouterQuizOptionResult { OptionText = "Loa", IsCorrect = false },
                            new OpenRouterQuizOptionResult { OptionText = "Ban phim", IsCorrect = false }
                        ]
                    }
                ]
            });

        Quiz? savedQuiz = null;
        quizRepository.Setup(x => x.AddAsync(It.IsAny<Quiz>(), It.IsAny<CancellationToken>()))
            .Callback<Quiz, CancellationToken>((quiz, _) => savedQuiz = quiz)
            .Returns(Task.CompletedTask);

        var service = new QuizGenerationService(courseRepository.Object, quizRepository.Object, openRouter.Object);

        await service.GenerateLessonQuizAsync(course.Id, lesson.Id);

        savedQuiz.Should().NotBeNull();
        savedQuiz!.Status.Should().Be("Ready");
        savedQuiz.Type.Should().Be("Lesson");
        savedQuiz.Questions.Should().HaveCount(1);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter QuizGenerationServiceTests
```

Expected: FAIL with missing `QuizGenerationService`.

- [ ] **Step 3: Implement orchestration and regenerate entry points**

Create `backend/CourseVideo.API/Services/Interfaces/IQuizGenerationService.cs`:

```csharp
namespace CourseVideo.API.Services.Interfaces;

public interface IQuizGenerationService
{
    Task GenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default);
    Task GenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task RegenerateQuizAsync(Guid quizId, CancellationToken cancellationToken = default);
}
```

Create `backend/CourseVideo.API/Services/QuizGenerationService.cs`:

```csharp
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class QuizGenerationService : IQuizGenerationService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IQuizRepository _quizRepository;
    private readonly IOpenRouterQuizGenerationService _openRouterQuizGenerationService;

    public QuizGenerationService(ICourseRepository courseRepository, IQuizRepository quizRepository, IOpenRouterQuizGenerationService openRouterQuizGenerationService)
    {
        _courseRepository = courseRepository;
        _quizRepository = quizRepository;
        _openRouterQuizGenerationService = openRouterQuizGenerationService;
    }

    public async Task GenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Khong tim thay khoa hoc.");

        var module = course.Modules.FirstOrDefault(x => x.Lessons.Any(l => l.Id == lessonId))
            ?? throw new KeyNotFoundException("Khong tim thay module cua lesson.");
        var lesson = module.Lessons.First(x => x.Id == lessonId);

        var existingQuiz = await _quizRepository.GetLessonQuizAsync(lessonId, cancellationToken);
        var generated = await _openRouterQuizGenerationService.GenerateLessonQuizAsync(course, module, lesson, cancellationToken);

        var quiz = existingQuiz ?? new Quiz
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            Type = "Lesson",
            CreatedAt = DateTime.UtcNow
        };

        quiz.Status = "Ready";
        quiz.Title = generated.Title;
        quiz.QuestionCount = generated.Questions.Count;
        quiz.GenerationError = null;
        quiz.LastGeneratedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        quiz.Questions = generated.Questions.Select((question, index) => new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuestionText = question.QuestionText,
            Explanation = question.Explanation,
            OrderIndex = index + 1,
            CreatedAt = DateTime.UtcNow,
            Options = question.Options.Select((option, optionIndex) => new QuizOption
            {
                Id = Guid.NewGuid(),
                OptionText = option.OptionText,
                OrderIndex = optionIndex + 1,
                IsCorrect = option.IsCorrect,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        }).ToList();

        if (existingQuiz is null)
        {
            await _quizRepository.AddAsync(quiz, cancellationToken);
        }

        await _quizRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task GenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Khong tim thay khoa hoc.");
        var lessons = course.Modules.SelectMany(x => x.Lessons).OrderBy(x => x.OrderIndex).ToList();
        var existingQuiz = await _quizRepository.GetCourseFinalQuizAsync(courseId, cancellationToken);
        var generated = await _openRouterQuizGenerationService.GenerateFinalQuizAsync(course, lessons, cancellationToken);

        var quiz = existingQuiz ?? new Quiz
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Type = "Final",
            CreatedAt = DateTime.UtcNow
        };

        quiz.Status = "Ready";
        quiz.Title = generated.Title;
        quiz.QuestionCount = generated.Questions.Count;
        quiz.GenerationError = null;
        quiz.LastGeneratedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        quiz.Questions = generated.Questions.Select((question, index) => new QuizQuestion
        {
            Id = Guid.NewGuid(),
            QuestionText = question.QuestionText,
            Explanation = question.Explanation,
            OrderIndex = index + 1,
            CreatedAt = DateTime.UtcNow,
            Options = question.Options.Select((option, optionIndex) => new QuizOption
            {
                Id = Guid.NewGuid(),
                OptionText = option.OptionText,
                OrderIndex = optionIndex + 1,
                IsCorrect = option.IsCorrect,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        }).ToList();

        if (existingQuiz is null)
        {
            await _quizRepository.AddAsync(quiz, cancellationToken);
        }

        await _quizRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RegenerateQuizAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken)
            ?? throw new KeyNotFoundException("Khong tim thay quiz.");

        if (quiz.LessonId.HasValue && quiz.CourseId.HasValue)
        {
            await GenerateLessonQuizAsync(quiz.CourseId.Value, quiz.LessonId.Value, cancellationToken);
            return;
        }

        if (quiz.CourseId.HasValue)
        {
            await GenerateFinalQuizAsync(quiz.CourseId.Value, cancellationToken);
            return;
        }

        throw new InvalidOperationException("Quiz khong co target de regenerate.");
    }
}
```

- [ ] **Step 4: Register services and expose generation hooks from CourseService**

Modify `backend/CourseVideo.API/Program.cs`:

```csharp
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuizGenerationService, QuizGenerationService>();
```

Modify `backend/CourseVideo.API/Services/CourseService.cs` constructor and methods to inject `IQuizGenerationService` and add:

```csharp
public Task GenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default) =>
    _quizGenerationService.GenerateLessonQuizAsync(courseId, lessonId, cancellationToken);

public Task GenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default) =>
    _quizGenerationService.GenerateFinalQuizAsync(courseId, cancellationToken);
```

Modify `backend/CourseVideo.API/Services/Interfaces/ICourseService.cs` to declare the two methods.

- [ ] **Step 5: Run tests to verify Task 3 passes**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "QuizGenerationServiceTests|CourseServiceTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Services/Interfaces/IQuizGenerationService.cs backend/CourseVideo.API/Services/QuizGenerationService.cs backend/CourseVideo.API/Services/CourseService.cs backend/CourseVideo.API/Services/Interfaces/ICourseService.cs backend/CourseVideo.API/Program.cs backend/CourseVideo.API.Tests/Services/QuizGenerationServiceTests.cs backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs
git commit -m "feat: orchestrate quiz generation in dotnet"
```

### Task 4: Add Learner Quiz APIs and Server-Side Scoring

**Files:**
- Create: `backend/CourseVideo.API/Services/Interfaces/IQuizService.cs`
- Create: `backend/CourseVideo.API/Services/QuizService.cs`
- Create: `backend/CourseVideo.API/DTOs/Quizzes/*.cs`
- Create: `backend/CourseVideo.API/Controllers/QuizzesController.cs`
- Create: `backend/CourseVideo.API/Controllers/AdminQuizzesController.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/QuizzesControllerTests.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/AdminQuizzesControllerTests.cs`
- Modify: `backend/CourseVideo.API/Program.cs`

- [ ] **Step 1: Write the failing controller and scoring tests**

Create `backend/CourseVideo.API.Tests/Controllers/QuizzesControllerTests.cs`:

```csharp
using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.Quizzes;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class QuizzesControllerTests
{
    [Fact]
    public async Task GetLessonQuiz_ReturnsOk_WhenQuizExists()
    {
        var service = new Mock<IQuizService>();
        service.Setup(x => x.GetLessonQuizAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuizResponse
            {
                QuizId = Guid.NewGuid(),
                Title = "Quiz",
                Status = "Ready",
                QuestionCount = 1
            });

        var controller = new QuizzesController(service.Object);

        var result = await controller.GetLessonQuiz(Guid.NewGuid());

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
```

Extend `backend/CourseVideo.API.Tests/Services/QuizServiceTests.cs` with the submit-scoring test:

```csharp
[Fact]
public async Task SubmitAttemptAsync_ComputesScore_AndReturnsCorrectAnswers()
{
    var quizId = Guid.NewGuid();
    var attemptId = Guid.NewGuid();
    var correctOptionId = Guid.NewGuid();
    var wrongOptionId = Guid.NewGuid();
    var repository = new Mock<IQuizRepository>();
    repository.Setup(x => x.GetByIdAsync(quizId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Quiz
        {
            Id = quizId,
            Status = "Ready",
            Questions =
            [
                new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    QuestionText = "AI giup gi?",
                    Explanation = "AI ho tro giai quyet bai toan tri tue.",
                    Options =
                    [
                        new QuizOption { Id = correctOptionId, OptionText = "Ho tro bai toan tri tue", IsCorrect = true, OrderIndex = 1 },
                        new QuizOption { Id = wrongOptionId, OptionText = "Chi de nghe nhac", IsCorrect = false, OrderIndex = 2 },
                        new QuizOption { Id = Guid.NewGuid(), OptionText = "Chi de luu file", IsCorrect = false, OrderIndex = 3 },
                        new QuizOption { Id = Guid.NewGuid(), OptionText = "Chi de in giay", IsCorrect = false, OrderIndex = 4 }
                    ]
                }
            ]
        });

    var service = new QuizService(repository.Object);

    var started = await service.StartAttemptAsync(quizId, Guid.NewGuid());
    var submitted = await service.SubmitAttemptAsync(
        quizId,
        started.AttemptId,
        Guid.NewGuid(),
        new SubmitQuizAttemptRequest
        {
            Answers =
            [
                new SubmitQuizAttemptAnswerRequest
                {
                    QuestionId = repository.Object.GetByIdAsync(quizId).Result!.Questions.First().Id,
                    SelectedOptionId = correctOptionId
                }
            ]
        });

    submitted.Score.Should().Be(100);
    submitted.CorrectCount.Should().Be(1);
    submitted.Answers.Should().ContainSingle(x => x.IsCorrect);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "QuizzesControllerTests|QuizServiceTests"
```

Expected: FAIL with missing DTOs and controller endpoints.

- [ ] **Step 3: Implement DTOs and `QuizService`**

Create the DTOs with these minimal shapes:

`backend/CourseVideo.API/DTOs/Quizzes/QuizResponse.cs`
```csharp
namespace CourseVideo.API.DTOs.Quizzes;

public class QuizResponse
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public IReadOnlyList<QuizQuestionResponse> Questions { get; set; } = [];
}
```

`backend/CourseVideo.API/DTOs/Quizzes/QuizQuestionResponse.cs`
```csharp
namespace CourseVideo.API.DTOs.Quizzes;

public class QuizQuestionResponse
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public IReadOnlyList<QuizOptionResponse> Options { get; set; } = [];
}
```

`backend/CourseVideo.API/DTOs/Quizzes/QuizOptionResponse.cs`
```csharp
namespace CourseVideo.API.DTOs.Quizzes;

public class QuizOptionResponse
{
    public Guid OptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
}
```

`backend/CourseVideo.API/DTOs/Quizzes/CreateQuizAttemptResponse.cs`
```csharp
namespace CourseVideo.API.DTOs.Quizzes;

public class CreateQuizAttemptResponse
{
    public Guid AttemptId { get; set; }
    public DateTime StartedAt { get; set; }
}
```

`backend/CourseVideo.API/DTOs/Quizzes/SubmitQuizAttemptRequest.cs`
```csharp
namespace CourseVideo.API.DTOs.Quizzes;

public class SubmitQuizAttemptRequest
{
    public List<SubmitQuizAttemptAnswerRequest> Answers { get; set; } = [];
}
```

`backend/CourseVideo.API/DTOs/Quizzes/SubmitQuizAttemptAnswerRequest.cs`
```csharp
namespace CourseVideo.API.DTOs.Quizzes;

public class SubmitQuizAttemptAnswerRequest
{
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
}
```

`backend/CourseVideo.API/DTOs/Quizzes/SubmitQuizAttemptResponse.cs`
```csharp
namespace CourseVideo.API.DTOs.Quizzes;

public class SubmitQuizAttemptResponse
{
    public Guid AttemptId { get; set; }
    public decimal Score { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public IReadOnlyList<SubmitQuizAttemptAnswerResultResponse> Answers { get; set; } = [];
}

public class SubmitQuizAttemptAnswerResultResponse
{
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public Guid CorrectOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
```

Implement `backend/CourseVideo.API/Services/Interfaces/IQuizService.cs` and `backend/CourseVideo.API/Services/QuizService.cs` with these public methods:

```csharp
public interface IQuizService
{
    Task<QuizResponse?> GetLessonQuizAsync(Guid lessonId, Guid userId, bool canPreviewDraft, CancellationToken cancellationToken = default);
    Task<QuizResponse?> GetFinalQuizAsync(Guid courseId, Guid userId, bool canPreviewDraft, CancellationToken cancellationToken = default);
    Task<CreateQuizAttemptResponse> StartAttemptAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default);
    Task<SubmitQuizAttemptResponse> SubmitAttemptAsync(Guid quizId, Guid attemptId, Guid userId, SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuizAttemptHistoryItemResponse>> GetAttemptHistoryAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default);
}
```

Key `SubmitAttemptAsync` scoring logic:

```csharp
var answerResults = quiz.Questions.Select(question =>
{
    var submitted = request.Answers.FirstOrDefault(x => x.QuestionId == question.Id)
        ?? throw new InvalidOperationException("Thieu cau tra loi cho quiz.");
    var correctOption = question.Options.Single(x => x.IsCorrect);
    var isCorrect = submitted.SelectedOptionId == correctOption.Id;

    return new SubmitQuizAttemptAnswerResultResponse
    {
        QuestionId = question.Id,
        SelectedOptionId = submitted.SelectedOptionId,
        CorrectOptionId = correctOption.Id,
        IsCorrect = isCorrect,
        Explanation = question.Explanation
    };
}).ToList();

var correctCount = answerResults.Count(x => x.IsCorrect);
var score = quiz.Questions.Count == 0 ? 0 : Math.Round((decimal)correctCount * 100 / quiz.Questions.Count, 2);
```

- [ ] **Step 4: Implement learner/admin controllers**

Create `backend/CourseVideo.API/Controllers/QuizzesController.cs`:

```csharp
using System.Security.Claims;
using CourseVideo.API.DTOs.Quizzes;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet("lessons/{lessonId:guid}/quiz")]
    public async Task<ActionResult<QuizResponse>> GetLessonQuiz(Guid lessonId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.Claims.Any(x => x.Type == ClaimTypes.Role && x.Value == "Admin");
        var quiz = await _quizService.GetLessonQuizAsync(lessonId, userId, isAdmin, cancellationToken);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpGet("courses/{courseId:guid}/final-quiz")]
    public async Task<ActionResult<QuizResponse>> GetFinalQuiz(Guid courseId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.Claims.Any(x => x.Type == ClaimTypes.Role && x.Value == "Admin");
        var quiz = await _quizService.GetFinalQuizAsync(courseId, userId, isAdmin, cancellationToken);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpPost("quizzes/{quizId:guid}/attempts")]
    public async Task<ActionResult<CreateQuizAttemptResponse>> StartAttempt(Guid quizId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _quizService.StartAttemptAsync(quizId, userId, cancellationToken));
    }

    [HttpPost("quizzes/{quizId:guid}/attempts/{attemptId:guid}/submit")]
    public async Task<ActionResult<SubmitQuizAttemptResponse>> SubmitAttempt(Guid quizId, Guid attemptId, [FromBody] SubmitQuizAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _quizService.SubmitAttemptAsync(quizId, attemptId, userId, request, cancellationToken));
    }
}
```

Create `backend/CourseVideo.API/Controllers/AdminQuizzesController.cs`:

```csharp
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseVideo.API.Controllers;

[ApiController]
[Route("api/admin/quizzes")]
[Authorize(Roles = "Admin")]
public class AdminQuizzesController : ControllerBase
{
    private readonly IQuizGenerationService _quizGenerationService;

    public AdminQuizzesController(IQuizGenerationService quizGenerationService)
    {
        _quizGenerationService = quizGenerationService;
    }

    [HttpPost("{quizId:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid quizId, CancellationToken cancellationToken = default)
    {
        await _quizGenerationService.RegenerateQuizAsync(quizId, cancellationToken);
        return Accepted();
    }
}
```

Register service in `backend/CourseVideo.API/Program.cs`:

```csharp
builder.Services.AddScoped<IQuizService, QuizService>();
```

- [ ] **Step 5: Run tests to verify Task 4 passes**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "QuizzesControllerTests|AdminQuizzesControllerTests|QuizServiceTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Controllers/QuizzesController.cs backend/CourseVideo.API/Controllers/AdminQuizzesController.cs backend/CourseVideo.API/DTOs/Quizzes backend/CourseVideo.API/Services/Interfaces/IQuizService.cs backend/CourseVideo.API/Services/QuizService.cs backend/CourseVideo.API/Program.cs backend/CourseVideo.API.Tests/Controllers/QuizzesControllerTests.cs backend/CourseVideo.API.Tests/Controllers/AdminQuizzesControllerTests.cs backend/CourseVideo.API.Tests/Services/QuizServiceTests.cs
git commit -m "feat: add quiz learner api and scoring"
```

### Task 5: Surface Quiz Data in Learn Payload and Frontend API Layer

**Files:**
- Modify: `backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs`
- Modify: `backend/CourseVideo.API/DTOs/Courses/CourseLearnResponse.cs`
- Modify: `backend/CourseVideo.API/Services/CourseService.cs`
- Create: `frontend/src/api/quizService.js`
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`

- [ ] **Step 1: Write the failing learn payload test**

Extend `backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs`:

```csharp
[Fact]
public async Task GetLearnPayloadAsync_MapsQuizFlags_ForLessonAndFinalQuiz()
{
    var repository = new Mock<ICourseRepository>();
    var courseId = Guid.NewGuid();
    var lessonId = Guid.NewGuid();
    repository.Setup(x => x.GetByIdWithStructureAsync(courseId)).ReturnsAsync(new Course
    {
        Id = courseId,
        Title = "AI",
        Description = "Desc",
        IsPublished = true,
        Modules =
        [
            new Module
            {
                Id = Guid.NewGuid(),
                Title = "Module",
                Description = "Desc",
                OrderIndex = 1,
                Lessons =
                [
                    new Lesson
                    {
                        Id = lessonId,
                        Title = "Lesson",
                        Description = "Desc",
                        ContentSeed = "Noi dung",
                        OrderIndex = 1
                    }
                ]
            }
        ],
        Quizzes =
        [
            new Quiz { Id = Guid.NewGuid(), CourseId = courseId, Type = "Final", Status = "Ready", Title = "Final Quiz", QuestionCount = 10 }
        ]
    });

    var service = CreateCourseService(repository);

    var result = await service.GetLearnPayloadAsync(courseId, true);

    result.Should().NotBeNull();
    result!.HasFinalQuiz.Should().BeTrue();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter CourseServiceTests
```

Expected: FAIL with missing quiz-mapping properties.

- [ ] **Step 3: Extend course DTOs and service mapping**

Add properties to `backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs`:

```csharp
public Guid? QuizId { get; set; }
public string QuizStatus { get; set; } = string.Empty;
public int QuizQuestionCount { get; set; }
```

Add properties to `backend/CourseVideo.API/DTOs/Courses/CourseLearnResponse.cs`:

```csharp
public Guid? FinalQuizId { get; set; }
public bool HasFinalQuiz { get; set; }
public string FinalQuizStatus { get; set; } = string.Empty;
public int FinalQuizQuestionCount { get; set; }
```

Update `backend/CourseVideo.API/Services/CourseService.cs` in `GetLearnPayloadAsync(...)` and `MapLearnLesson(...)` to map the linked quiz records:

```csharp
var lessonQuizLookup = course.Modules
    .SelectMany(x => x.Lessons)
    .Join(course.Quizzes.Where(q => q.LessonId.HasValue), lesson => lesson.Id, quiz => quiz.LessonId!.Value, (lesson, quiz) => new { lesson.Id, Quiz = quiz })
    .ToDictionary(x => x.Id, x => x.Quiz);

var finalQuiz = course.Quizzes.FirstOrDefault(x => x.CourseId == course.Id && x.Type == "Final");
```

and:

```csharp
QuizId = lessonQuizLookup.TryGetValue(lesson.Id, out var quiz) ? quiz.Id : null,
QuizStatus = lessonQuizLookup.TryGetValue(lesson.Id, out quiz) ? quiz.Status : string.Empty,
QuizQuestionCount = lessonQuizLookup.TryGetValue(lesson.Id, out quiz) ? quiz.QuestionCount : 0
```

Also update `backend/CourseVideo.API/Models/Course.cs` to expose:

```csharp
public ICollection<Quiz> Quizzes { get; set; } = [];
```

- [ ] **Step 4: Add frontend quiz API adapter**

Create `frontend/src/api/quizService.js`:

```javascript
import { axiosClient } from "./axiosClient";

export async function getLessonQuiz(lessonId) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/quiz`);
  return data;
}

export async function getFinalQuiz(courseId) {
  const { data } = await axiosClient.get(`/courses/${courseId}/final-quiz`);
  return data;
}

export async function startQuizAttempt(quizId) {
  const { data } = await axiosClient.post(`/quizzes/${quizId}/attempts`);
  return data;
}

export async function submitQuizAttempt(quizId, attemptId, payload) {
  const { data } = await axiosClient.post(`/quizzes/${quizId}/attempts/${attemptId}/submit`, payload);
  return data;
}
```

- [ ] **Step 5: Run tests to verify Task 5 passes**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter CourseServiceTests
npx vitest run frontend/src/pages/CourseLearnPage.test.jsx
```

Expected: backend PASS, frontend still failing or unchanged until Task 6.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/DTOs/Courses/CourseLearnLessonResponse.cs backend/CourseVideo.API/DTOs/Courses/CourseLearnResponse.cs backend/CourseVideo.API/Models/Course.cs backend/CourseVideo.API/Services/CourseService.cs backend/CourseVideo.API.Tests/Services/CourseServiceTests.cs frontend/src/api/quizService.js
git commit -m "feat: expose quiz metadata on learn payload"
```

### Task 6: Build Lesson Quiz and Final Quiz UI in the Learner Page

**Files:**
- Create: `frontend/src/components/course/LessonQuizPanel.jsx`
- Create: `frontend/src/components/course/FinalQuizCard.jsx`
- Create: `frontend/src/components/course/QuizAttemptResult.jsx`
- Create: `frontend/src/components/course/LessonQuizPanel.test.jsx`
- Create: `frontend/src/components/course/FinalQuizCard.test.jsx`
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Write the failing component tests**

Create `frontend/src/components/course/LessonQuizPanel.test.jsx`:

```jsx
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonQuizPanel from "./LessonQuizPanel";

describe("LessonQuizPanel", () => {
  it("loads quiz and submits answers", async () => {
    const onLoadQuiz = vi.fn().mockResolvedValue({
      quizId: "quiz-1",
      title: "Kiem tra nhanh",
      status: "Ready",
      questionCount: 1,
      questions: [
        {
          questionId: "q1",
          questionText: "AI mo phong dieu gi?",
          explanation: "AI mo phong tri tue con nguoi.",
          options: [
            { optionId: "o1", optionText: "Tri tue con nguoi" },
            { optionId: "o2", optionText: "May in" },
            { optionId: "o3", optionText: "Ban phim" },
            { optionId: "o4", optionText: "Loa" }
          ]
        }
      ]
    });

    const onStartAttempt = vi.fn().mockResolvedValue({ attemptId: "attempt-1", startedAt: "2026-05-28T00:00:00Z" });
    const onSubmitAttempt = vi.fn().mockResolvedValue({
      attemptId: "attempt-1",
      score: 100,
      correctCount: 1,
      totalQuestions: 1,
      answers: [
        {
          questionId: "q1",
          selectedOptionId: "o1",
          correctOptionId: "o1",
          isCorrect: true,
          explanation: "AI mo phong tri tue con nguoi."
        }
      ]
    });

    render(
      <LessonQuizPanel
        lessonId="lesson-1"
        initialStatus="Ready"
        onLoadQuiz={onLoadQuiz}
        onStartAttempt={onStartAttempt}
        onSubmitAttempt={onSubmitAttempt}
      />
    );

    fireEvent.click(await screen.findByRole("button", { name: "Lam quiz" }));
    fireEvent.click(await screen.findByLabelText("Tri tue con nguoi"));
    fireEvent.click(screen.getByRole("button", { name: "Nop bai" }));

    expect(await screen.findByText("Diem: 100")).toBeInTheDocument();
    expect(screen.getByText("AI mo phong tri tue con nguoi.")).toBeInTheDocument();
  });
});
```

Create `frontend/src/components/course/FinalQuizCard.test.jsx`:

```jsx
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import FinalQuizCard from "./FinalQuizCard";

describe("FinalQuizCard", () => {
  it("renders final quiz CTA when ready", () => {
    render(<FinalQuizCard courseId="course-1" quizId="quiz-1" status="Ready" questionCount={15} />);

    expect(screen.getByText("Quiz tong ket khoa hoc")).toBeInTheDocument();
    expect(screen.getByText("15 cau hoi")).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
npx vitest run frontend/src/components/course/LessonQuizPanel.test.jsx frontend/src/components/course/FinalQuizCard.test.jsx
```

Expected: FAIL with missing component files.

- [ ] **Step 3: Implement reusable quiz UI components**

Create `frontend/src/components/course/QuizAttemptResult.jsx`:

```jsx
export default function QuizAttemptResult({ result, questions }) {
  return (
    <div className="quiz-result">
      <p className="quiz-result__headline">Diem: {result.score}</p>
      <p className="quiz-result__meta">Dung {result.correctCount}/{result.totalQuestions} cau</p>
      <div className="quiz-result__answers">
        {questions.map((question) => {
          const answer = result.answers.find((item) => item.questionId === question.questionId);
          return (
            <article className="quiz-result__answer" key={question.questionId}>
              <strong>{question.questionText}</strong>
              <p>{answer?.isCorrect ? "Ban tra loi dung." : "Ban tra loi chua dung."}</p>
              <p>{question.options.find((option) => option.optionId === answer?.correctOptionId)?.optionText}</p>
              <p>{answer?.explanation}</p>
            </article>
          );
        })}
      </div>
    </div>
  );
}
```

Create `frontend/src/components/course/LessonQuizPanel.jsx`:

```jsx
import { useState } from "react";
import QuizAttemptResult from "./QuizAttemptResult";

export default function LessonQuizPanel({ lessonId, initialStatus, onLoadQuiz, onStartAttempt, onSubmitAttempt }) {
  const [quiz, setQuiz] = useState(null);
  const [attemptId, setAttemptId] = useState("");
  const [answers, setAnswers] = useState({});
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  async function handleStart() {
    setIsLoading(true);
    const loadedQuiz = await onLoadQuiz(lessonId);
    const started = await onStartAttempt(loadedQuiz.quizId);
    setQuiz(loadedQuiz);
    setAttemptId(started.attemptId);
    setResult(null);
    setAnswers({});
    setIsLoading(false);
  }

  async function handleSubmit() {
    const payload = {
      answers: quiz.questions.map((question) => ({
        questionId: question.questionId,
        selectedOptionId: answers[question.questionId]
      }))
    };
    const submitted = await onSubmitAttempt(quiz.quizId, attemptId, payload);
    setResult(submitted);
  }

  if (initialStatus && initialStatus !== "Ready") {
    return <div className="lesson-quiz-panel"><p>Quiz dang duoc chuan bi.</p></div>;
  }

  return (
    <div className="lesson-quiz-panel">
      <div className="lesson-quiz-panel__header">
        <h3>Kiem tra nhanh sau bai hoc</h3>
      </div>
      {!quiz ? (
        <button disabled={isLoading} onClick={handleStart} type="button">
          Lam quiz
        </button>
      ) : (
        <>
          <h4>{quiz.title}</h4>
          {quiz.questions.map((question) => (
            <fieldset className="lesson-quiz-panel__question" key={question.questionId}>
              <legend>{question.questionText}</legend>
              {question.options.map((option) => (
                <label key={option.optionId}>
                  <input
                    checked={answers[question.questionId] === option.optionId}
                    name={question.questionId}
                    onChange={() => setAnswers((current) => ({ ...current, [question.questionId]: option.optionId }))}
                    type="radio"
                  />
                  {option.optionText}
                </label>
              ))}
            </fieldset>
          ))}
          <button onClick={handleSubmit} type="button">Nop bai</button>
          {result ? <QuizAttemptResult questions={quiz.questions} result={result} /> : null}
        </>
      )}
    </div>
  );
}
```

Create `frontend/src/components/course/FinalQuizCard.jsx`:

```jsx
import { Link } from "react-router-dom";

export default function FinalQuizCard({ courseId, quizId, status, questionCount }) {
  return (
    <div className="final-quiz-card">
      <p className="final-quiz-card__eyebrow">Danh gia tong ket</p>
      <h3>Quiz tong ket khoa hoc</h3>
      <p>{questionCount} cau hoi</p>
      {status === "Ready" ? (
        <Link to={`/courses/${courseId}/learn?finalQuiz=${quizId}`}>Lam quiz tong ket</Link>
      ) : (
        <p>Quiz dang duoc chuan bi.</p>
      )}
    </div>
  );
}
```

- [ ] **Step 4: Wire the learner page and styles**

Modify `frontend/src/pages/CourseLearnPage.jsx`:

```jsx
import { getFinalQuiz, getLessonQuiz, startQuizAttempt, submitQuizAttempt } from "../api/quizService";
import FinalQuizCard from "../components/course/FinalQuizCard";
import LessonQuizPanel from "../components/course/LessonQuizPanel";
```

Render the lesson quiz between the reading card and comments:

```jsx
<LessonQuizPanel
  lessonId={selectedLesson.lessonId}
  initialStatus={selectedLesson.quizStatus}
  onLoadQuiz={getLessonQuiz}
  onStartAttempt={startQuizAttempt}
  onSubmitAttempt={submitQuizAttempt}
/>
```

Render the final quiz card in the sidebar header area:

```jsx
{course.hasFinalQuiz ? (
  <FinalQuizCard
    courseId={course.courseId}
    quizId={course.finalQuizId}
    status={course.finalQuizStatus}
    questionCount={course.finalQuizQuestionCount}
  />
) : null}
```

Append to `frontend/src/styles/theme.css`:

```css
.lesson-quiz-panel,
.final-quiz-card,
.quiz-result {
  border: 1px solid var(--color-charcoal-border);
  border-radius: 16px;
  background: #ffffff;
  box-shadow: var(--shadow-subtle);
  padding: 20px;
}

.lesson-quiz-panel__question {
  border: 0;
  padding: 0;
  margin: 0 0 20px;
  display: grid;
  gap: 12px;
}

.lesson-quiz-panel label {
  display: flex;
  gap: 10px;
  align-items: start;
}

.quiz-result__headline {
  font-weight: 700;
}

.final-quiz-card {
  margin-top: 20px;
}
```

- [ ] **Step 5: Run tests to verify Task 6 passes**

Run:

```bash
npx vitest run frontend/src/components/course/LessonQuizPanel.test.jsx frontend/src/components/course/FinalQuizCard.test.jsx frontend/src/pages/CourseLearnPage.test.jsx
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/quizService.js frontend/src/components/course/LessonQuizPanel.jsx frontend/src/components/course/FinalQuizCard.jsx frontend/src/components/course/QuizAttemptResult.jsx frontend/src/components/course/LessonQuizPanel.test.jsx frontend/src/components/course/FinalQuizCard.test.jsx frontend/src/pages/CourseLearnPage.jsx frontend/src/pages/CourseLearnPage.test.jsx frontend/src/styles/theme.css
git commit -m "feat: add learner quiz experience"
```

### Task 7: End-to-End Verification and Cleanup

**Files:**
- Modify as needed based on failures from previous tasks

- [ ] **Step 1: Run the focused backend suite**

Run:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "QuizServiceTests|QuizGenerationServiceTests|OpenRouterQuizGenerationServiceTests|QuizzesControllerTests|AdminQuizzesControllerTests|CourseServiceTests"
```

Expected: PASS.

- [ ] **Step 2: Run the focused frontend suite**

Run:

```bash
npx vitest run frontend/src/components/course/LessonQuizPanel.test.jsx frontend/src/components/course/FinalQuizCard.test.jsx frontend/src/pages/CourseLearnPage.test.jsx
```

Expected: PASS.

- [ ] **Step 3: Run a production build for frontend**

Run:

```bash
npm run build --prefix frontend
```

Expected: Vite build completes successfully without errors.

- [ ] **Step 4: Run a final diff review**

Run:

```bash
git diff --stat HEAD~7..HEAD
git diff -- backend/CourseVideo.API frontend/src
```

Expected: only quiz-related backend and frontend files changed; no `ai-worker` quiz changes.

- [ ] **Step 5: Commit any final fixes**

```bash
git add backend/CourseVideo.API frontend/src
git commit -m "test: verify quiz assessment flow"
```

---

## Self-Review

### Spec coverage

- Lesson quiz: covered in Tasks 1, 3, 4, 6
- Final quiz: covered in Tasks 3, 5, 6
- Vietnamese, concise, on-topic AI generation: covered in Task 2 validation and prompt rules
- Unlimited attempts: covered in Task 4 persistence and scoring
- Show answer + explanation after submit: covered in Task 4 DTOs and Task 6 UI
- Admin regenerate: covered in Task 4 controller and Task 3 service
- Keep ASP.NET Core as the core: enforced in File Structure, Task 2, Task 3, Task 4, and final diff review

### Placeholder scan

- No `TODO`, `TBD`, or “appropriate handling” placeholders remain.
- Each code-bearing step includes concrete snippets and commands.

### Type consistency

- Quiz runtime types consistently use `Quiz`, `QuizQuestion`, `QuizOption`, `QuizAttempt`, `QuizAttemptAnswer`.
- Service names are consistent: `IQuizRepository`, `IQuizService`, `IQuizGenerationService`, `IOpenRouterQuizGenerationService`.
- Frontend API names are consistent: `getLessonQuiz`, `getFinalQuiz`, `startQuizAttempt`, `submitQuizAttempt`.
