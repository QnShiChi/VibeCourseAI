using CourseVideo.API.Configuration;
using CourseVideo.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext dbContext, IOptions<AdminSeedOptions> adminSeedOptions)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(3);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (dbContext.Database.IsRelational() && dbContext.Database.GetMigrations().Any())
                {
                    dbContext.Database.Migrate();
                }
                else
                {
                    dbContext.Database.EnsureCreated();
                }

                EnsureRefreshTokensTableExists(dbContext);
                EnsureSyllabusesTableExists(dbContext);
                EnsureCourseColumnsExist(dbContext);
                EnsureModulesTableExists(dbContext);
                EnsureLessonsTableExists(dbContext);
                EnsureLessonCommentsTableExists(dbContext);
                EnsureLessonCommentReactionsTableExists(dbContext);
                EnsureGenerationJobsTableExists(dbContext);
                EnsureLessonGeneratedContentColumnsExist(dbContext);
                EnsureGenerationJobColumnsExist(dbContext);
                EnsureUserColumnsExist(dbContext);
                Seed(dbContext, adminSeedOptions.Value);
                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException("Database initialization failed after multiple attempts.");
    }

    public static void Seed(AppDbContext dbContext, AdminSeedOptions adminSeed)
    {
        if (!dbContext.Courses.Any())
        {
            dbContext.Courses.Add(new Course
            {
                Title = "Sample Course",
                Description = "Skeleton course created during initial project setup.",
                IsPublished = false
            });
        }

        var hasAdminSeed = !string.IsNullOrWhiteSpace(adminSeed.Email)
            && !string.IsNullOrWhiteSpace(adminSeed.Password)
            && !string.IsNullOrWhiteSpace(adminSeed.FullName);

        var adminRole = dbContext.Roles.First(role => role.Name == "Admin");
        var hasConfiguredAdminUser = dbContext.Users.Any(user => user.Email == adminSeed.Email);

        if (hasAdminSeed && !hasConfiguredAdminUser)
        {
            var adminUser = new User
            {
                FullName = adminSeed.FullName,
                Email = adminSeed.Email,
                RoleId = adminRole.Id,
                IsActive = true
            };

            var passwordHasher = new PasswordHasher<User>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminSeed.Password);
            dbContext.Users.Add(adminUser);
        }

        dbContext.SaveChanges();
    }

    private static void EnsureSyllabusesTableExists(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[Syllabuses]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Syllabuses] (
                    [Id] uniqueidentifier NOT NULL,
                    [Title] nvarchar(255) NOT NULL,
                    [Description] nvarchar(2000) NOT NULL,
                    [OriginalFileName] nvarchar(255) NOT NULL,
                    [StoredFileName] nvarchar(255) NOT NULL,
                    [FilePath] nvarchar(500) NOT NULL,
                    [FileType] nvarchar(50) NOT NULL,
                    [FileSize] bigint NOT NULL,
                    [ExtractedText] nvarchar(max) NOT NULL,
                    [UploadedByUserId] uniqueidentifier NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_Syllabuses] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Syllabuses_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users]([Id])
                );

                CREATE INDEX [IX_Syllabuses_CreatedAt] ON [Syllabuses] ([CreatedAt]);
                CREATE INDEX [IX_Syllabuses_UploadedByUserId] ON [Syllabuses] ([UploadedByUserId]);
            END
            """);
    }

    private static void EnsureCourseColumnsExist(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF COL_LENGTH('Courses', 'SyllabusId') IS NULL
            BEGIN
                ALTER TABLE [Courses] ADD [SyllabusId] uniqueidentifier NULL;
                CREATE INDEX [IX_Courses_SyllabusId] ON [Courses] ([SyllabusId]);
                ALTER TABLE [Courses] ADD CONSTRAINT [FK_Courses_Syllabuses_SyllabusId] FOREIGN KEY ([SyllabusId]) REFERENCES [Syllabuses]([Id]) ON DELETE CASCADE;
            END

            IF COL_LENGTH('Courses', 'CreatedByUserId') IS NULL
            BEGIN
                ALTER TABLE [Courses] ADD [CreatedByUserId] uniqueidentifier NULL;
            END

            IF COL_LENGTH('Courses', 'ThumbnailUrl') IS NULL
            BEGIN
                ALTER TABLE [Courses] ADD [ThumbnailUrl] nvarchar(1000) NULL;
            END

            IF COL_LENGTH('Courses', 'Category') IS NULL
            BEGIN
                ALTER TABLE [Courses] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_Courses_Category] DEFAULT N'UiUxDesign';
            END
            """);
    }

    private static void EnsureGenerationJobsTableExists(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[GenerationJobs]', N'U') IS NULL
            BEGIN
                CREATE TABLE [GenerationJobs] (
                    [Id] uniqueidentifier NOT NULL,
                    [SyllabusId] uniqueidentifier NOT NULL,
                    [CourseId] uniqueidentifier NULL,
                    [LessonId] uniqueidentifier NULL,
                    [JobType] nvarchar(100) NULL,
                    [Status] nvarchar(50) NOT NULL,
                    [ErrorMessage] nvarchar(2000) NULL,
                    [TotalItems] int NULL,
                    [ProcessedItems] int NULL,
                    [FailedItems] int NULL,
                    [ProgressMessage] nvarchar(500) NULL,
                    [CreatedByUserId] uniqueidentifier NOT NULL,
                    [StartedAt] datetime2 NULL,
                    [CompletedAt] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_GenerationJobs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_GenerationJobs_Syllabuses_SyllabusId] FOREIGN KEY ([SyllabusId]) REFERENCES [Syllabuses]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_GenerationJobs_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]),
                    CONSTRAINT [FK_GenerationJobs_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users]([Id])
                );

                CREATE INDEX [IX_GenerationJobs_CreatedAt] ON [GenerationJobs] ([CreatedAt]);
                CREATE INDEX [IX_GenerationJobs_SyllabusId] ON [GenerationJobs] ([SyllabusId]);
                CREATE INDEX [IX_GenerationJobs_CourseId] ON [GenerationJobs] ([CourseId]);
                CREATE INDEX [IX_GenerationJobs_CreatedByUserId] ON [GenerationJobs] ([CreatedByUserId]);
            END
            """);
    }

    private static void EnsureModulesTableExists(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[Modules]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Modules] (
                    [Id] uniqueidentifier NOT NULL,
                    [CourseId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [Description] nvarchar(2000) NOT NULL,
                    [OrderIndex] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_Modules] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Modules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_Modules_CourseId_OrderIndex] ON [Modules] ([CourseId], [OrderIndex]);
            END
            """);
    }

    private static void EnsureLessonsTableExists(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[Lessons]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Lessons] (
                    [Id] uniqueidentifier NOT NULL,
                    [ModuleId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [Description] nvarchar(2000) NOT NULL,
                    [OrderIndex] int NOT NULL,
                    [ContentSeed] nvarchar(max) NOT NULL,
                    [TeachingScript] nvarchar(max) NULL,
                    [SlideOutlineJson] nvarchar(max) NULL,
                    [VoiceoverPlanJson] nvarchar(max) NULL,
                    [ContentGenerationStatus] nvarchar(50) NOT NULL,
                    [ContentGeneratedAt] datetime2 NULL,
                    [ContentGenerationError] nvarchar(2000) NULL,
                    [VideoUrl] nvarchar(1000) NULL,
                    [VideoGenerationStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_Lessons_VideoGenerationStatus] DEFAULT N'NotGenerated',
                    [VideoGenerationError] nvarchar(2000) NULL,
                    [VideoGeneratedAt] datetime2 NULL,
                    [AudioUrl] nvarchar(1000) NULL,
                    [Duration] int NULL,
                    [AudioSegmentsJson] nvarchar(max) NULL,
                    [AudioGenerationStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_Lessons_AudioGenerationStatus] DEFAULT N'NotGenerated',
                    [AudioGenerationError] nvarchar(2000) NULL,
                    [AudioGeneratedAt] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_Lessons] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Lessons_Modules_ModuleId] FOREIGN KEY ([ModuleId]) REFERENCES [Modules]([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_Lessons_ModuleId_OrderIndex] ON [Lessons] ([ModuleId], [OrderIndex]);
            END
            """);
    }

    private static void EnsureLessonGeneratedContentColumnsExist(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF COL_LENGTH('Lessons', 'TeachingScript') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [TeachingScript] nvarchar(max) NULL;
            END

            IF COL_LENGTH('Lessons', 'SlideOutlineJson') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [SlideOutlineJson] nvarchar(max) NULL;
            END

            IF COL_LENGTH('Lessons', 'VoiceoverPlanJson') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [VoiceoverPlanJson] nvarchar(max) NULL;
            END

            IF COL_LENGTH('Lessons', 'ContentGenerationStatus') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [ContentGenerationStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_Lessons_ContentGenerationStatus] DEFAULT N'NotGenerated';
            END

            IF COL_LENGTH('Lessons', 'ContentGeneratedAt') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [ContentGeneratedAt] datetime2 NULL;
            END

            IF COL_LENGTH('Lessons', 'ContentGenerationError') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [ContentGenerationError] nvarchar(2000) NULL;
            END

            IF COL_LENGTH('Lessons', 'AudioSegmentsJson') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [AudioSegmentsJson] nvarchar(max) NULL;
            END

            IF COL_LENGTH('Lessons', 'AudioGenerationStatus') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [AudioGenerationStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_Lessons_AudioGenerationStatus] DEFAULT N'NotGenerated';
            END

            IF COL_LENGTH('Lessons', 'AudioGenerationError') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [AudioGenerationError] nvarchar(2000) NULL;
            END

            IF COL_LENGTH('Lessons', 'AudioGeneratedAt') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [AudioGeneratedAt] datetime2 NULL;
            END

            IF COL_LENGTH('Lessons', 'VideoGenerationStatus') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [VideoGenerationStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_Lessons_VideoGenerationStatus_Legacy] DEFAULT N'NotGenerated';
            END

            IF COL_LENGTH('Lessons', 'VideoGenerationError') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [VideoGenerationError] nvarchar(2000) NULL;
            END

            IF COL_LENGTH('Lessons', 'VideoGeneratedAt') IS NULL
            BEGIN
                ALTER TABLE [Lessons] ADD [VideoGeneratedAt] datetime2 NULL;
            END
            """);
    }

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
                CREATE INDEX [IX_LessonComments_ParentCommentId] ON [LessonComments] ([ParentCommentId]);
            END

            IF COL_LENGTH('LessonComments', 'Sentiment') IS NULL
            BEGIN
                ALTER TABLE [LessonComments] ADD [Sentiment] nvarchar(50) NULL;
            END
            """);
    }

    private static void EnsureLessonCommentReactionsTableExists(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[LessonCommentReactions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [LessonCommentReactions] (
                    [Id] uniqueidentifier NOT NULL,
                    [CommentId] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [Emoji] nvarchar(32) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_LessonCommentReactions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_LessonCommentReactions_LessonComments_CommentId] FOREIGN KEY ([CommentId]) REFERENCES [LessonComments]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_LessonCommentReactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
                );

                CREATE UNIQUE INDEX [IX_LessonCommentReactions_CommentId_UserId_Emoji]
                    ON [LessonCommentReactions] ([CommentId], [UserId], [Emoji]);
            END
            """);
    }

    private static void EnsureGenerationJobColumnsExist(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF COL_LENGTH('GenerationJobs', 'LessonId') IS NULL
            BEGIN
                ALTER TABLE [GenerationJobs] ADD [LessonId] uniqueidentifier NULL;
            END

            IF COL_LENGTH('GenerationJobs', 'JobType') IS NULL
            BEGIN
                ALTER TABLE [GenerationJobs] ADD [JobType] nvarchar(100) NULL;
            END

            IF COL_LENGTH('GenerationJobs', 'TotalItems') IS NULL
            BEGIN
                ALTER TABLE [GenerationJobs] ADD [TotalItems] int NULL;
            END

            IF COL_LENGTH('GenerationJobs', 'ProcessedItems') IS NULL
            BEGIN
                ALTER TABLE [GenerationJobs] ADD [ProcessedItems] int NULL;
            END

            IF COL_LENGTH('GenerationJobs', 'FailedItems') IS NULL
            BEGIN
                ALTER TABLE [GenerationJobs] ADD [FailedItems] int NULL;
            END

            IF COL_LENGTH('GenerationJobs', 'ProgressMessage') IS NULL
            BEGIN
                ALTER TABLE [GenerationJobs] ADD [ProgressMessage] nvarchar(500) NULL;
            END
            """);
    }

    private static void EnsureRefreshTokensTableExists(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[RefreshTokens]', N'U') IS NULL
            BEGIN
                CREATE TABLE [RefreshTokens] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [TokenHash] nvarchar(500) NOT NULL,
                    [ExpiresAt] datetime2 NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [RevokedAt] datetime2 NULL,
                    [ReplacedByTokenHash] nvarchar(500) NULL,
                    [CreatedByIp] nvarchar(100) NULL,
                    [RevokedByIp] nvarchar(100) NULL,
                    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
                CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
            END
            """);
    }

    private static void EnsureUserColumnsExist(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF COL_LENGTH('Users', 'ResetPasswordToken') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [ResetPasswordToken] nvarchar(500) NULL;
            END

            IF COL_LENGTH('Users', 'ResetPasswordTokenExpiry') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [ResetPasswordTokenExpiry] datetime2 NULL;
            END
            """);
    }
}
