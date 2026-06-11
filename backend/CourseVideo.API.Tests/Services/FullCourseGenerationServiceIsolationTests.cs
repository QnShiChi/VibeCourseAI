using CourseVideo.API.Data;
using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.OpenRouter;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services;
using CourseVideo.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CourseVideo.API.Tests.Services;

public class FullCourseGenerationServiceIsolationTests
{
    [Fact]
    public async Task ProcessJobAsync_ContinuesLessonPipeline_WhenLessonQuizGenerationHitsConcurrencyConflict()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IGenerationJobRepository, GenerationJobRepository>();
        services.AddScoped<IQuizRepository, ConcurrencyFailingQuizRepository>();
        services.AddScoped<IQuizGenerationService, QuizGenerationService>();
        services.AddScoped<IFullCourseGenerationService, FullCourseGenerationService>();
        services.AddSingleton<IFullCourseJobQueue, NoOpFullCourseJobQueue>();
        services.AddSingleton<ILessonContentGenerationService, StubLessonContentGenerationService>();
        services.AddSingleton<ILessonAudioGenerationService, StubLessonAudioGenerationService>();
        services.AddSingleton<ILessonVideoGenerationService, StubLessonVideoGenerationService>();
        services.AddSingleton<IOpenRouterQuizGenerationService, StubOpenRouterQuizGenerationService>();

        await using var provider = services.BuildServiceProvider();
        await SeedAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IFullCourseGenerationService>();

        var action = async () => await service.ProcessJobAsync(TestData.JobId, CancellationToken.None);

        await action.Should().NotThrowAsync();

        await using var verificationScope = provider.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await dbContext.GenerationJobs.SingleAsync(item => item.Id == TestData.JobId);
        var lesson = await dbContext.Lessons.SingleAsync(item => item.Id == TestData.LessonId);

        job.Status.Should().Be("Completed");
        job.ProcessedItems.Should().Be(3);
        job.FailedItems.Should().Be(0);
        lesson.ContentGenerationStatus.Should().Be("Completed");
        lesson.AudioGenerationStatus.Should().Be("Completed");
        lesson.VideoGenerationStatus.Should().Be("Completed");
    }

    private static async Task SeedAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = TestData.UserId,
            FullName = "Admin",
            Email = "admin@example.com",
            PasswordHash = "hash",
            RoleId = 1
        };

        var category = new Category
        {
            Id = TestData.CategoryId,
            Name = "Programming",
            Description = "Programming courses"
        };

        var syllabus = new Syllabus
        {
            Id = TestData.SyllabusId,
            Title = "Syllabus",
            Description = "Desc",
            OriginalFileName = "oop.pdf",
            StoredFileName = "oop.pdf",
            FilePath = "/tmp/oop.pdf",
            FileType = "application/pdf",
            FileSize = 1024,
            ExtractedText = "Week 1: Basics",
            UploadedByUserId = user.Id
        };

        var course = new Course
        {
            Id = TestData.CourseId,
            Title = "OOP",
            Description = "Object oriented programming",
            CategoryId = category.Id,
            SyllabusId = syllabus.Id
        };

        var module = new Module
        {
            Id = TestData.ModuleId,
            CourseId = course.Id,
            Title = "Module 1",
            Description = "Foundations",
            OrderIndex = 1
        };

        var lesson = new Lesson
        {
            Id = TestData.LessonId,
            ModuleId = module.Id,
            Title = "Lesson 1",
            Description = "Intro",
            ContentSeed = "Class, object, encapsulation",
            OrderIndex = 1,
            ContentGenerationStatus = "NotGenerated",
            AudioGenerationStatus = "NotGenerated",
            VideoGenerationStatus = "NotGenerated"
        };

        var quiz = new Quiz
        {
            Id = TestData.LessonQuizId,
            LessonId = lesson.Id,
            CourseId = course.Id,
            Type = "Lesson",
            Status = "Ready",
            Title = "Existing lesson quiz",
            QuestionCount = 1,
            Questions =
            [
                new QuizQuestion
                {
                    Id = TestData.QuestionId,
                    QuestionText = "Old question",
                    Explanation = "Old explanation",
                    OrderIndex = 1,
                    Options =
                    [
                        new QuizOption
                        {
                            Id = TestData.OptionId,
                            OptionText = "Old option",
                            OrderIndex = 1,
                            IsCorrect = true
                        }
                    ]
                }
            ]
        };

        var job = new GenerationJob
        {
            Id = TestData.JobId,
            SyllabusId = syllabus.Id,
            CourseId = course.Id,
            CreatedByUserId = user.Id,
            JobType = "GenerateFullCourse",
            Status = "Pending"
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.Categories.AddAsync(category);
        await dbContext.Syllabuses.AddAsync(syllabus);
        await dbContext.Courses.AddAsync(course);
        await dbContext.Modules.AddAsync(module);
        await dbContext.Lessons.AddAsync(lesson);
        await dbContext.Quizzes.AddAsync(quiz);
        await dbContext.GenerationJobs.AddAsync(job);
        await dbContext.SaveChangesAsync();
    }

    private static class TestData
    {
        public static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000101");
        public static readonly Guid CategoryId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        public static readonly Guid SyllabusId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        public static readonly Guid CourseId = Guid.Parse("00000000-0000-0000-0000-000000000104");
        public static readonly Guid ModuleId = Guid.Parse("00000000-0000-0000-0000-000000000105");
        public static readonly Guid LessonId = Guid.Parse("00000000-0000-0000-0000-000000000106");
        public static readonly Guid LessonQuizId = Guid.Parse("00000000-0000-0000-0000-000000000107");
        public static readonly Guid QuestionId = Guid.Parse("00000000-0000-0000-0000-000000000108");
        public static readonly Guid OptionId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        public static readonly Guid JobId = Guid.Parse("00000000-0000-0000-0000-000000000110");
    }

    private sealed class ConcurrencyFailingQuizRepository : IQuizRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly DbContextOptions<AppDbContext> _options;
        private bool _injectedConflict;

        public ConcurrencyFailingQuizRepository(AppDbContext dbContext, DbContextOptions<AppDbContext> options)
        {
            _dbContext = dbContext;
            _options = options;
        }

        public Task<Quiz?> GetLessonQuizAsync(Guid lessonId, CancellationToken cancellationToken = default) =>
            _dbContext.Quizzes
                .Include(quiz => quiz.Questions.OrderBy(question => question.OrderIndex))
                .ThenInclude(question => question.Options.OrderBy(option => option.OrderIndex))
                .FirstOrDefaultAsync(quiz => quiz.LessonId == lessonId, cancellationToken);

        public Task<Quiz?> GetCourseFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default) =>
            _dbContext.Quizzes
                .Include(quiz => quiz.Questions.OrderBy(question => question.OrderIndex))
                .ThenInclude(question => question.Options.OrderBy(option => option.OrderIndex))
                .FirstOrDefaultAsync(quiz => quiz.CourseId == courseId && quiz.Type == "Final", cancellationToken);

        public Task<Quiz?> GetByIdAsync(Guid quizId, CancellationToken cancellationToken = default) =>
            _dbContext.Quizzes
                .Include(quiz => quiz.Questions.OrderBy(question => question.OrderIndex))
                .ThenInclude(question => question.Options.OrderBy(option => option.OrderIndex))
                .Include(quiz => quiz.Attempts)
                .FirstOrDefaultAsync(quiz => quiz.Id == quizId, cancellationToken);

        public Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default) =>
            _dbContext.Quizzes.AddAsync(quiz, cancellationToken).AsTask();

        public Task AddAttemptAsync(QuizAttempt attempt, CancellationToken cancellationToken = default) =>
            _dbContext.QuizAttempts.AddAsync(attempt, cancellationToken).AsTask();

        public Task AddAttemptAnswersAsync(IReadOnlyCollection<QuizAttemptAnswer> answers, CancellationToken cancellationToken = default) =>
            _dbContext.QuizAttemptAnswers.AddRangeAsync(answers, cancellationToken);

        public async Task<IReadOnlyList<QuizAttempt>> GetAttemptsAsync(Guid quizId, Guid userId, CancellationToken cancellationToken = default) =>
            await _dbContext.QuizAttempts
                .Include(attempt => attempt.Answers)
                .Where(attempt => attempt.QuizId == quizId && attempt.UserId == userId)
                .OrderByDescending(attempt => attempt.StartedAt)
                .ToListAsync(cancellationToken);

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!_injectedConflict)
            {
                _injectedConflict = true;
                var trackedQuizId = _dbContext.ChangeTracker.Entries<Quiz>()
                    .Where(entry => entry.State != EntityState.Unchanged)
                    .Select(entry => entry.Entity.Id)
                    .FirstOrDefault();

                if (trackedQuizId != Guid.Empty)
                {
                    await using var externalContext = new AppDbContext(_options);
                    var persistedQuiz = await externalContext.Quizzes.FirstAsync(quiz => quiz.Id == trackedQuizId, cancellationToken);
                    externalContext.Quizzes.Remove(persistedQuiz);
                    await externalContext.SaveChangesAsync(cancellationToken);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class StubOpenRouterQuizGenerationService : IOpenRouterQuizGenerationService
    {
        public Task<OpenRouterQuizGenerationResult> GenerateLessonQuizAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult("Lesson quiz"));

        public Task<OpenRouterQuizGenerationResult> GenerateFinalQuizAsync(Course course, IReadOnlyList<Lesson> lessons, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResult("Final quiz"));

        private static OpenRouterQuizGenerationResult CreateResult(string title) =>
            new()
            {
                Title = title,
                Questions =
                [
                    new OpenRouterQuizQuestionResult
                    {
                        QuestionText = "What is OOP?",
                        Explanation = "A programming paradigm.",
                        Options =
                        [
                            new OpenRouterQuizOptionResult { OptionText = "A paradigm", IsCorrect = true },
                            new OpenRouterQuizOptionResult { OptionText = "A database", IsCorrect = false }
                        ]
                    }
                ]
            };
    }

    private sealed class StubLessonContentGenerationService : ILessonContentGenerationService
    {
        public Task<GenerateLessonContentResponse> GenerateCourseContentAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenerateLessonContentResponse> RegenerateLessonContentAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task GenerateContentForLessonInternalAsync(Course course, Module module, Lesson lesson, CancellationToken cancellationToken)
        {
            lesson.ContentGenerationStatus = "Completed";
            lesson.ContentGeneratedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }

    private sealed class StubLessonAudioGenerationService : ILessonAudioGenerationService
    {
        public Task<GenerateLessonAudioResponse> GenerateCourseAudioAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenerateLessonAudioResponse> GenerateLessonAudioAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task GenerateAudioForLessonInternalAsync(Lesson lesson, CancellationToken cancellationToken, Func<int, int, Task>? onSegmentCompleted = null)
        {
            lesson.AudioGenerationStatus = "Completed";
            lesson.AudioGeneratedAt = DateTime.UtcNow;
            if (onSegmentCompleted is not null)
            {
                await onSegmentCompleted(1, 1);
            }
        }
    }

    private sealed class StubLessonVideoGenerationService : ILessonVideoGenerationService
    {
        public Task<GenerateLessonVideoResponse> GenerateCourseVideoAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task GenerateVideoForLessonInternalAsync(Lesson lesson, CancellationToken cancellationToken)
        {
            lesson.VideoGenerationStatus = "Completed";
            lesson.VideoGeneratedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpFullCourseJobQueue : IFullCourseJobQueue
    {
        public void Enqueue(Guid jobId)
        {
        }

        public Task<Guid> DequeueAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
