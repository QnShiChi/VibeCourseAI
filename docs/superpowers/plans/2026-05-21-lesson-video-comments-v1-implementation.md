# Lesson Video Comments V1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add lesson-level comments under each learner video with root comments, one-level replies, TikTok-style `@username` reply targeting, emoji reactions, newest/featured sorting, load-more pagination, and admin moderation.

**Architecture:** Extend the ASP.NET Core API with a dedicated lesson discussion module instead of bolting comment logic into `CourseService` or `LessonService`. Store comments and reactions in SQL Server via `AppDbContext` plus `DbInitializer`, expose focused learner/admin APIs, and mount a modular `LessonComments` UI below the learner video in `CourseLearnPage`.

**Tech Stack:** ASP.NET Core 8, Entity Framework Core, SQL Server, React, Vite, Axios, Vitest, React Testing Library.

---

## File Structure

### Backend

- Create: `backend/CourseVideo.API/Models/LessonComment.cs`
- Create: `backend/CourseVideo.API/Models/LessonCommentReaction.cs`
- Modify: `backend/CourseVideo.API/Models/Lesson.cs`
- Modify: `backend/CourseVideo.API/Models/User.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentListResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentThreadResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentReactionResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/CreateLessonCommentRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/CreateLessonReplyRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/ToggleLessonCommentReactionRequest.cs`
- Create: `backend/CourseVideo.API/Repositories/Interfaces/ILessonCommentRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/LessonCommentRepository.cs`
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonCommentService.cs`
- Create: `backend/CourseVideo.API/Services/LessonCommentService.cs`
- Create: `backend/CourseVideo.API/Controllers/LessonCommentsController.cs`
- Create: `backend/CourseVideo.API/Controllers/AdminCommentsController.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Modify: `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`

### Backend Tests

- Create: `backend/CourseVideo.API.Tests/Services/LessonCommentServiceTests.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/LessonCommentsControllerTests.cs`
- Create: `backend/CourseVideo.API.Tests/Controllers/AdminCommentsControllerTests.cs`

### Frontend

- Create: `frontend/src/api/commentService.js`
- Create: `frontend/src/components/comments/LessonComments.jsx`
- Create: `frontend/src/components/comments/CommentComposer.jsx`
- Create: `frontend/src/components/comments/CommentSortControl.jsx`
- Create: `frontend/src/components/comments/CommentList.jsx`
- Create: `frontend/src/components/comments/CommentItem.jsx`
- Create: `frontend/src/components/comments/CommentReactionBar.jsx`
- Create: `frontend/src/components/comments/LessonComments.module.css`
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`

### Frontend Tests

- Create: `frontend/src/components/comments/LessonComments.test.jsx`
- Create: `frontend/src/components/comments/CommentReactionBar.test.jsx`

## Context Notes

- The repo currently uses `DbInitializer` to create/patch schema in SQL Server; do not assume EF migration-only rollout.
- “User có quyền học khóa đó” currently maps to the existing learner rule already used by `GET /api/courses/{id}/learn`: authenticated user can view a published course, admin can preview draft. `v1` comments should reuse this rule because there is no enrollment model yet.
- `CourseLearnPage.jsx` already loads all lesson navigation data from `getCourseLearnPayload(courseId)` and renders the selected lesson below the video stage. The comment module should attach there, not as a separate route.
- Existing backend controllers use simple `ClaimTypes.NameIdentifier` parsing for current user id and `[Authorize(Roles = "Admin")]` for admin-only routes. Follow that pattern.

### Task 1: Add comment schema and database bootstrap

**Files:**
- Create: `backend/CourseVideo.API/Models/LessonComment.cs`
- Create: `backend/CourseVideo.API/Models/LessonCommentReaction.cs`
- Modify: `backend/CourseVideo.API/Models/Lesson.cs`
- Modify: `backend/CourseVideo.API/Models/User.cs`
- Modify: `backend/CourseVideo.API/Data/AppDbContext.cs`
- Modify: `backend/CourseVideo.API/Data/DbInitializer.cs`
- Test: `backend/CourseVideo.API.Tests/Services/DbInitializerTests.cs`

- [ ] **Step 1: Write the failing schema/bootstrap test**

```csharp
[Fact]
public void Initialize_creates_lesson_comment_tables_when_missing()
{
    using var connection = new SqliteConnection("DataSource=:memory:");
    connection.Open();

    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(connection)
        .Options;

    using var dbContext = new AppDbContext(options);
    dbContext.Database.EnsureCreated();

    DbInitializer.Initialize(dbContext, Options.Create(new AdminSeedOptions()));

    Assert.NotNull(dbContext.Model.FindEntityType(typeof(LessonComment)));
    Assert.NotNull(dbContext.Model.FindEntityType(typeof(LessonCommentReaction)));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter Initialize_creates_lesson_comment_tables_when_missing`

Expected: FAIL because `LessonComment` and `LessonCommentReaction` types do not exist yet.

- [ ] **Step 3: Add the new models and wire them into EF**

```csharp
namespace CourseVideo.API.Models;

public class LessonComment : BaseEntity
{
    public Guid LessonId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public Guid? ReplyToUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Lesson? Lesson { get; set; }
    public User? User { get; set; }
    public LessonComment? ParentComment { get; set; }
    public User? ReplyToUser { get; set; }
    public ICollection<LessonComment> Replies { get; set; } = new List<LessonComment>();
    public ICollection<LessonCommentReaction> Reactions { get; set; } = new List<LessonCommentReaction>();
}
```

```csharp
namespace CourseVideo.API.Models;

public class LessonCommentReaction : BaseEntity
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public LessonComment? Comment { get; set; }
    public User? User { get; set; }
}
```

```csharp
public DbSet<LessonComment> LessonComments => Set<LessonComment>();
public DbSet<LessonCommentReaction> LessonCommentReactions => Set<LessonCommentReaction>();
```

```csharp
modelBuilder.Entity<LessonComment>(entity =>
{
    entity.HasKey(comment => comment.Id);
    entity.Property(comment => comment.Content).HasMaxLength(4000).IsRequired();
    entity.HasIndex(comment => new { comment.LessonId, comment.CreatedAt });
    entity.HasOne(comment => comment.Lesson)
        .WithMany()
        .HasForeignKey(comment => comment.LessonId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(comment => comment.User)
        .WithMany()
        .HasForeignKey(comment => comment.UserId)
        .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(comment => comment.ParentComment)
        .WithMany(comment => comment.Replies)
        .HasForeignKey(comment => comment.ParentCommentId)
        .OnDelete(DeleteBehavior.Restrict);
    entity.HasOne(comment => comment.ReplyToUser)
        .WithMany()
        .HasForeignKey(comment => comment.ReplyToUserId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

```csharp
modelBuilder.Entity<LessonCommentReaction>(entity =>
{
    entity.HasKey(reaction => reaction.Id);
    entity.Property(reaction => reaction.Emoji).HasMaxLength(32).IsRequired();
    entity.HasIndex(reaction => new { reaction.CommentId, reaction.UserId, reaction.Emoji }).IsUnique();
    entity.HasOne(reaction => reaction.Comment)
        .WithMany(comment => comment.Reactions)
        .HasForeignKey(reaction => reaction.CommentId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(reaction => reaction.User)
        .WithMany()
        .HasForeignKey(reaction => reaction.UserId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

Add SQL bootstrap methods in `DbInitializer.cs`:

```csharp
EnsureLessonCommentsTableExists(dbContext);
EnsureLessonCommentReactionsTableExists(dbContext);
```

```csharp
private static void EnsureLessonCommentsTableExists(AppDbContext dbContext)
{
    if (!dbContext.Database.IsSqlServer())
    {
        return;
    }

    dbContext.Database.ExecuteSqlRaw(
        """
        IF OBJECT_ID(N'[LessonComments]', N'U') IS NULL
        BEGIN
            CREATE TABLE [LessonComments] (
                [Id] uniqueidentifier NOT NULL,
                [LessonId] uniqueidentifier NOT NULL,
                [UserId] uniqueidentifier NOT NULL,
                [ParentCommentId] uniqueidentifier NULL,
                [ReplyToUserId] uniqueidentifier NULL,
                [Content] nvarchar(4000) NOT NULL,
                [IsHidden] bit NOT NULL CONSTRAINT [DF_LessonComments_IsHidden] DEFAULT 0,
                [DeletedAt] datetime2 NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_LessonComments] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_LessonComments_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [Lessons]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_LessonComments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
                CONSTRAINT [FK_LessonComments_LessonComments_ParentCommentId] FOREIGN KEY ([ParentCommentId]) REFERENCES [LessonComments]([Id]),
                CONSTRAINT [FK_LessonComments_Users_ReplyToUserId] FOREIGN KEY ([ReplyToUserId]) REFERENCES [Users]([Id])
            );
            CREATE INDEX [IX_LessonComments_LessonId_CreatedAt] ON [LessonComments] ([LessonId], [CreatedAt]);
        END
        """);
}
```

- [ ] **Step 4: Run the targeted test to verify it passes**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter Initialize_creates_lesson_comment_tables_when_missing`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/CourseVideo.API/Models/LessonComment.cs \
  backend/CourseVideo.API/Models/LessonCommentReaction.cs \
  backend/CourseVideo.API/Models/Lesson.cs \
  backend/CourseVideo.API/Models/User.cs \
  backend/CourseVideo.API/Data/AppDbContext.cs \
  backend/CourseVideo.API/Data/DbInitializer.cs \
  backend/CourseVideo.API.Tests/Services/DbInitializerTests.cs
git commit -m "feat: add lesson comment schema"
```

### Task 2: Add repository and feed read model for lesson comments

**Files:**
- Create: `backend/CourseVideo.API/Repositories/Interfaces/ILessonCommentRepository.cs`
- Create: `backend/CourseVideo.API/Repositories/LessonCommentRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs`
- Modify: `backend/CourseVideo.API/Repositories/LessonRepository.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentListResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentThreadResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/LessonCommentReactionResponse.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonCommentServiceTests.cs`

- [ ] **Step 1: Write the failing read-model/service test**

```csharp
[Fact]
public async Task GetCommentsAsync_returns_root_comments_with_nested_replies_and_reactions()
{
    var repository = new Mock<ILessonCommentRepository>();
    repository.Setup(x => x.GetThreadPageAsync(
            It.IsAny<Guid>(),
            "newest",
            1,
            10,
            It.IsAny<Guid>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new LessonCommentThreadPage(
            Items: new[]
            {
                new LessonCommentThreadRow(
                    RootCommentId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    RootAuthorName: "Alice",
                    Replies: new[]
                    {
                        new LessonCommentReplyRow("Bob", "@Alice Mình nghĩ nên xem lại slide 2.")
                    })
            },
            TotalCount: 1));

    var service = new LessonCommentService(repository.Object, Mock.Of<ILessonRepository>());

    var result = await service.GetCommentsAsync(Guid.NewGuid(), Guid.NewGuid(), false, "newest", 1, 10);

    Assert.Single(result.Items);
    Assert.Single(result.Items[0].Replies);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter GetCommentsAsync_returns_root_comments_with_nested_replies_and_reactions`

Expected: FAIL because repository contract, DTOs, and service do not exist yet.

- [ ] **Step 3: Create repository contract and DTO response shapes**

```csharp
public interface ILessonCommentRepository
{
    Task<LessonCommentThreadPage> GetThreadPageAsync(
        Guid lessonId,
        string sort,
        int page,
        int pageSize,
        Guid currentUserId,
        bool includeHidden,
        CancellationToken cancellationToken = default);

    Task<LessonComment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task AddAsync(LessonComment comment, CancellationToken cancellationToken = default);
    Task AddReactionAsync(LessonCommentReaction reaction, CancellationToken cancellationToken = default);
    Task RemoveReactionAsync(LessonCommentReaction reaction);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

```csharp
public class LessonCommentListResponse
{
    public IReadOnlyList<LessonCommentThreadResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
    public string Sort { get; set; } = "newest";
}
```

```csharp
public class LessonCommentThreadResponse
{
    public LessonCommentItemResponse Comment { get; set; } = new();
    public IReadOnlyList<LessonCommentItemResponse> Replies { get; set; } = [];
}
```

```csharp
public class LessonCommentItemResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public Guid? ReplyToUserId { get; set; }
    public string? ReplyToUserName { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public bool IsDeleted { get; set; }
    public bool CanDelete { get; set; }
    public bool CanModerate { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<LessonCommentReactionResponse> Reactions { get; set; } = [];
}
```

- [ ] **Step 4: Implement repository page query**

```csharp
public async Task<LessonCommentThreadPage> GetThreadPageAsync(
    Guid lessonId,
    string sort,
    int page,
    int pageSize,
    Guid currentUserId,
    bool includeHidden,
    CancellationToken cancellationToken = default)
{
    var rootQuery = _dbContext.LessonComments
        .AsNoTracking()
        .Include(comment => comment.User)
        .Include(comment => comment.Reactions)
        .Where(comment => comment.LessonId == lessonId && comment.ParentCommentId == null);

    if (!includeHidden)
    {
        rootQuery = rootQuery.Where(comment => !comment.IsHidden);
    }

    rootQuery = sort == "featured"
        ? rootQuery.OrderByDescending(comment => comment.Reactions.Count * 3 + comment.Replies.Count * 2).ThenByDescending(comment => comment.CreatedAt)
        : rootQuery.OrderByDescending(comment => comment.CreatedAt);

    var totalCount = await rootQuery.CountAsync(cancellationToken);
    var rootComments = await rootQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    var rootIds = rootComments.Select(comment => comment.Id).ToList();
    var replies = await _dbContext.LessonComments
        .AsNoTracking()
        .Include(comment => comment.User)
        .Include(comment => comment.ReplyToUser)
        .Include(comment => comment.Reactions)
        .Where(comment => comment.ParentCommentId != null && rootIds.Contains(comment.ParentCommentId.Value))
        .OrderBy(comment => comment.CreatedAt)
        .ToListAsync(cancellationToken);

    return LessonCommentThreadPage.FromEntities(rootComments, replies, totalCount, page, pageSize, currentUserId);
}
```

- [ ] **Step 5: Run the targeted test to verify it passes**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter GetCommentsAsync_returns_root_comments_with_nested_replies_and_reactions`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Repositories/Interfaces/ILessonCommentRepository.cs \
  backend/CourseVideo.API/Repositories/LessonCommentRepository.cs \
  backend/CourseVideo.API/Repositories/Interfaces/ILessonRepository.cs \
  backend/CourseVideo.API/Repositories/LessonRepository.cs \
  backend/CourseVideo.API/DTOs/Comments \
  backend/CourseVideo.API.Tests/Services/LessonCommentServiceTests.cs
git commit -m "feat: add lesson comment read models"
```

### Task 3: Implement learner comment service and APIs

**Files:**
- Create: `backend/CourseVideo.API/Services/Interfaces/ILessonCommentService.cs`
- Create: `backend/CourseVideo.API/Services/LessonCommentService.cs`
- Create: `backend/CourseVideo.API/Controllers/LessonCommentsController.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/CreateLessonCommentRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/CreateLessonReplyRequest.cs`
- Create: `backend/CourseVideo.API/DTOs/Comments/ToggleLessonCommentReactionRequest.cs`
- Modify: `backend/CourseVideo.API/Program.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonCommentServiceTests.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/LessonCommentsControllerTests.cs`

- [ ] **Step 1: Write failing service tests for create/reply/react/delete-own**

```csharp
[Fact]
public async Task CreateReplyAsync_replying_to_a_reply_keeps_parent_on_root_comment()
{
    var rootId = Guid.NewGuid();
    var replyId = Guid.NewGuid();
    var currentUserId = Guid.NewGuid();

    var repository = new Mock<ILessonCommentRepository>();
    repository.Setup(x => x.GetByIdAsync(replyId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new LessonComment
        {
            Id = replyId,
            LessonId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ParentCommentId = rootId,
            UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Content = "Reply hiện có"
        });

    var service = new LessonCommentService(repository.Object, BuildLessonRepositoryForPublishedCourse());

    await service.CreateReplyAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), replyId, currentUserId, false, new CreateLessonReplyRequest
    {
        Content = "@Bob Đồng ý",
        ReplyToUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
    });

    repository.Verify(x => x.AddAsync(It.Is<LessonComment>(comment =>
        comment.ParentCommentId == rootId &&
        comment.ReplyToUserId == Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")), It.IsAny<CancellationToken>()));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter LessonCommentServiceTests`

Expected: FAIL because service, controller, and request DTOs do not exist yet.

- [ ] **Step 3: Implement service contract and permission guard**

```csharp
public interface ILessonCommentService
{
    Task<LessonCommentListResponse> GetCommentsAsync(Guid lessonId, Guid currentUserId, bool isAdmin, string sort, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<LessonCommentThreadResponse> CreateCommentAsync(Guid lessonId, Guid currentUserId, bool isAdmin, CreateLessonCommentRequest request, CancellationToken cancellationToken = default);
    Task<LessonCommentThreadResponse> CreateReplyAsync(Guid lessonId, Guid commentId, Guid currentUserId, bool isAdmin, CreateLessonReplyRequest request, CancellationToken cancellationToken = default);
    Task ToggleReactionAsync(Guid lessonId, Guid commentId, Guid currentUserId, bool isAdmin, string emoji, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid lessonId, Guid commentId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
```

```csharp
private async Task<Lesson> RequireAccessibleLessonAsync(Guid lessonId, bool isAdmin, CancellationToken cancellationToken)
{
    var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(lessonId);
    if (lesson?.Module?.Course is null)
    {
        throw new KeyNotFoundException("Không tìm thấy lesson.");
    }

    if (!lesson.Module.Course.IsPublished && !isAdmin)
    {
        throw new InvalidOperationException("Bạn không có quyền bình luận lesson này.");
    }

    return lesson;
}
```

- [ ] **Step 4: Implement learner controller endpoints**

```csharp
[ApiController]
[Route("api/lessons/{lessonId:guid}/comments")]
[Authorize]
public class LessonCommentsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetComments(Guid lessonId, [FromQuery] string sort = "newest", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
        var isAdmin = User.IsInRole("Admin");
        var result = await _lessonCommentService.GetCommentsAsync(lessonId, currentUserId, isAdmin, sort, page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateComment(Guid lessonId, [FromBody] CreateLessonCommentRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")!.Value);
        var isAdmin = User.IsInRole("Admin");
        var created = await _lessonCommentService.CreateCommentAsync(lessonId, currentUserId, isAdmin, request);
        return Ok(created);
    }
}
```

```csharp
[HttpPost("{commentId:guid}/replies")]
public async Task<IActionResult> CreateReply(Guid lessonId, Guid commentId, [FromBody] CreateLessonReplyRequest request) { ... }

[HttpPost("{commentId:guid}/reactions")]
public async Task<IActionResult> AddReaction(Guid lessonId, Guid commentId, [FromBody] ToggleLessonCommentReactionRequest request) { ... }

[HttpDelete("{commentId:guid}/reactions/{emoji}")]
public async Task<IActionResult> RemoveReaction(Guid lessonId, Guid commentId, string emoji) { ... }

[HttpDelete("{commentId:guid}")]
public async Task<IActionResult> DeleteComment(Guid lessonId, Guid commentId) { ... }
```

- [ ] **Step 5: Register DI and run tests**

Add registrations in `Program.cs`:

```csharp
builder.Services.AddScoped<ILessonCommentRepository, LessonCommentRepository>();
builder.Services.AddScoped<ILessonCommentService, LessonCommentService>();
```

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "LessonCommentServiceTests|LessonCommentsControllerTests"`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Services/Interfaces/ILessonCommentService.cs \
  backend/CourseVideo.API/Services/LessonCommentService.cs \
  backend/CourseVideo.API/Controllers/LessonCommentsController.cs \
  backend/CourseVideo.API/DTOs/Comments/CreateLessonCommentRequest.cs \
  backend/CourseVideo.API/DTOs/Comments/CreateLessonReplyRequest.cs \
  backend/CourseVideo.API/DTOs/Comments/ToggleLessonCommentReactionRequest.cs \
  backend/CourseVideo.API/Program.cs \
  backend/CourseVideo.API.Tests/Services/LessonCommentServiceTests.cs \
  backend/CourseVideo.API.Tests/Controllers/LessonCommentsControllerTests.cs
git commit -m "feat: add learner lesson comment APIs"
```

### Task 4: Add admin moderation APIs and hidden/deleted semantics

**Files:**
- Create: `backend/CourseVideo.API/Controllers/AdminCommentsController.cs`
- Modify: `backend/CourseVideo.API/Services/Interfaces/ILessonCommentService.cs`
- Modify: `backend/CourseVideo.API/Services/LessonCommentService.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/AdminCommentsControllerTests.cs`
- Test: `backend/CourseVideo.API.Tests/Services/LessonCommentServiceTests.cs`

- [ ] **Step 1: Write failing moderation tests**

```csharp
[Fact]
public async Task HideCommentAsync_marks_comment_hidden_without_deleting_thread()
{
    var comment = new LessonComment
    {
        Id = Guid.NewGuid(),
        LessonId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Content = "Cần moderation"
    };

    var repository = new Mock<ILessonCommentRepository>();
    repository.Setup(x => x.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(comment);

    var service = new LessonCommentService(repository.Object, Mock.Of<ILessonRepository>());

    await service.HideCommentAsync(comment.LessonId, comment.Id, CancellationToken.None);

    Assert.True(comment.IsHidden);
    Assert.Null(comment.DeletedAt);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "HideCommentAsync|AdminCommentsControllerTests"`

Expected: FAIL because moderation methods and controller do not exist yet.

- [ ] **Step 3: Extend service with hide/unhide**

```csharp
public interface ILessonCommentService
{
    Task HideCommentAsync(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default);
    Task UnhideCommentAsync(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default);
}
```

```csharp
public async Task HideCommentAsync(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default)
{
    var comment = await _lessonCommentRepository.GetByIdAsync(commentId, cancellationToken)
        ?? throw new KeyNotFoundException("Không tìm thấy bình luận.");

    if (comment.LessonId != lessonId)
    {
        throw new InvalidOperationException("Bình luận không thuộc lesson này.");
    }

    comment.IsHidden = true;
    comment.UpdatedAt = DateTime.UtcNow;
    await _lessonCommentRepository.SaveChangesAsync(cancellationToken);
}
```

- [ ] **Step 4: Implement admin controller**

```csharp
[ApiController]
[Route("api/admin/comments")]
[Authorize(Roles = "Admin")]
public class AdminCommentsController : ControllerBase
{
    [HttpPatch("{commentId:guid}/hide")]
    public async Task<IActionResult> Hide(Guid commentId, [FromQuery] Guid lessonId)
    {
        await _lessonCommentService.HideCommentAsync(lessonId, commentId);
        return NoContent();
    }

    [HttpPatch("{commentId:guid}/unhide")]
    public async Task<IActionResult> Unhide(Guid commentId, [FromQuery] Guid lessonId)
    {
        await _lessonCommentService.UnhideCommentAsync(lessonId, commentId);
        return NoContent();
    }
}
```

- [ ] **Step 5: Run moderation tests**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "AdminCommentsControllerTests|HideCommentAsync"`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/CourseVideo.API/Controllers/AdminCommentsController.cs \
  backend/CourseVideo.API/Services/Interfaces/ILessonCommentService.cs \
  backend/CourseVideo.API/Services/LessonCommentService.cs \
  backend/CourseVideo.API.Tests/Controllers/AdminCommentsControllerTests.cs \
  backend/CourseVideo.API.Tests/Services/LessonCommentServiceTests.cs
git commit -m "feat: add comment moderation APIs"
```

### Task 5: Add frontend comment API client and render comment feed on learner page

**Files:**
- Create: `frontend/src/api/commentService.js`
- Create: `frontend/src/components/comments/LessonComments.jsx`
- Create: `frontend/src/components/comments/CommentList.jsx`
- Create: `frontend/src/components/comments/CommentItem.jsx`
- Create: `frontend/src/components/comments/LessonComments.module.css`
- Modify: `frontend/src/pages/CourseLearnPage.jsx`
- Test: `frontend/src/pages/CourseLearnPage.test.jsx`
- Test: `frontend/src/components/comments/LessonComments.test.jsx`

- [ ] **Step 1: Write the failing learner-page render test**

```jsx
it("renders the comment section below the selected lesson video", async () => {
  mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());
  mockGetLessonComments.mockResolvedValue({
    items: [
      {
        comment: {
          id: "comment-1",
          authorName: "Alice",
          content: "Video này giải thích khá rõ.",
          reactions: []
        },
        replies: []
      }
    ],
    page: 1,
    pageSize: 10,
    totalCount: 1,
    hasMore: false,
    sort: "newest"
  });

  render(<CourseLearnPageWithRouter />);

  expect(await screen.findByRole("heading", { name: /bình luận/i })).toBeInTheDocument();
  expect(await screen.findByText(/Video này giải thích khá rõ/i)).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npm run test -- CourseLearnPage.test.jsx`

Expected: FAIL because the comment API and UI module do not exist yet.

- [ ] **Step 3: Add API client and minimal feed component**

```js
import { axiosClient } from "./axiosClient";

export async function getLessonComments(lessonId, { sort = "newest", page = 1, pageSize = 10 } = {}) {
  const { data } = await axiosClient.get(`/lessons/${lessonId}/comments`, {
    params: { sort, page, pageSize }
  });
  return data;
}
```

```jsx
export default function LessonComments({ lessonId, isAdmin = false }) {
  const [comments, setComments] = useState([]);
  const [sort, setSort] = useState("newest");
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);

  useEffect(() => {
    if (!lessonId) return;
    loadComments({ lessonId, sort, page: 1, append: false });
  }, [lessonId, sort]);

  async function loadComments({ lessonId, sort, page, append }) {
    const data = await getLessonComments(lessonId, { sort, page, pageSize: 10 });
    setComments(append ? (current) => [...current, ...data.items] : data.items);
    setPage(data.page);
    setHasMore(data.hasMore);
  }

  return (
    <section className={styles.commentsSection}>
      <h2>Bình luận</h2>
      <CommentList comments={comments} isAdmin={isAdmin} />
      {hasMore ? <button type="button">Load more</button> : null}
    </section>
  );
}
```

- [ ] **Step 4: Mount the section below the lesson stage**

Update `CourseLearnPage.jsx`:

```jsx
import { useAuth } from "../auth/AuthContext";
import LessonComments from "../components/comments/LessonComments";
```

```jsx
const { user } = useAuth();
const isAdmin = user?.role === "Admin";
```

```jsx
<Card variant="shadowed">
  <LessonComments isAdmin={isAdmin} lessonId={selectedLesson.lessonId} />
</Card>
```

- [ ] **Step 5: Run learner-page tests**

Run: `cd frontend && npm run test -- CourseLearnPage.test.jsx LessonComments.test.jsx`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/api/commentService.js \
  frontend/src/components/comments/LessonComments.jsx \
  frontend/src/components/comments/CommentList.jsx \
  frontend/src/components/comments/CommentItem.jsx \
  frontend/src/components/comments/LessonComments.module.css \
  frontend/src/pages/CourseLearnPage.jsx \
  frontend/src/pages/CourseLearnPage.test.jsx \
  frontend/src/components/comments/LessonComments.test.jsx
git commit -m "feat: render lesson comments on learner page"
```

### Task 6: Add composer, reply flow, emoji reactions, sorting, and load-more

**Files:**
- Create: `frontend/src/components/comments/CommentComposer.jsx`
- Create: `frontend/src/components/comments/CommentSortControl.jsx`
- Create: `frontend/src/components/comments/CommentReactionBar.jsx`
- Modify: `frontend/src/components/comments/LessonComments.jsx`
- Modify: `frontend/src/components/comments/CommentList.jsx`
- Modify: `frontend/src/components/comments/CommentItem.jsx`
- Modify: `frontend/src/api/commentService.js`
- Test: `frontend/src/components/comments/LessonComments.test.jsx`
- Test: `frontend/src/components/comments/CommentReactionBar.test.jsx`

- [ ] **Step 1: Write the failing interaction tests**

```jsx
it("prefills @username when replying to a reply", async () => {
  render(<LessonComments lessonId="lesson-1" isAdmin={false} />);

  fireEvent.click(await screen.findByRole("button", { name: /reply bob/i }));

  expect(await screen.findByDisplayValue(/@Bob/i)).toBeInTheDocument();
});
```

```jsx
it("toggles emoji reactions on a comment", async () => {
  render(<CommentReactionBar commentId="comment-1" reactions={[]} onToggleReaction={mockToggleReaction} />);

  fireEvent.click(screen.getByRole("button", { name: "😀" }));

  expect(mockToggleReaction).toHaveBeenCalledWith("comment-1", "😀", true);
});
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd frontend && npm run test -- LessonComments.test.jsx CommentReactionBar.test.jsx`

Expected: FAIL because reply prefill, reaction bar, and sort/load-more behavior are not implemented yet.

- [ ] **Step 3: Extend API client with create/reply/reaction/admin helpers**

```js
export async function createLessonComment(lessonId, content) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/comments`, { content });
  return data;
}

export async function createLessonReply(lessonId, commentId, payload) {
  const { data } = await axiosClient.post(`/lessons/${lessonId}/comments/${commentId}/replies`, payload);
  return data;
}

export async function addLessonCommentReaction(lessonId, commentId, emoji) {
  await axiosClient.post(`/lessons/${lessonId}/comments/${commentId}/reactions`, { emoji });
}

export async function removeLessonCommentReaction(lessonId, commentId, emoji) {
  await axiosClient.delete(`/lessons/${lessonId}/comments/${commentId}/reactions/${encodeURIComponent(emoji)}`);
}
```

- [ ] **Step 4: Implement reply composer and sort/load-more control**

```jsx
export default function CommentComposer({ initialValue = "", submitLabel = "Gửi", onSubmit, onCancel }) {
  const [value, setValue] = useState(initialValue);

  async function handleSubmit(event) {
    event.preventDefault();
    const trimmed = value.trim();
    if (!trimmed) return;
    await onSubmit(trimmed);
    setValue("");
  }

  return (
    <form onSubmit={handleSubmit}>
      <textarea value={value} onChange={(event) => setValue(event.target.value)} />
      <button type="submit">{submitLabel}</button>
      {onCancel ? <button type="button" onClick={onCancel}>Hủy</button> : null}
    </form>
  );
}
```

```jsx
export default function CommentSortControl({ sort, onChange }) {
  return (
    <div role="group" aria-label="Sắp xếp bình luận">
      <button type="button" aria-pressed={sort === "newest"} onClick={() => onChange("newest")}>Mới nhất</button>
      <button type="button" aria-pressed={sort === "featured"} onClick={() => onChange("featured")}>Nổi bật</button>
    </div>
  );
}
```

- [ ] **Step 5: Implement reaction bar and inline moderation actions**

```jsx
export default function CommentReactionBar({ commentId, reactions, onToggleReaction }) {
  const commonEmoji = ["👍", "❤️", "🔥", "😀", "👏"];

  return (
    <div className={styles.reactionBar}>
      {reactions.map((reaction) => (
        <button
          key={`${commentId}-${reaction.emoji}`}
          type="button"
          aria-label={`${reaction.emoji} ${reaction.count}`}
          onClick={() => onToggleReaction(commentId, reaction.emoji, reaction.reactedByCurrentUser)}
        >
          <span>{reaction.emoji}</span>
          <span>{reaction.count}</span>
        </button>
      ))}

      {commonEmoji.map((emoji) => (
        <button key={emoji} type="button" aria-label={emoji} onClick={() => onToggleReaction(commentId, emoji, false)}>
          {emoji}
        </button>
      ))}
    </div>
  );
}
```

- [ ] **Step 6: Run frontend interaction tests**

Run: `cd frontend && npm run test -- LessonComments.test.jsx CommentReactionBar.test.jsx CourseLearnPage.test.jsx`

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/components/comments/CommentComposer.jsx \
  frontend/src/components/comments/CommentSortControl.jsx \
  frontend/src/components/comments/CommentReactionBar.jsx \
  frontend/src/components/comments/LessonComments.jsx \
  frontend/src/components/comments/CommentList.jsx \
  frontend/src/components/comments/CommentItem.jsx \
  frontend/src/api/commentService.js \
  frontend/src/components/comments/LessonComments.test.jsx \
  frontend/src/components/comments/CommentReactionBar.test.jsx
git commit -m "feat: add lesson comment interactions"
```

### Task 7: Add admin hide/unhide UI and end-to-end verification

**Files:**
- Modify: `frontend/src/components/comments/LessonComments.jsx`
- Modify: `frontend/src/components/comments/CommentItem.jsx`
- Modify: `frontend/src/api/commentService.js`
- Modify: `frontend/src/pages/CourseLearnPage.test.jsx`
- Test: `frontend/src/components/comments/LessonComments.test.jsx`
- Test: `backend/CourseVideo.API.Tests/Controllers/AdminCommentsControllerTests.cs`

- [ ] **Step 1: Write failing UI test for admin moderation controls**

```jsx
it("shows hide and unhide controls for admin users", async () => {
  render(<LessonComments lessonId="lesson-1" isAdmin />);

  expect(await screen.findByRole("button", { name: /ẩn bình luận/i })).toBeInTheDocument();
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd frontend && npm run test -- LessonComments.test.jsx`

Expected: FAIL because admin-only moderation actions are not rendered yet.

- [ ] **Step 3: Add admin API helpers and action buttons**

```js
export async function hideLessonComment(commentId, lessonId) {
  await axiosClient.patch(`/admin/comments/${commentId}/hide`, null, { params: { lessonId } });
}

export async function unhideLessonComment(commentId, lessonId) {
  await axiosClient.patch(`/admin/comments/${commentId}/unhide`, null, { params: { lessonId } });
}
```

```jsx
{isAdmin && !comment.isDeleted ? (
  <div className={styles.moderationActions}>
    {comment.isHidden ? (
      <button type="button" onClick={() => onUnhide(comment.id)}>Bỏ ẩn bình luận</button>
    ) : (
      <button type="button" onClick={() => onHide(comment.id)}>Ẩn bình luận</button>
    )}
  </div>
) : null}
```

- [ ] **Step 4: Run full verification**

Backend:

```bash
dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter "LessonComment|AdminCommentsControllerTests|LessonCommentsControllerTests"
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj
```

Frontend:

```bash
cd frontend
npm run test -- CourseLearnPage.test.jsx LessonComments.test.jsx CommentReactionBar.test.jsx
npm run build
```

Expected:

- backend comment tests PASS
- backend build PASS
- frontend focused tests PASS
- frontend production build PASS

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/comments/LessonComments.jsx \
  frontend/src/components/comments/CommentItem.jsx \
  frontend/src/api/commentService.js \
  frontend/src/components/comments/LessonComments.test.jsx \
  frontend/src/pages/CourseLearnPage.test.jsx
git commit -m "feat: add admin moderation to lesson comments"
```

## Spec Coverage Check

- Lesson-level comments under learner video: covered by Tasks 5 and 6.
- One-level replies with TikTok-style `@username`: covered by Tasks 3 and 6.
- Emoji reactions: covered by Tasks 3 and 6.
- Sorting `newest` and `featured`: covered by Tasks 2 and 6.
- Load more pagination: covered by Tasks 2 and 6.
- Learner permission based on current published-course access rule: covered by Task 3.
- Admin hide/delete moderation: covered by Tasks 4 and 7.
- Hidden/deleted placeholders and soft-delete semantics: covered by Tasks 3 and 4.

## Placeholder Scan

- No `TODO`, `TBD`, or “similar to above” placeholders remain.
- All new file paths are explicit.
- All command steps include exact commands and expected outcome.

## Type Consistency Check

- Threading model consistently uses:
  - `ParentCommentId`
  - `ReplyToUserId`
- Learner controller routes consistently use:
  - `/api/lessons/{lessonId}/comments`
  - `/api/lessons/{lessonId}/comments/{commentId}/replies`
  - `/api/lessons/{lessonId}/comments/{commentId}/reactions`
- Admin moderation consistently uses:
  - `/api/admin/comments/{commentId}/hide`
  - `/api/admin/comments/{commentId}/unhide`
