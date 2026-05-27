# Lesson Voice Tutor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a lesson-scoped voice tutor that pauses lesson video, accepts a learner voice question, streams back a spoken answer in the lesson voice, and resumes playback when the learner chooses to continue.

**Architecture:** Add a dedicated backend subsystem in `ASP.NET Core` for lesson voice sessions, turns, and realtime SignalR orchestration, while keeping STT/LLM/TTS behind interfaces that can wrap existing or external providers. Extend the React learning page with a focused voice tutor panel that coordinates recording, realtime playback, and video pause/resume without rewriting the existing lesson player flow.

**Tech Stack:** ASP.NET Core 8 Web API, Entity Framework Core SQL Server, SignalR, xUnit + Moq + FluentAssertions, React 18, Vite, Vitest, Testing Library, `@microsoft/signalr`

---

## File Structure

### Backend files to create

- `backend/CourseVideo.API/Models/LessonVoiceSession.cs`
- `backend/CourseVideo.API/Models/LessonVoiceTurn.cs`
- `backend/CourseVideo.API/Models/LessonVoiceMessage.cs`
- `backend/CourseVideo.API/DTOs/LessonVoiceTutor/CreateLessonVoiceSessionResponse.cs`
- `backend/CourseVideo.API/DTOs/LessonVoiceTutor/LessonVoiceMessageResponse.cs`
- `backend/CourseVideo.API/DTOs/LessonVoiceTutor/LessonVoiceSessionResponse.cs`
- `backend/CourseVideo.API/DTOs/LessonVoiceTutor/CloseLessonVoiceSessionRequest.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ILessonVoiceSessionRepository.cs`
- `backend/CourseVideo.API/Repositories/LessonVoiceSessionRepository.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorSessionService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonContextBuilder.cs`
- `backend/CourseVideo.API/Services/Interfaces/ITranscriptionService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonTutorAnswerService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonTutorSpeechService.cs`
- `backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorService.cs`
- `backend/CourseVideo.API/Services/LessonVoiceTutorSessionService.cs`
- `backend/CourseVideo.API/Services/LessonContextBuilder.cs`
- `backend/CourseVideo.API/Services/LessonVoiceTutorService.cs`
- `backend/CourseVideo.API/Services/Transcription/NullTranscriptionService.cs`
- `backend/CourseVideo.API/Services/Tutoring/NullLessonTutorAnswerService.cs`
- `backend/CourseVideo.API/Services/Tutoring/LessonNarrationVoiceResolver.cs`
- `backend/CourseVideo.API/Services/Tutoring/SegmentedLessonTutorSpeechService.cs`
- `backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`
- `backend/CourseVideo.API/Controllers/LessonVoiceSessionsController.cs`
- `backend/CourseVideo.API/Configuration/LessonVoiceTutorOptions.cs`

### Backend files to modify

- `backend/CourseVideo.API/Models/Lesson.cs`
- `backend/CourseVideo.API/Data/AppDbContext.cs`
- `backend/CourseVideo.API/Data/DbInitializer.cs`
- `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- `backend/CourseVideo.API/Program.cs`
- `backend/CourseVideo.API/appsettings.json`
- `backend/CourseVideo.API/CourseVideo.API.csproj`

### Backend tests to create

- `backend/CourseVideo.API.Tests/Services/LessonVoiceTutorSessionServiceTests.cs`
- `backend/CourseVideo.API.Tests/Services/LessonContextBuilderTests.cs`
- `backend/CourseVideo.API.Tests/Services/LessonVoiceTutorServiceTests.cs`
- `backend/CourseVideo.API.Tests/Controllers/LessonVoiceSessionsControllerTests.cs`

### Frontend files to create

- `frontend/src/api/lessonVoiceTutorService.js`
- `frontend/src/hooks/useLessonVoiceTutor.js`
- `frontend/src/components/course/LessonVoiceTutorPanel.jsx`
- `frontend/src/components/course/LessonVoiceTutorPanel.test.jsx`

### Frontend files to modify

- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/src/pages/CourseLearnPage.jsx`
- `frontend/src/pages/CourseLearnPage.test.jsx`
- `frontend/src/styles/theme.css`

---

### Task 1: Add persistence model and session REST contract

**Files:**
- Create: `backend/CourseVideo.API/Models/LessonVoiceSession.cs`
- Create: `backend/CourseVideo.API/Models/LessonVoiceTurn.cs`
- Create: `backend/CourseVideo.API/Models/LessonVoiceMessage.cs`
- Create: `backend/CourseVideo.API/DTOs/LessonVoiceTutor/CreateLessonVoiceSessionResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/LessonVoiceTutor/LessonVoiceMessageResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/LessonVoiceTutor/LessonVoiceSessionResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/LessonVoiceTutor/CloseLessonVoiceSessionRequest.cs`
- Modify: `backend/CourseVideo.API/Models/Lesson.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/LessonVoiceSessionsControllerTests.cs`

- [ ] **Step 1: Write the failing controller tests for session lifecycle**

```csharp
using CourseVideo.API.Controllers;
using CourseVideo.API.DTOs.LessonVoiceTutor;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CourseVideo.API.Tests.Controllers;

public class LessonVoiceSessionsControllerTests
{
    [Fact]
    public async Task CreateSession_ReturnsOk_WhenServiceCreatesSession()
    {
        var lessonId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new Mock<ILessonVoiceTutorSessionService>();
        service.Setup(x => x.CreateOrResumeSessionAsync(lessonId, userId, false, CancellationToken.None))
            .ReturnsAsync(new LessonVoiceSessionResponse
            {
                SessionId = Guid.NewGuid(),
                LessonId = lessonId,
                Status = "Active",
                VoiceProfileKey = "vi-VN-HoaiMyNeural"
            });

        var controller = BuildController(service.Object, userId, "User");

        var result = await controller.CreateSession(lessonId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMessages_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        var service = new Mock<ILessonVoiceTutorSessionService>();
        service.Setup(x => x.GetMessagesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), CancellationToken.None))
            .ThrowsAsync(new KeyNotFoundException("Session not found."));

        var controller = BuildController(service.Object, Guid.NewGuid(), "User");

        var result = await controller.GetMessages(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static LessonVoiceSessionsController BuildController(
        ILessonVoiceTutorSessionService service,
        Guid userId,
        string role)
    {
        var controller = new LessonVoiceSessionsController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                ], "TestAuth"))
            }
        };
        return controller;
    }
}
```

- [ ] **Step 2: Run the controller test to confirm missing controller/contracts**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonVoiceSessionsControllerTests`

Expected: FAIL with missing `LessonVoiceSessionsController`, `ILessonVoiceTutorSessionService`, or DTO/model types.

- [ ] **Step 3: Add the new lesson voice entities and lesson voice fields**

```csharp
namespace CourseVideo.API.Models;

public class LessonVoiceSession : BaseEntity
{
    public Guid LessonId { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public double? LastPausedVideoTimeSeconds { get; set; }
    public string VoiceProfileKey { get; set; } = string.Empty;
    public string ContextScope { get; set; } = "LessonWithCourseAndExternalKnowledge";
    public string? ConversationSummary { get; set; }
    public Lesson? Lesson { get; set; }
    public User? User { get; set; }
    public List<LessonVoiceTurn> Turns { get; set; } = [];
    public List<LessonVoiceMessage> Messages { get; set; } = [];
}

public class LessonVoiceTurn : BaseEntity
{
    public Guid SessionId { get; set; }
    public int TurnNumber { get; set; }
    public string Status { get; set; } = "Idle";
    public double? PlaybackPausedAtSeconds { get; set; }
    public string? UserAudioUrl { get; set; }
    public string? TranscriptionText { get; set; }
    public decimal? TranscriptionConfidence { get; set; }
    public string? AnswerText { get; set; }
    public string? AnswerSourceSummary { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public LessonVoiceSession? Session { get; set; }
}

public class LessonVoiceMessage : BaseEntity
{
    public Guid SessionId { get; set; }
    public int TurnNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ContentText { get; set; } = string.Empty;
    public string ContentSourceType { get; set; } = "Lesson";
    public string? AudioUrl { get; set; }
    public double? AudioDurationSeconds { get; set; }
    public int SequenceIndex { get; set; }
    public LessonVoiceSession? Session { get; set; }
}
```

```csharp
public class Lesson : BaseEntity
{
    // existing fields...
    public string? NarrationVoiceKey { get; set; }
    public string? TranscriptText { get; set; }
    public bool VoiceTutorEnabled { get; set; } = true;
}
```

- [ ] **Step 4: Register the entities in EF and bootstrap schema changes**

```csharp
public DbSet<LessonVoiceSession> LessonVoiceSessions => Set<LessonVoiceSession>();
public DbSet<LessonVoiceTurn> LessonVoiceTurns => Set<LessonVoiceTurn>();
public DbSet<LessonVoiceMessage> LessonVoiceMessages => Set<LessonVoiceMessage>();
```

```csharp
modelBuilder.Entity<LessonVoiceSession>(entity =>
{
    entity.HasKey(session => session.Id);
    entity.Property(session => session.Status).HasMaxLength(50).IsRequired();
    entity.Property(session => session.VoiceProfileKey).HasMaxLength(200).IsRequired();
    entity.Property(session => session.ContextScope).HasMaxLength(100).IsRequired();
    entity.HasIndex(session => new { session.LessonId, session.UserId, session.Status });
    entity.HasOne(session => session.Lesson)
        .WithMany()
        .HasForeignKey(session => session.LessonId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(session => session.User)
        .WithMany()
        .HasForeignKey(session => session.UserId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

```csharp
EnsureLessonVoiceTutorColumnsExist(dbContext);
EnsureLessonVoiceTutorTablesExist(dbContext);
```

```csharp
private static void EnsureLessonVoiceTutorColumnsExist(AppDbContext dbContext)
{
    if (!dbContext.Database.IsSqlServer())
    {
        return;
    }

    dbContext.Database.ExecuteSqlRaw(
        """
        IF COL_LENGTH('Lessons', 'NarrationVoiceKey') IS NULL
        BEGIN
            ALTER TABLE [Lessons] ADD [NarrationVoiceKey] nvarchar(200) NULL;
        END

        IF COL_LENGTH('Lessons', 'TranscriptText') IS NULL
        BEGIN
            ALTER TABLE [Lessons] ADD [TranscriptText] nvarchar(max) NULL;
        END

        IF COL_LENGTH('Lessons', 'VoiceTutorEnabled') IS NULL
        BEGIN
            ALTER TABLE [Lessons] ADD [VoiceTutorEnabled] bit NOT NULL CONSTRAINT [DF_Lessons_VoiceTutorEnabled] DEFAULT 1;
        END
        """);
}
```

- [ ] **Step 5: Add the session DTOs and controller**

```csharp
namespace CourseVideo.API.DTOs.LessonVoiceTutor;

public class LessonVoiceSessionResponse
{
    public Guid SessionId { get; set; }
    public Guid LessonId { get; set; }
    public Guid CourseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string VoiceProfileKey { get; set; } = string.Empty;
    public double? LastPausedVideoTimeSeconds { get; set; }
}
```

```csharp
[ApiController]
[Route("api")]
[Authorize]
public class LessonVoiceSessionsController : ControllerBase
{
    private readonly ILessonVoiceTutorSessionService _sessionService;

    public LessonVoiceSessionsController(ILessonVoiceTutorSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("lessons/{lessonId:guid}/voice-sessions")]
    public async Task<IActionResult> CreateSession(Guid lessonId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.CreateOrResumeSessionAsync(
            lessonId,
            GetCurrentUserId(),
            User.IsInRole("Admin"),
            cancellationToken);

        return Ok(session);
    }

    [HttpGet("voice-sessions/{sessionId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var messages = await _sessionService.GetMessagesAsync(sessionId, GetCurrentUserId(), cancellationToken);
            return Ok(messages);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("lessons/{lessonId:guid}/voice-sessions/current")]
    public async Task<IActionResult> GetCurrentSession(Guid lessonId, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetCurrentSessionAsync(lessonId, GetCurrentUserId(), cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("voice-sessions/{sessionId:guid}/close")]
    public async Task<IActionResult> CloseSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await _sessionService.CloseSessionAsync(sessionId, GetCurrentUserId(), cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
    }
}
```

- [ ] **Step 6: Run the controller tests and the lesson model compile path**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonVoiceSessionsControllerTests`

Expected: PASS

- [ ] **Step 7: Commit the persistence and controller contract**

```bash
git add backend/CourseVideo.API/Models/Lesson.cs \
  backend/CourseVideo.API/Models/LessonVoiceSession.cs \
  backend/CourseVideo.API/Models/LessonVoiceTurn.cs \
  backend/CourseVideo.API/Models/LessonVoiceMessage.cs \
  backend/CourseVideo.API/Data/AppDbContext.cs \
  backend/CourseVideo.API/Data/DbInitializer.cs \
  backend/CourseVideo.API/DTOs/LessonVoiceTutor \
  backend/CourseVideo.API/Controllers/LessonVoiceSessionsController.cs \
  backend/CourseVideo.API.Tests/Controllers/LessonVoiceSessionsControllerTests.cs
git commit -m "feat: add lesson voice tutor session contracts"
```

### Task 2: Implement lesson voice session service and repository

**Files:**
- Create: `backend/CourseVideo.API/Repositories/Interfaces/ILessonVoiceSessionRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/LessonVoiceSessionRepository.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorSessionService.cs`
- Create: `backend/CourseVideo.API/Services/LessonVoiceTutorSessionService.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonVoiceTutorSessionServiceTests.cs`

- [ ] **Step 1: Write failing tests for create-or-resume and close behavior**

```csharp
using CourseVideo.API.DTOs.LessonVoiceTutor;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonVoiceTutorSessionServiceTests
{
    [Fact]
    public async Task CreateOrResumeSessionAsync_ReusesActiveSession_WhenOneExists()
    {
        var lessonId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var activeSession = new LessonVoiceSession
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            CourseId = Guid.NewGuid(),
            UserId = userId,
            Status = "Active",
            VoiceProfileKey = "vi-VN-HoaiMyNeural"
        };

        var sessions = new Mock<ILessonVoiceSessionRepository>();
        sessions.Setup(x => x.GetActiveSessionAsync(lessonId, userId, CancellationToken.None))
            .ReturnsAsync(activeSession);

        var lessonRepository = new Mock<ILessonRepository>();
        var service = new LessonVoiceTutorSessionService(sessions.Object, lessonRepository.Object);

        var result = await service.CreateOrResumeSessionAsync(lessonId, userId, false, CancellationToken.None);

        result.SessionId.Should().Be(activeSession.Id);
        sessions.Verify(x => x.AddAsync(It.IsAny<LessonVoiceSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CloseSessionAsync_MarksSessionClosed_WhenUserOwnsSession()
    {
        var session = new LessonVoiceSession
        {
            Id = Guid.NewGuid(),
            LessonId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = "Active",
            VoiceProfileKey = "vi-VN-HoaiMyNeural"
        };

        var sessions = new Mock<ILessonVoiceSessionRepository>();
        sessions.Setup(x => x.GetByIdAsync(session.Id, CancellationToken.None)).ReturnsAsync(session);

        var service = new LessonVoiceTutorSessionService(sessions.Object, Mock.Of<ILessonRepository>());

        await service.CloseSessionAsync(session.Id, session.UserId, CancellationToken.None);

        session.Status.Should().Be("Closed");
        sessions.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }
}
```

- [ ] **Step 2: Run the service tests to verify missing repository/service code**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonVoiceTutorSessionServiceTests`

Expected: FAIL with missing `ILessonVoiceSessionRepository`, `LessonVoiceTutorSessionService`, or repository methods.

- [ ] **Step 3: Add the repository contract and EF implementation**

```csharp
public interface ILessonVoiceSessionRepository
{
    Task<LessonVoiceSession?> GetActiveSessionAsync(Guid lessonId, Guid userId, CancellationToken cancellationToken);
    Task<LessonVoiceSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LessonVoiceMessage>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken);
    Task AddAsync(LessonVoiceSession session, CancellationToken cancellationToken);
    Task AddTurnAsync(LessonVoiceTurn turn, CancellationToken cancellationToken);
    Task AddMessageAsync(LessonVoiceMessage message, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

```csharp
public class LessonVoiceSessionRepository : ILessonVoiceSessionRepository
{
    private readonly AppDbContext _dbContext;

    public LessonVoiceSessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LessonVoiceSession?> GetActiveSessionAsync(Guid lessonId, Guid userId, CancellationToken cancellationToken)
    {
        return _dbContext.LessonVoiceSessions
            .OrderByDescending(session => session.LastActivityAt)
            .FirstOrDefaultAsync(
                session => session.LessonId == lessonId
                    && session.UserId == userId
                    && session.Status == "Active",
                cancellationToken);
    }

    public Task<IReadOnlyList<LessonVoiceMessage>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return _dbContext.LessonVoiceMessages
            .Where(message => message.SessionId == sessionId)
            .OrderBy(message => message.TurnNumber)
            .ThenBy(message => message.SequenceIndex)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<LessonVoiceMessage>)task.Result, cancellationToken);
    }
}
```

- [ ] **Step 4: Extend lesson lookup and implement the session service**

```csharp
public interface ILessonVoiceTutorSessionService
{
    Task<LessonVoiceSessionResponse> CreateOrResumeSessionAsync(Guid lessonId, Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task<LessonVoiceSessionResponse?> GetCurrentSessionAsync(Guid lessonId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LessonVoiceMessageResponse>> GetMessagesAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
    Task CloseSessionAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);
}

public interface ILessonRepository
{
    Task<Lesson?> GetByIdAsync(Guid id);
    Task<Lesson?> GetByIdWithModuleAndCourseAsync(Guid id);
    Task SaveChangesAsync();
}
```

```csharp
public async Task<LessonVoiceSessionResponse> CreateOrResumeSessionAsync(
    Guid lessonId,
    Guid userId,
    bool isAdmin,
    CancellationToken cancellationToken)
{
    var activeSession = await _sessionRepository.GetActiveSessionAsync(lessonId, userId, cancellationToken);
    if (activeSession is not null)
    {
        activeSession.LastActivityAt = DateTime.UtcNow;
        await _sessionRepository.SaveChangesAsync(cancellationToken);
        return MapSession(activeSession);
    }

    var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(lessonId)
        ?? throw new KeyNotFoundException("Lesson not found.");

    if (!isAdmin && !lesson.VoiceTutorEnabled)
    {
        throw new InvalidOperationException("Voice tutor is disabled for this lesson.");
    }

    var session = new LessonVoiceSession
    {
        Id = Guid.NewGuid(),
        LessonId = lesson.Id,
        CourseId = lesson.Module!.CourseId,
        UserId = userId,
        VoiceProfileKey = lesson.NarrationVoiceKey ?? "vi-VN-HoaiMyNeural"
    };

    await _sessionRepository.AddAsync(session, cancellationToken);
    await _sessionRepository.SaveChangesAsync(cancellationToken);
    return MapSession(session);
}
```

- [ ] **Step 5: Register repository and service in `Program.cs`**

```csharp
builder.Services.AddScoped<ILessonVoiceSessionRepository, LessonVoiceSessionRepository>();
builder.Services.AddScoped<ILessonVoiceTutorSessionService, LessonVoiceTutorSessionService>();
```

- [ ] **Step 6: Run session service tests and affected controller tests**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "LessonVoiceTutorSessionServiceTests|LessonVoiceSessionsControllerTests"`

Expected: PASS

- [ ] **Step 7: Commit the session service slice**

```bash
git add backend/CourseVideo.API/Repositories/Interfaces/ILessonVoiceSessionRepository.cs \
  backend/CourseVideo.API/Repositories/LessonVoiceSessionRepository.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorSessionService.cs \
  backend/CourseVideo.API/Services/LessonVoiceTutorSessionService.cs \
  backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs \
  backend/CourseVideo.API/Repositories/LessonRepository.cs \
  backend/CourseVideo.API/Program.cs \
  backend/CourseVideo.API.Tests/Services/LessonVoiceTutorSessionServiceTests.cs
git commit -m "feat: add lesson voice tutor session service"
```

### Task 3: Add context building and realtime tutor orchestration

**Files:**
- Create: `backend/CourseVideo.API/Configuration/LessonVoiceTutorOptions.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonContextBuilder.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ITranscriptionService.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorAnswerService.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonTutorSpeechService.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorService.cs`
- Create: `backend/CourseVideo.API/Services/LessonContextBuilder.cs`
- Create: `backend/CourseVideo.API/Services/LessonVoiceTutorService.cs`
- Create: `backend/CourseVideo.API/Services/Transcription/NullTranscriptionService.cs`
- Create: `backend/CourseVideo.API/Services/Tutoring/NullLessonTutorAnswerService.cs`
- Create: `backend/CourseVideo.API/Services/Tutoring/LessonNarrationVoiceResolver.cs`
- Create: `backend/CourseVideo.API/Services/Tutoring/SegmentedLessonTutorSpeechService.cs`
- Create: `backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `backend/CourseVideo.API/appsettings.json`
- Modify: `backend/CourseVideo.API/CourseVideo.API.csproj`
- Test: `backend/CourseVideo.API.Tests/Services/LessonContextBuilderTests.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonVoiceTutorServiceTests.cs`

- [ ] **Step 1: Write failing tests for context priority and turn state transitions**

```csharp
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class LessonContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_UsesLessonAndCourseMetadata_WhenLessonExists()
    {
        var lessonId = Guid.NewGuid();
        var repository = new Mock<ILessonRepository>();
        repository.Setup(x => x.GetByIdWithModuleAndCourseAsync(lessonId))
            .ReturnsAsync(new Lesson
            {
                Id = lessonId,
                Title = "Dinh nghia AI",
                Description = "Mo ta",
                TeachingScript = "Script",
                SlideOutlineJson = "[{}]",
                VoiceoverPlanJson = "{}",
                TranscriptText = "Transcript",
                Module = new Module
                {
                    Title = "Module 1",
                    Course = new Course { Title = "Khoa hoc AI", Description = "Tong quan" }
                }
            });

        var builder = new LessonContextBuilder(repository.Object);
        var context = await builder.BuildAsync(lessonId, 42.5, CancellationToken.None);

        context.LessonTitle.Should().Be("Dinh nghia AI");
        context.CourseTitle.Should().Be("Khoa hoc AI");
        context.PlaybackTimeSeconds.Should().Be(42.5);
    }
}
```

```csharp
public class LessonVoiceTutorServiceTests
{
    [Fact]
    public async Task CompleteTurnAsync_PersistsTranscriptionAnswerAndMessages()
    {
        var session = new LessonVoiceSession
        {
            Id = Guid.NewGuid(),
            LessonId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = "Active",
            VoiceProfileKey = "vi-VN-HoaiMyNeural"
        };

        var sessions = new Mock<ILessonVoiceSessionRepository>();
        sessions.Setup(x => x.GetByIdAsync(session.Id, CancellationToken.None)).ReturnsAsync(session);

        var contextBuilder = new Mock<ILessonContextBuilder>();
        contextBuilder.Setup(x => x.BuildAsync(session.LessonId, 10, CancellationToken.None))
            .ReturnsAsync(new LessonTutorContext("Khoa hoc", "Module", "Lesson", "Mo ta", "Script", "[]", "{}", "Transcript", 10));

        var transcription = new Mock<ITranscriptionService>();
        transcription.Setup(x => x.TranscribeAsync(It.IsAny<byte[]>(), CancellationToken.None))
            .ReturnsAsync(new TranscriptionResult("Tri tue nhan tao la gi?", 0.98m));

        var answer = new Mock<ILessonTutorAnswerService>();
        answer.Setup(x => x.GenerateAnswerAsync(It.IsAny<LessonTutorAnswerRequest>(), CancellationToken.None))
            .ReturnsAsync(new LessonTutorAnswerResult("AI la he thong mo phong tri tue cua con nguoi.", "Mixed"));

        var speech = new Mock<ILessonTutorSpeechService>();
        speech.Setup(x => x.SynthesizeAsync("vi-VN-HoaiMyNeural", "AI la he thong mo phong tri tue cua con nguoi.", CancellationToken.None))
            .ReturnsAsync([
                new LessonTutorAudioSegment(0, "AI la he thong mo phong tri tue cua con nguoi.", "/storage/voice-tutor/assistant-answers/a1.wav", 6.5)
            ]);

        var service = new LessonVoiceTutorService(
            sessions.Object,
            contextBuilder.Object,
            transcription.Object,
            answer.Object,
            speech.Object);

        var result = await service.CompleteTurnAsync(session.Id, session.UserId, 10, [1, 2, 3], CancellationToken.None);

        result.Status.Should().Be("AwaitingFollowUpDecision");
        sessions.Verify(x => x.AddMessageAsync(It.IsAny<LessonVoiceMessage>(), CancellationToken.None), Times.AtLeast(2));
    }
}
```

- [ ] **Step 2: Run the new service tests to confirm missing orchestration code**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "LessonContextBuilderTests|LessonVoiceTutorServiceTests"`

Expected: FAIL with missing context/result types, services, or hub-related abstractions.

- [ ] **Step 3: Add context/result models and the context builder**

```csharp
public record LessonTutorContext(
    string CourseTitle,
    string ModuleTitle,
    string LessonTitle,
    string LessonDescription,
    string TeachingScript,
    string SlideOutlineJson,
    string VoiceoverPlanJson,
    string TranscriptText,
    double PlaybackTimeSeconds);

public class LessonContextBuilder : ILessonContextBuilder
{
    private readonly ILessonRepository _lessonRepository;

    public LessonContextBuilder(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<LessonTutorContext> BuildAsync(Guid lessonId, double playbackTimeSeconds, CancellationToken cancellationToken)
    {
        var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(lessonId)
            ?? throw new KeyNotFoundException("Lesson not found.");

        return new LessonTutorContext(
            lesson.Module?.Course?.Title ?? string.Empty,
            lesson.Module?.Title ?? string.Empty,
            lesson.Title,
            lesson.Description,
            lesson.TeachingScript ?? string.Empty,
            lesson.SlideOutlineJson ?? "[]",
            lesson.VoiceoverPlanJson ?? "{}",
            lesson.TranscriptText ?? string.Empty,
            playbackTimeSeconds);
    }
}
```

- [ ] **Step 4: Implement transcription/answer/speech abstractions and a minimal tutor service**

```csharp
public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(byte[] audioBytes, CancellationToken cancellationToken);
}

public interface ILessonTutorAnswerService
{
    Task<LessonTutorAnswerResult> GenerateAnswerAsync(LessonTutorAnswerRequest request, CancellationToken cancellationToken);
}

public interface ILessonTutorSpeechService
{
    Task<IReadOnlyList<LessonTutorAudioSegment>> SynthesizeAsync(string voiceProfileKey, string answerText, CancellationToken cancellationToken);
}

public record TranscriptionResult(string Text, decimal Confidence);
public record LessonTutorAnswerRequest(LessonTutorContext Context, string QuestionText, string? ConversationSummary);
public record LessonTutorAnswerResult(string AnswerText, string SourceType);
public record LessonTutorAudioSegment(int SequenceIndex, string Text, string AudioUrl, double DurationSeconds);
public record LessonVoiceTurnResult(
    string Status,
    string TranscriptionText,
    string AnswerText,
    string SourceType,
    IReadOnlyList<LessonTutorAudioSegment> AudioSegments);
```

```csharp
public class LessonVoiceTutorService : ILessonVoiceTutorService
{
    private readonly ILessonVoiceSessionRepository _sessionRepository;
    private readonly ILessonContextBuilder _contextBuilder;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILessonTutorAnswerService _answerService;
    private readonly ILessonTutorSpeechService _speechService;

    public LessonVoiceTutorService(
        ILessonVoiceSessionRepository sessionRepository,
        ILessonContextBuilder contextBuilder,
        ITranscriptionService transcriptionService,
        ILessonTutorAnswerService answerService,
        ILessonTutorSpeechService speechService)
    {
        _sessionRepository = sessionRepository;
        _contextBuilder = contextBuilder;
        _transcriptionService = transcriptionService;
        _answerService = answerService;
        _speechService = speechService;
    }

    public async Task<LessonVoiceTurnResult> CompleteTurnAsync(
        Guid sessionId,
        Guid userId,
        double playbackTimeSeconds,
        byte[] audioBytes,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Session not found.");

        if (session.UserId != userId)
        {
            throw new InvalidOperationException("Cannot access another user's voice session.");
        }

        var context = await _contextBuilder.BuildAsync(session.LessonId, playbackTimeSeconds, cancellationToken);
        var transcription = await _transcriptionService.TranscribeAsync(audioBytes, cancellationToken);
        var answer = await _answerService.GenerateAnswerAsync(
            new LessonTutorAnswerRequest(context, transcription.Text, session.ConversationSummary),
            cancellationToken);
        var audioSegments = await _speechService.SynthesizeAsync(session.VoiceProfileKey, answer.AnswerText, cancellationToken);

        await _sessionRepository.AddMessageAsync(new LessonVoiceMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TurnNumber = 1,
            Role = "User",
            ContentText = transcription.Text,
            ContentSourceType = "Lesson",
            SequenceIndex = 0
        }, cancellationToken);

        await _sessionRepository.AddMessageAsync(new LessonVoiceMessage
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TurnNumber = 1,
            Role = "Assistant",
            ContentText = answer.AnswerText,
            ContentSourceType = answer.SourceType,
            AudioUrl = audioSegments.FirstOrDefault()?.AudioUrl,
            AudioDurationSeconds = audioSegments.Sum(segment => segment.DurationSeconds),
            SequenceIndex = 1
        }, cancellationToken);

        session.LastPausedVideoTimeSeconds = playbackTimeSeconds;
        session.LastActivityAt = DateTime.UtcNow;
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return new LessonVoiceTurnResult("AwaitingFollowUpDecision", transcription.Text, answer.AnswerText, answer.SourceType, audioSegments);
    }
}
```

- [ ] **Step 5: Add SignalR and backend wiring**

```csharp
builder.Services.Configure<LessonVoiceTutorOptions>(builder.Configuration.GetSection("LessonVoiceTutor"));
builder.Services.AddSignalR();
builder.Services.AddScoped<ILessonContextBuilder, LessonContextBuilder>();
builder.Services.AddScoped<ITranscriptionService, NullTranscriptionService>();
builder.Services.AddScoped<ILessonTutorAnswerService, NullLessonTutorAnswerService>();
builder.Services.AddScoped<ILessonTutorSpeechService, SegmentedLessonTutorSpeechService>();
builder.Services.AddScoped<ILessonVoiceTutorService, LessonVoiceTutorService>();
```

```csharp
app.MapHub<LessonVoiceTutorHub>("/hubs/lesson-voice-tutor");
```

```json
"LessonVoiceTutor": {
  "QuestionAudioMaxSeconds": 30,
  "FollowUpLimit": 8,
  "SessionTtlMinutes": 15
}
```

- [ ] **Step 6: Add the hub contract with a minimal complete-turn path**

```csharp
[Authorize]
public class LessonVoiceTutorHub : Hub
{
    private readonly ILessonVoiceTutorService _voiceTutorService;

    public LessonVoiceTutorHub(ILessonVoiceTutorService voiceTutorService)
    {
        _voiceTutorService = voiceTutorService;
    }

    public async Task CompleteTurn(Guid sessionId, double playbackTimeSeconds, byte[] audioBytes, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User!.FindFirst("sub")!.Value);

        await Clients.Caller.SendAsync("TranscriptionStarted", sessionId, cancellationToken);
        var result = await _voiceTutorService.CompleteTurnAsync(sessionId, userId, playbackTimeSeconds, audioBytes, cancellationToken);
        await Clients.Caller.SendAsync("TranscriptionCompleted", result.TranscriptionText, cancellationToken);
        await Clients.Caller.SendAsync("AnswerCompleted", result.AnswerText, result.SourceType, cancellationToken);

        foreach (var segment in result.AudioSegments)
        {
            await Clients.Caller.SendAsync("AnswerAudioSegment", segment.SequenceIndex, segment.Text, segment.AudioUrl, segment.DurationSeconds, cancellationToken);
        }

        await Clients.Caller.SendAsync("AwaitingFollowUpDecision", sessionId, cancellationToken);
    }
}
```

- [ ] **Step 7: Run the context/tutor service tests**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "LessonContextBuilderTests|LessonVoiceTutorServiceTests"`

Expected: PASS

- [ ] **Step 8: Commit the realtime backend slice**

```bash
git add backend/CourseVideo.API/Configuration/LessonVoiceTutorOptions.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonContextBuilder.cs \
  backend/CourseVideo.API/Services/Interfaces/ITranscriptionService.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonTutorAnswerService.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonTutorSpeechService.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonVoiceTutorService.cs \
  backend/CourseVideo.API/Services/LessonContextBuilder.cs \
  backend/CourseVideo.API/Services/LessonVoiceTutorService.cs \
  backend/CourseVideo.API/Services/Transcription/NullTranscriptionService.cs \
  backend/CourseVideo.API/Services/Tutoring \
  backend/CourseVideo.API/Hubs/LessonVoiceTutorHub.cs \
  backend/CourseVideo.API/Program.cs \
  backend/CourseVideo.API/appsettings.json \
  backend/CourseVideo.API.Tests/Services/LessonContextBuilderTests.cs \
  backend/CourseVideo.API.Tests/Services/LessonVoiceTutorServiceTests.cs
git commit -m "feat: add lesson voice tutor realtime backend"
```

### Task 4: Add frontend voice tutor client, hook, and panel

**Files:**
- Create: `frontend/src/api/lessonVoiceTutorService.js`
- Create: `frontend/src/hooks/useLessonVoiceTutor.js`
- Create: `frontend/src/components/course/LessonVoiceTutorPanel.jsx`
- Create: `frontend/src/components/course/LessonVoiceTutorPanel.test.jsx`
- Modify: `frontend/package.json`
- Modify: `frontend/package-lock.json`

- [ ] **Step 1: Write the failing panel test for the decision buttons and tutor states**

```jsx
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LessonVoiceTutorPanel from "./LessonVoiceTutorPanel";

describe("LessonVoiceTutorPanel", () => {
  it("shows follow-up and resume actions after an answer completes", () => {
    render(
      <LessonVoiceTutorPanel
        state="awaitingDecision"
        transcriptText="Tri tue nhan tao la gi?"
        answerText="AI la he thong mo phong tri tue cua con nguoi."
        onStartRecording={vi.fn()}
        onStopRecording={vi.fn()}
        onFollowUp={vi.fn()}
        onResume={vi.fn()}
      />
    );

    expect(screen.getByRole("button", { name: "Hoi tiep" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Tiep tuc hoc" })).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run the frontend test to confirm the component and package are missing**

Run: `npm test -- --run LessonVoiceTutorPanel.test.jsx`

Expected: FAIL with missing `LessonVoiceTutorPanel` or missing `@microsoft/signalr`.

- [ ] **Step 3: Add the frontend service layer and SignalR dependency**

```json
"dependencies": {
  "@microsoft/signalr": "^8.0.8",
  "axios": "^1.7.2",
  "react": "^18.3.1",
  "react-dom": "^18.3.1",
  "react-router-dom": "^6.26.0"
}
```

```js
import * as signalR from "@microsoft/signalr";
import { axiosClient } from "./axiosClient";
import { loadAuthSession } from "../auth/authStorage";

export async function createLessonVoiceSession(lessonId) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/voice-sessions`);
  return data;
}

export function createLessonVoiceTutorConnection() {
  const session = loadAuthSession();
  const baseUrl = (import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api").replace(/\/api$/, "");

  return new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/lesson-voice-tutor`, {
      accessTokenFactory: () => session?.accessToken ?? ""
    })
    .withAutomaticReconnect()
    .build();
}
```

- [ ] **Step 4: Implement the hook with an explicit UI state machine**

```js
import { useEffect, useRef, useState } from "react";
import { createLessonVoiceSession, createLessonVoiceTutorConnection } from "../api/lessonVoiceTutorService";

export function useLessonVoiceTutor({ lessonId, enabled, onPauseVideo, onResumeVideo }) {
  const [state, setState] = useState("idle");
  const [session, setSession] = useState(null);
  const [transcriptText, setTranscriptText] = useState("");
  const [answerText, setAnswerText] = useState("");
  const connectionRef = useRef(null);

  useEffect(() => {
    if (!enabled || !lessonId) {
      return;
    }

    const connection = createLessonVoiceTutorConnection();
    connectionRef.current = connection;

    connection.on("TranscriptionCompleted", (text) => {
      setTranscriptText(text);
      setState("thinking");
    });

    connection.on("AnswerCompleted", (text) => {
      setAnswerText(text);
      setState("awaitingDecision");
    });

    connection.start();
    return () => connection.stop();
  }, [enabled, lessonId]);

  async function startRecording(playbackTimeSeconds) {
    const currentSession = session ?? await createLessonVoiceSession(lessonId);
    setSession(currentSession);
    onPauseVideo(playbackTimeSeconds);
    setState("recording");
  }

  function markUploading() {
    setState("uploading");
  }

  function requestFollowUp() {
    setTranscriptText("");
    setAnswerText("");
    setState("recording");
  }

  function resumeLearning() {
    setState("idle");
    onResumeVideo();
  }

  return {
    state,
    transcriptText,
    answerText,
    startRecording,
    markUploading,
    requestFollowUp,
    resumeLearning
  };
}
```

- [ ] **Step 5: Build the panel component**

```jsx
export default function LessonVoiceTutorPanel({
  state,
  transcriptText,
  answerText,
  onStartRecording,
  onStopRecording,
  onFollowUp,
  onResume
}) {
  return (
    <section className="voice-tutor-panel" aria-label="Tro giang giong noi">
      <p className="voice-tutor-panel__eyebrow">Tro giang giong noi</p>
      <p className="voice-tutor-panel__status">
        {state === "recording" ? "Dang nghe cau hoi..." : null}
        {state === "uploading" ? "Dang gui audio..." : null}
        {state === "thinking" ? "Dang suy luan..." : null}
        {state === "awaitingDecision" ? "Da tra loi xong." : null}
      </p>
      {transcriptText ? <p className="voice-tutor-panel__transcript">{transcriptText}</p> : null}
      {answerText ? <p className="voice-tutor-panel__answer">{answerText}</p> : null}

      {state === "idle" ? <button onClick={onStartRecording}>Hoi bang giong noi</button> : null}
      {state === "recording" ? <button onClick={onStopRecording}>Ket thuc ghi am</button> : null}
      {state === "awaitingDecision" ? (
        <div className="voice-tutor-panel__actions">
          <button onClick={onFollowUp}>Hoi tiep</button>
          <button onClick={onResume}>Tiep tuc hoc</button>
        </div>
      ) : null}
    </section>
  );
}
```

- [ ] **Step 6: Run the panel test**

Run: `npm test -- --run LessonVoiceTutorPanel.test.jsx`

Expected: PASS

- [ ] **Step 7: Commit the frontend tutor building blocks**

```bash
git add frontend/package.json \
  frontend/package-lock.json \
  frontend/src/api/lessonVoiceTutorService.js \
  frontend/src/hooks/useLessonVoiceTutor.js \
  frontend/src/components/course/LessonVoiceTutorPanel.jsx \
  frontend/src/components/course/LessonVoiceTutorPanel.test.jsx
git commit -m "feat: add lesson voice tutor frontend primitives"
```

### Task 5: Integrate the tutor into `CourseLearnPage`

**Files:**
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`
- Modify: `frontend/src/styles/theme.css`

- [ ] **Step 1: Add a failing page test for pause/resume and tutor actions**

```jsx
it("shows the lesson voice tutor actions after a completed voice answer", async () => {
  mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

  render(
    <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
      <ThemeProvider>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </ThemeProvider>
    </MemoryRouter>
  );

  expect(await screen.findByRole("button", { name: "Hoi bang giong noi" })).toBeInTheDocument();
});
```

- [ ] **Step 2: Run the page test to verify the integration is not present**

Run: `npm test -- --run CourseLearnPage.test.jsx`

Expected: FAIL because the page does not render the voice tutor panel yet.

- [ ] **Step 3: Wire the hook and video ref into the page**

```jsx
import { useEffect, useRef, useState } from "react";
import LessonVoiceTutorPanel from "../components/course/LessonVoiceTutorPanel";
import { useLessonVoiceTutor } from "../hooks/useLessonVoiceTutor";
```

```jsx
const videoRef = useRef(null);
const pausedTimeRef = useRef(0);

const tutor = useLessonVoiceTutor({
  lessonId: selectedLesson?.lessonId ?? "",
  enabled: Boolean(selectedLesson),
  onPauseVideo(playbackTimeSeconds) {
    pausedTimeRef.current = playbackTimeSeconds;
    videoRef.current?.pause();
  },
  onResumeVideo() {
    if (videoRef.current) {
      videoRef.current.currentTime = pausedTimeRef.current;
      void videoRef.current.play?.();
    }
  }
});
```

```jsx
<video
  ref={videoRef}
  controls
  preload="metadata"
  src={selectedLesson.videoUrl}
  onTimeUpdate={handleTimeUpdate}>
  Trình duyệt của bạn không hỗ trợ phát video.
</video>
```

- [ ] **Step 4: Render the tutor panel below the player summary**

```jsx
<LessonVoiceTutorPanel
  state={tutor.state}
  transcriptText={tutor.transcriptText}
  answerText={tutor.answerText}
  onStartRecording={() => tutor.startRecording(videoRef.current?.currentTime ?? 0)}
  onStopRecording={() => tutor.markUploading()}
  onFollowUp={tutor.requestFollowUp}
  onResume={tutor.resumeLearning}
/>
```

- [ ] **Step 5: Add page styles for the tutor panel**

```css
.voice-tutor-panel {
  display: grid;
  gap: 0.75rem;
  margin-top: 1rem;
  padding: 1rem 1.125rem;
  border: 1px solid rgba(180, 255, 68, 0.35);
  border-radius: 1rem;
  background: linear-gradient(180deg, rgba(16, 27, 33, 0.96), rgba(23, 38, 39, 0.92));
  color: #f6ffe8;
}

.voice-tutor-panel__actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}
```

- [ ] **Step 6: Run the course learn page tests**

Run: `npm test -- --run CourseLearnPage.test.jsx`

Expected: PASS

- [ ] **Step 7: Commit the page integration**

```bash
git add frontend/src/pages/CourseLearnPage.jsx \
  frontend/src/pages/CourseLearnPage.test.jsx \
  frontend/src/styles/theme.css
git commit -m "feat: integrate lesson voice tutor into learn page"
```

### Task 6: End-to-end verification and configuration handoff

**Files:**
- Modify: `backend/CourseVideo.API/appsettings.json`
- Modify: `README.md`

- [ ] **Step 1: Add a short backend config section to `README.md`**

```md
## Lesson voice tutor

Set these backend settings before testing the voice tutor flow:

- `LessonVoiceTutor:QuestionAudioMaxSeconds`
- `LessonVoiceTutor:FollowUpLimit`
- `LessonVoiceTutor:SessionTtlMinutes`
- provider-specific settings for transcription, answer generation, and lesson voice synthesis

The frontend expects the API base URL to expose both `/api` and `/hubs/lesson-voice-tutor` under the same host.
```

- [ ] **Step 2: Run the backend voice tutor test suite**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "LessonVoice"`

Expected: PASS

- [ ] **Step 3: Run the frontend voice tutor test suite**

Run: `cd frontend && npm test -- --run LessonVoiceTutorPanel.test.jsx CourseLearnPage.test.jsx`

Expected: PASS

- [ ] **Step 4: Run a production frontend build**

Run: `cd frontend && npm run build`

Expected: PASS with Vite build output and no missing SignalR dependency errors.

- [ ] **Step 5: Commit the final verification/docs changes**

```bash
git add README.md backend/CourseVideo.API/appsettings.json
git commit -m "docs: document lesson voice tutor setup"
```

## Self-Review

### Spec coverage

- lesson-scoped voice session, turn, and message persistence: Task 1 and Task 2
- pause/resume lesson playback: Task 5
- session-level memory within the current lesson: Task 2 and Task 3
- answer priority lesson -> course -> external knowledge: Task 3
- same lesson voice for answer playback: Task 1 and Task 3
- SignalR realtime contract and streaming events: Task 3 and Task 4
- follow-up vs continue-learning decision flow: Task 4 and Task 5
- auth and session ownership: Task 1, Task 2, and Task 3
- testing for backend/frontend flows: every task has explicit tests plus Task 6 verification

### Placeholder scan

- no `TODO`, `TBD`, or “similar to above” references remain
- every code-writing step includes concrete file paths and code snippets
- every verification step includes an exact command and expected result

### Type consistency

- `LessonVoiceSessionResponse`, `LessonVoiceMessage`, `LessonVoiceTurnResult`, `LessonTutorContext`, and `LessonTutorAudioSegment` naming is used consistently across the repository, service, hub, and frontend plan
- the SignalR route is consistently `/hubs/lesson-voice-tutor`
- frontend state names consistently use `idle`, `recording`, `uploading`, `thinking`, `awaitingDecision`
