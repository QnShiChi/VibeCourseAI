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
                EnsureCategoriesTableExists(dbContext);
                EnsureCourseColumnsExist(dbContext);
                EnsureModulesTableExists(dbContext);
                EnsureLessonsTableExists(dbContext);
                EnsureLessonCommentsTableExists(dbContext);
                EnsureLessonCommentReactionsTableExists(dbContext);
                EnsureGenerationJobsTableExists(dbContext);
                EnsureLessonGeneratedContentColumnsExist(dbContext);
                EnsureLessonVoiceTutorColumnsExist(dbContext);
                EnsureLessonVoiceTutorTablesExist(dbContext);
                EnsureQuizTablesExist(dbContext);
                EnsureGenerationJobColumnsExist(dbContext);
                EnsureUserColumnsExist(dbContext);
                EnsurePaymentTablesExist(dbContext);
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

            IF COL_LENGTH('Courses', 'CategoryId') IS NULL
            BEGIN
                ALTER TABLE [Courses] ADD [CategoryId] uniqueidentifier NULL;
            END

            IF COL_LENGTH('Courses', 'Price') IS NULL
            BEGIN
                ALTER TABLE [Courses] ADD [Price] int NOT NULL CONSTRAINT [DF_Courses_Price] DEFAULT 599000;
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Courses_CategoryId' AND object_id = OBJECT_ID(N'[Courses]'))
            BEGIN
                CREATE INDEX [IX_Courses_CategoryId] ON [Courses] ([CategoryId]);
            END

            IF NOT EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_Courses_Categories_CategoryId'
            )
            BEGIN
                ALTER TABLE [Courses]
                ADD CONSTRAINT [FK_Courses_Categories_CategoryId]
                FOREIGN KEY ([CategoryId]) REFERENCES [Categories]([Id]);
            END
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO [Categories] ([Id], [Name], [Description], [Status], [SortOrder], [CreatedAt], [UpdatedAt])
            SELECT NEWID(), source.[MappedName], N'Chưa có mô tả ngắn.', N'Visible', source.[SortOrder], SYSUTCDATETIME(), NULL
            FROM (
                SELECT N'UI/UX Design' AS [MappedName], 100 AS [SortOrder]
                UNION ALL SELECT N'AI & Data', 200
                UNION ALL SELECT N'Development', 300
            ) AS source
            WHERE NOT EXISTS (
                SELECT 1 FROM [Categories] existing WHERE existing.[Name] = source.[MappedName]
            );

            INSERT INTO [Categories] ([Id], [Name], [Description], [Status], [SortOrder], [CreatedAt], [UpdatedAt])
            SELECT NEWID(), source.[MappedName], N'Chưa có mô tả ngắn.', N'Visible', 1000 + ROW_NUMBER() OVER (ORDER BY source.[MappedName]) * 100, SYSUTCDATETIME(), NULL
            FROM (
                SELECT DISTINCT
                    CASE
                        WHEN [Category] = N'UiUxDesign' THEN N'UI/UX Design'
                        WHEN [Category] = N'AiAndData' THEN N'AI & Data'
                        WHEN [Category] = N'Development' THEN N'Development'
                        ELSE LTRIM(RTRIM([Category]))
                    END AS [MappedName]
                FROM [Courses]
                WHERE [Category] IS NOT NULL AND LTRIM(RTRIM([Category])) <> N''
            ) AS source
            WHERE NOT EXISTS (
                SELECT 1 FROM [Categories] existing WHERE existing.[Name] = source.[MappedName]
            );
            """);

        dbContext.Database.ExecuteSqlRaw(
            """
            UPDATE [Courses]
            SET [CategoryId] = [Categories].[Id]
            FROM [Courses]
            INNER JOIN [Categories] ON [Categories].[Name] = CASE
                WHEN [Courses].[Category] = N'UiUxDesign' THEN N'UI/UX Design'
                WHEN [Courses].[Category] = N'AiAndData' THEN N'AI & Data'
                WHEN [Courses].[Category] = N'Development' THEN N'Development'
                ELSE LTRIM(RTRIM([Courses].[Category]))
            END
            WHERE [Courses].[CategoryId] IS NULL;

            DECLARE @DefaultCategoryId uniqueidentifier;
            SELECT TOP (1) @DefaultCategoryId = [Id]
            FROM [Categories]
            WHERE [Status] = N'Visible'
            ORDER BY [SortOrder], [CreatedAt] DESC;

            UPDATE [Courses]
            SET [CategoryId] = @DefaultCategoryId
            WHERE [CategoryId] IS NULL AND @DefaultCategoryId IS NOT NULL;
            """);
    }

    private static void EnsureCategoriesTableExists(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[Categories]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Categories] (
                    [Id] uniqueidentifier NOT NULL,
                    [Name] nvarchar(120) NOT NULL,
                    [Description] nvarchar(400) NOT NULL,
                    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_Categories_Status] DEFAULT N'Visible',
                    [SortOrder] int NOT NULL CONSTRAINT [DF_Categories_SortOrder] DEFAULT 0,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
                );

                CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
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

    private static void EnsureLessonVoiceTutorTablesExist(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[LessonVoiceSessions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [LessonVoiceSessions] (
                    [Id] uniqueidentifier NOT NULL,
                    [LessonId] uniqueidentifier NOT NULL,
                    [CourseId] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [Status] nvarchar(50) NOT NULL,
                    [StartedAt] datetime2 NOT NULL,
                    [LastActivityAt] datetime2 NOT NULL,
                    [EndedAt] datetime2 NULL,
                    [LastPausedVideoTimeSeconds] float NULL,
                    [VoiceProfileKey] nvarchar(200) NOT NULL,
                    [ContextScope] nvarchar(100) NOT NULL,
                    [ConversationSummary] nvarchar(max) NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_LessonVoiceSessions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_LessonVoiceSessions_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [Lessons]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_LessonVoiceSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
                );

                CREATE INDEX [IX_LessonVoiceSessions_LessonId_UserId_Status] ON [LessonVoiceSessions] ([LessonId], [UserId], [Status]);
            END

            IF OBJECT_ID(N'[LessonVoiceTurns]', N'U') IS NULL
            BEGIN
                CREATE TABLE [LessonVoiceTurns] (
                    [Id] uniqueidentifier NOT NULL,
                    [SessionId] uniqueidentifier NOT NULL,
                    [TurnNumber] int NOT NULL,
                    [Status] nvarchar(50) NOT NULL,
                    [PlaybackPausedAtSeconds] float NULL,
                    [UserAudioUrl] nvarchar(1000) NULL,
                    [TranscriptionText] nvarchar(max) NULL,
                    [TranscriptionConfidence] decimal(18, 2) NULL,
                    [AnswerText] nvarchar(max) NULL,
                    [AnswerSourceSummary] nvarchar(max) NULL,
                    [ErrorCode] nvarchar(100) NULL,
                    [ErrorMessage] nvarchar(2000) NULL,
                    [StartedAt] datetime2 NOT NULL,
                    [CompletedAt] datetime2 NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_LessonVoiceTurns] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_LessonVoiceTurns_LessonVoiceSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [LessonVoiceSessions]([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_LessonVoiceTurns_SessionId_TurnNumber] ON [LessonVoiceTurns] ([SessionId], [TurnNumber]);
            END

            IF OBJECT_ID(N'[LessonVoiceMessages]', N'U') IS NULL
            BEGIN
                CREATE TABLE [LessonVoiceMessages] (
                    [Id] uniqueidentifier NOT NULL,
                    [SessionId] uniqueidentifier NOT NULL,
                    [TurnNumber] int NOT NULL,
                    [Role] nvarchar(30) NOT NULL,
                    [ContentText] nvarchar(max) NOT NULL,
                    [ContentSourceType] nvarchar(50) NOT NULL,
                    [AudioUrl] nvarchar(1000) NULL,
                    [AudioDurationSeconds] float NULL,
                    [SequenceIndex] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_LessonVoiceMessages] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_LessonVoiceMessages_LessonVoiceSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [LessonVoiceSessions]([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_LessonVoiceMessages_SessionId_TurnNumber_SequenceIndex]
                    ON [LessonVoiceMessages] ([SessionId], [TurnNumber], [SequenceIndex]);
            END
            """);
    }

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
                    CONSTRAINT [FK_Quizzes_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id])
                );

                CREATE UNIQUE INDEX [IX_Quizzes_LessonId] ON [Quizzes]([LessonId]) WHERE [LessonId] IS NOT NULL;
                CREATE UNIQUE INDEX [IX_Quizzes_CourseId] ON [Quizzes]([CourseId]) WHERE [CourseId] IS NOT NULL AND [Type] = 'Final';
            END

            IF OBJECT_ID(N'[Quizzes]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[Quizzes]')
                      AND name = N'IX_Quizzes_LessonId'
                )
                BEGIN
                    CREATE UNIQUE INDEX [IX_Quizzes_LessonId] ON [Quizzes]([LessonId]) WHERE [LessonId] IS NOT NULL;
                END

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[Quizzes]')
                      AND name = N'IX_Quizzes_CourseId'
                )
                BEGIN
                    DROP INDEX [IX_Quizzes_CourseId] ON [Quizzes];
                END

                CREATE UNIQUE INDEX [IX_Quizzes_CourseId] ON [Quizzes]([CourseId]) WHERE [CourseId] IS NOT NULL AND [Type] = 'Final';
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

            IF COL_LENGTH('LessonComments', 'PinnedAt') IS NULL
            BEGIN
                ALTER TABLE [LessonComments] ADD [PinnedAt] datetime2 NULL;
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

    private static void EnsurePaymentTablesExist(AppDbContext dbContext)
    {
        if (!dbContext.Database.IsSqlServer())
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            IF OBJECT_ID(N'[CartItems]', N'U') IS NULL
            BEGIN
                CREATE TABLE [CartItems] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NULL,
                    [GuestCartToken] nvarchar(100) NULL,
                    [CourseId] uniqueidentifier NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CartItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_CartItems_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE
                );
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_CartItems_UserId_CourseId' AND object_id = OBJECT_ID(N'[CartItems]')
            )
            BEGIN
                CREATE UNIQUE INDEX [IX_CartItems_UserId_CourseId]
                    ON [CartItems]([UserId], [CourseId])
                    WHERE [UserId] IS NOT NULL;
            END

            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_CartItems_GuestCartToken_CourseId' AND object_id = OBJECT_ID(N'[CartItems]')
            )
            BEGIN
                CREATE UNIQUE INDEX [IX_CartItems_GuestCartToken_CourseId]
                    ON [CartItems]([GuestCartToken], [CourseId])
                    WHERE [GuestCartToken] IS NOT NULL;
            END

            IF OBJECT_ID(N'[PaymentOrders]', N'U') IS NULL
            BEGIN
                CREATE TABLE [PaymentOrders] (
                    [Id] uniqueidentifier NOT NULL,
                    [OrderCode] nvarchar(32) NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [CourseId] uniqueidentifier NOT NULL,
                    [Amount] int NOT NULL,
                    [Status] nvarchar(30) NOT NULL,
                    [ExpiresAt] datetime2 NOT NULL,
                    [PaidAt] datetime2 NULL,
                    [SepayTransactionId] int NULL,
                    [BankCode] nvarchar(50) NULL,
                    [BankName] nvarchar(200) NULL,
                    [BankAccountNumber] nvarchar(50) NULL,
                    [AccountHolderName] nvarchar(200) NULL,
                    [TransferContent] nvarchar(200) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_PaymentOrders] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PaymentOrders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
                    CONSTRAINT [FK_PaymentOrders_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX [IX_PaymentOrders_OrderCode] ON [PaymentOrders]([OrderCode]);
                CREATE INDEX [IX_PaymentOrders_UserId_CourseId_Status] ON [PaymentOrders]([UserId], [CourseId], [Status]);
            END

            IF OBJECT_ID(N'[CourseEnrollments]', N'U') IS NULL
            BEGIN
                CREATE TABLE [CourseEnrollments] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [CourseId] uniqueidentifier NOT NULL,
                    [PaymentOrderId] uniqueidentifier NOT NULL,
                    [GrantedAt] datetime2 NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_CourseEnrollments] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CourseEnrollments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]),
                    CONSTRAINT [FK_CourseEnrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_CourseEnrollments_PaymentOrders_PaymentOrderId] FOREIGN KEY ([PaymentOrderId]) REFERENCES [PaymentOrders]([Id])
                );

                CREATE UNIQUE INDEX [IX_CourseEnrollments_UserId_CourseId] ON [CourseEnrollments]([UserId], [CourseId]);
            END

            IF OBJECT_ID(N'[PaymentTransactionLogs]', N'U') IS NULL
            BEGIN
                CREATE TABLE [PaymentTransactionLogs] (
                    [Id] uniqueidentifier NOT NULL,
                    [SepayTransactionId] int NOT NULL,
                    [Gateway] nvarchar(100) NOT NULL,
                    [TransactionDateText] nvarchar(50) NOT NULL,
                    [AccountNumber] nvarchar(50) NOT NULL,
                    [SubAccount] nvarchar(100) NULL,
                    [Code] nvarchar(100) NULL,
                    [Content] nvarchar(500) NOT NULL,
                    [TransferType] nvarchar(10) NOT NULL,
                    [Description] nvarchar(1000) NULL,
                    [TransferAmount] int NOT NULL,
                    [Accumulated] bigint NOT NULL,
                    [ReferenceCode] nvarchar(100) NULL,
                    [RawPayload] nvarchar(max) NOT NULL,
                    [MatchedPaymentOrderId] uniqueidentifier NULL,
                    [IsDuplicate] bit NOT NULL CONSTRAINT [DF_PaymentTransactionLogs_IsDuplicate] DEFAULT 0,
                    [ProcessedAt] datetime2 NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_PaymentTransactionLogs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PaymentTransactionLogs_PaymentOrders_MatchedPaymentOrderId] FOREIGN KEY ([MatchedPaymentOrderId]) REFERENCES [PaymentOrders]([Id]) ON DELETE SET NULL
                );

                CREATE UNIQUE INDEX [IX_PaymentTransactionLogs_SepayTransactionId] ON [PaymentTransactionLogs]([SepayTransactionId]);
            END

            IF COL_LENGTH(N'[Users]', N'PaymentBankCode') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [PaymentBankCode] nvarchar(50) NULL;
            END

            IF COL_LENGTH(N'[Users]', N'PaymentBankName') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [PaymentBankName] nvarchar(200) NULL;
            END

            IF COL_LENGTH(N'[Users]', N'PaymentBankAccountNumber') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [PaymentBankAccountNumber] nvarchar(50) NULL;
            END

            IF COL_LENGTH(N'[Users]', N'PaymentAccountHolderName') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [PaymentAccountHolderName] nvarchar(200) NULL;
            END

            IF COL_LENGTH(N'[Users]', N'PaymentSettingsUpdatedAt') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [PaymentSettingsUpdatedAt] datetime2 NULL;
            END
            """);
    }
}
