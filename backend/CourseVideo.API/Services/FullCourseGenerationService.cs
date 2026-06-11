using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class FullCourseGenerationService : IFullCourseGenerationService
{
    private const string GenerateFullCourseJobType = "GenerateFullCourse";
    private const int MaxConsecutiveSystemicAudioFailures = 3;
    private readonly ICourseRepository _courseRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IGenerationJobRepository _generationJobRepository;
    private readonly IFullCourseJobQueue _fullCourseJobQueue;
    private readonly ILessonContentGenerationService _lessonContentService;
    private readonly ILessonAudioGenerationService _lessonAudioService;
    private readonly ILessonVideoGenerationService _lessonVideoService;
    private readonly IQuizGenerationService _quizGenerationService;
    private readonly IServiceScopeFactory? _serviceScopeFactory;

    public FullCourseGenerationService(
        ICourseRepository courseRepository,
        ILessonRepository lessonRepository,
        IGenerationJobRepository generationJobRepository,
        IFullCourseJobQueue fullCourseJobQueue,
        ILessonContentGenerationService lessonContentService,
        ILessonAudioGenerationService lessonAudioService,
        ILessonVideoGenerationService lessonVideoService,
        IQuizGenerationService quizGenerationService,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _courseRepository = courseRepository;
        _lessonRepository = lessonRepository;
        _generationJobRepository = generationJobRepository;
        _fullCourseJobQueue = fullCourseJobQueue;
        _lessonContentService = lessonContentService;
        _lessonAudioService = lessonAudioService;
        _lessonVideoService = lessonVideoService;
        _quizGenerationService = quizGenerationService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<GenerateFullCourseResponse> GenerateFullCourseAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Không tìm thấy khóa học.");

        if (!course.SyllabusId.HasValue)
        {
            throw new InvalidOperationException("Khóa học này chưa có syllabus nguồn để gắn generation job.");
        }

        // Check if there are any running jobs for this course
        if (await _generationJobRepository.HasRunningLessonContentJobForCourseAsync(courseId) ||
            await _generationJobRepository.HasRunningLessonAudioJobForCourseAsync(courseId) ||
            await _generationJobRepository.HasRunningLessonVideoJobForCourseAsync(courseId) ||
            await HasRunningFullCourseJobAsync(courseId))
        {
            throw new InvalidOperationException("Khóa học này đang có job chạy nền. Vui lòng đợi job hoàn tất.");
        }

        var lessons = GetEligibleLessons(course);
        var totalPendingSteps = CountRemainingSteps(lessons);
        if (lessons.Count == 0)
        {
            throw new InvalidOperationException("Không còn lesson nào cần generate trong khóa học này.");
        }

        var job = new GenerationJob
        {
            SyllabusId = course.SyllabusId.Value,
            CourseId = course.Id,
            CreatedByUserId = createdByUserId,
            JobType = GenerateFullCourseJobType,
            Status = "Pending",
            TotalItems = totalPendingSteps,
            ProcessedItems = 0,
            FailedItems = 0,
            ProgressMessage = $"Đã tạo job generate A->Z cho {lessons.Count} lesson với {totalPendingSteps} bước cần xử lý."
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();
        _fullCourseJobQueue.Enqueue(job.Id);

        return new GenerateFullCourseResponse
        {
            JobId = job.Id,
            CourseId = courseId,
            Status = job.Status,
            TotalLessons = lessons.Count,
            ProcessedLessons = 0,
            FailedLessons = 0,
            Message = job.ProgressMessage
        };
    }

    public async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _generationJobRepository.GetByIdAsync(jobId);
        if (job is null || !job.CourseId.HasValue) return;

        try
        {
            await ProcessCourseJobAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await PersistFailedJobAsync(job, exception, cancellationToken);
        }
    }

    private async Task ProcessCourseJobAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(job.CourseId!.Value)
            ?? throw new KeyNotFoundException("Không tìm thấy khóa học cho generation job.");

        var lessonPairs = GetEligibleLessons(course);
        var totalPendingSteps = CountRemainingSteps(lessonPairs);

        if (lessonPairs.Count == 0)
        {
            job.Status = "Completed";
            job.TotalItems = 0;
            job.ProcessedItems = 0;
            job.FailedItems = 0;
            job.ProgressMessage = "Không còn lesson nào cần generate.";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();
            return;
        }

        job.Status = "GeneratingFullCourse";
        job.StartedAt ??= DateTime.UtcNow;
        job.TotalItems = totalPendingSteps;
        job.ProcessedItems = 0;
        job.FailedItems = 0;
        job.ProgressMessage = $"Bắt đầu tự động generate A->Z cho {lessonPairs.Count} lesson với {totalPendingSteps} bước.";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();

        var processed = 0;
        var failed = 0;
        var consecutiveSystemicAudioFailures = 0;

        for (var lessonIndex = 0; lessonIndex < lessonPairs.Count; lessonIndex++)
        {
            var (module, lesson) = lessonPairs[lessonIndex];

            var result = await ProcessLessonStepsAsync(
                course,
                module,
                lesson,
                job,
                lessonIndex + 1,
                lessonPairs.Count,
                processed,
                failed,
                totalPendingSteps,
                cancellationToken);

            processed += result.ProcessedSteps;
            job.ProcessedItems = processed;

            if (!result.Success)
            {
                failed++;
                job.FailedItems = failed;
                job.ProgressMessage = $"Bài {lessonIndex + 1}/{lessonPairs.Count} gặp lỗi ở bước {result.FailedStepLabel}. Tiếp tục lesson tiếp theo.";

                if (IsSystemicAudioFailure(result, lesson))
                {
                    consecutiveSystemicAudioFailures++;
                    if (consecutiveSystemicAudioFailures >= MaxConsecutiveSystemicAudioFailures)
                    {
                        job.Status = "Failed";
                        job.CompletedAt = DateTime.UtcNow;
                        job.ProgressMessage = "Dừng sớm vì dịch vụ audio gặp lỗi hệ thống liên tiếp.";
                        job.ErrorMessage = $"Dừng sớm sau {consecutiveSystemicAudioFailures} lỗi audio hệ thống liên tiếp. Lỗi gần nhất: {lesson.AudioGenerationError}";
                        job.UpdatedAt = DateTime.UtcNow;
                        await _generationJobRepository.SaveChangesAsync();
                        return;
                    }
                }
                else
                {
                    consecutiveSystemicAudioFailures = 0;
                }
            }
            else
            {
                job.FailedItems = failed;
                job.ProgressMessage = $"Đã hoàn tất lesson {lessonIndex + 1}/{lessonPairs.Count}: {lesson.Title}.";
                consecutiveSystemicAudioFailures = 0;
            }

            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();
        }

        var generatedFinalQuiz = false;
        if (course.Modules.SelectMany(module => module.Lessons).All(lesson => lesson.ContentGenerationStatus == "Completed"))
        {
            generatedFinalQuiz = await TryGenerateFinalQuizAsync(course.Id, cancellationToken);
        }

        var status = failed switch
        {
            0 => "Completed",
            _ when failed < lessonPairs.Count => "CompletedWithWarnings",
            _ => "Failed"
        };

        job.Status = status;
        job.CompletedAt = DateTime.UtcNow;
        job.ProgressMessage = status switch
        {
            "Completed" => generatedFinalQuiz ? "Đã hoàn tất quá trình generate tự động A->Z và tạo quiz tổng kết." : "Đã hoàn tất quá trình generate tự động A->Z.",
            "CompletedWithWarnings" => generatedFinalQuiz ? "Đã hoàn tất generate A->Z, có bài học lỗi, nhưng đã tạo quiz tổng kết." : "Đã hoàn tất generate A->Z nhưng có bài học lỗi.",
            _ => "Không thể generate A->Z cho các lesson."
        };
        job.ErrorMessage = failed > 0 ? $"Có {failed} lesson gặp lỗi trong quá trình generate." : null;
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();
    }

    private async Task<LessonStepProgressResult> ProcessLessonStepsAsync(
        Course course,
        Module module,
        Lesson lesson,
        GenerationJob job,
        int lessonNumber,
        int totalLessons,
        int processedStepsBeforeLesson,
        int failedLessonsBeforeLesson,
        int totalPendingSteps,
        CancellationToken cancellationToken)
    {
        var processedSteps = 0;

        // STEP 1: Content
        if (lesson.ContentGenerationStatus != "Completed")
        {
            await UpdateStepStartProgressAsync(job, lessonNumber, totalLessons, "nội dung", processedStepsBeforeLesson + processedSteps);
            lesson.ContentGenerationStatus = "Processing";
            lesson.ContentGenerationError = null;
            await _lessonRepository.SaveChangesAsync();

            try
            {
                await _lessonContentService.GenerateContentForLessonInternalAsync(course, module, lesson, cancellationToken);
                await _lessonRepository.SaveChangesAsync();
                processedSteps++;
                await UpdateStepCompletionProgressAsync(job, lessonNumber, totalLessons, "nội dung", processedStepsBeforeLesson + processedSteps, failedLessonsBeforeLesson, totalPendingSteps);
            }
            catch (Exception ex)
            {
                lesson.ContentGenerationStatus = "Failed";
                lesson.ContentGenerationError = ex.Message;
                lesson.ContentGeneratedAt = DateTime.UtcNow;
                await _lessonRepository.SaveChangesAsync();
                await UpdateStepFailureProgressAsync(job, lessonNumber, totalLessons, "nội dung", processedStepsBeforeLesson + processedSteps, failedLessonsBeforeLesson + 1, totalPendingSteps);
                return new LessonStepProgressResult(false, processedSteps, "nội dung");
            }
        }

        if (lesson.ContentGenerationStatus == "Completed")
        {
            await TryGenerateLessonQuizAsync(course.Id, lesson.Id, cancellationToken);
        }

        // STEP 2: Audio
        if (lesson.AudioGenerationStatus != "Completed")
        {
            await UpdateStepStartProgressAsync(job, lessonNumber, totalLessons, "audio", processedStepsBeforeLesson + processedSteps);
            lesson.AudioGenerationStatus = "GeneratingAudio";
            lesson.AudioGenerationError = null;
            await _lessonRepository.SaveChangesAsync();

            try
            {
                await _lessonAudioService.GenerateAudioForLessonInternalAsync(
                    lesson,
                    cancellationToken,
                    async (completedSegments, totalSegments) =>
                    {
                        job.ProgressMessage = $"Bài {lessonNumber}/{totalLessons} - đang tạo audio ({completedSegments}/{totalSegments} segment).";
                        job.UpdatedAt = DateTime.UtcNow;
                        await _generationJobRepository.SaveChangesAsync();
                    });
                await _lessonRepository.SaveChangesAsync();
                processedSteps++;
                await UpdateStepCompletionProgressAsync(job, lessonNumber, totalLessons, "audio", processedStepsBeforeLesson + processedSteps, failedLessonsBeforeLesson, totalPendingSteps);
            }
            catch (Exception ex)
            {
                lesson.AudioGenerationStatus = "Failed";
                lesson.AudioGenerationError = ex.Message;
                lesson.AudioGeneratedAt = DateTime.UtcNow;
                await _lessonRepository.SaveChangesAsync();
                await UpdateStepFailureProgressAsync(job, lessonNumber, totalLessons, "audio", processedStepsBeforeLesson + processedSteps, failedLessonsBeforeLesson + 1, totalPendingSteps);
                return new LessonStepProgressResult(false, processedSteps, "audio");
            }
        }

        // STEP 3: Video
        if (lesson.VideoGenerationStatus != "Completed")
        {
            await UpdateStepStartProgressAsync(job, lessonNumber, totalLessons, "video", processedStepsBeforeLesson + processedSteps);
            lesson.VideoGenerationStatus = "GeneratingVideo";
            lesson.VideoGenerationError = null;
            await _lessonRepository.SaveChangesAsync();

            try
            {
                await _lessonVideoService.GenerateVideoForLessonInternalAsync(lesson, cancellationToken);
                await _lessonRepository.SaveChangesAsync();
                processedSteps++;
                await UpdateStepCompletionProgressAsync(job, lessonNumber, totalLessons, "video", processedStepsBeforeLesson + processedSteps, failedLessonsBeforeLesson, totalPendingSteps);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                lesson.VideoGenerationStatus = "NotGenerated";
                lesson.VideoGenerationError = "Tiến trình tạo video đã bị hủy.";
                lesson.VideoGeneratedAt = null;
                lesson.UpdatedAt = DateTime.UtcNow;
                await _lessonRepository.SaveChangesAsync();
                throw;
            }
            catch (Exception ex)
            {
                lesson.VideoGenerationStatus = "Failed";
                lesson.VideoGenerationError = ex.Message;
                lesson.VideoGeneratedAt = DateTime.UtcNow;
                await _lessonRepository.SaveChangesAsync();
                await UpdateStepFailureProgressAsync(job, lessonNumber, totalLessons, "video", processedStepsBeforeLesson + processedSteps, failedLessonsBeforeLesson + 1, totalPendingSteps);
                return new LessonStepProgressResult(false, processedSteps, "video");
            }
        }

        return new LessonStepProgressResult(true, processedSteps, null);
    }

    private async Task TryGenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteQuizGenerationAsync(
                quizService => quizService.GenerateLessonQuizAsync(courseId, lessonId, cancellationToken));
        }
        catch
        {
            // Quiz generation is best-effort and should not interrupt full-course processing.
        }
    }

    private async Task<bool> TryGenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteQuizGenerationAsync(
                quizService => quizService.GenerateFinalQuizAsync(courseId, cancellationToken));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ExecuteQuizGenerationAsync(Func<IQuizGenerationService, Task> action)
    {
        if (_serviceScopeFactory is null)
        {
            await action(_quizGenerationService);
            return;
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var quizGenerationService = scope.ServiceProvider.GetRequiredService<IQuizGenerationService>();
        await action(quizGenerationService);
    }

    private async Task PersistFailedJobAsync(GenerationJob trackedJob, Exception exception, CancellationToken cancellationToken)
    {
        if (_serviceScopeFactory is null)
        {
            trackedJob.Status = "Failed";
            trackedJob.ErrorMessage = exception.Message;
            trackedJob.ProgressMessage = "Job generate full course kết thúc với lỗi hệ thống.";
            trackedJob.CompletedAt = DateTime.UtcNow;
            trackedJob.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();
            return;
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var generationJobRepository = scope.ServiceProvider.GetRequiredService<IGenerationJobRepository>();
        var persistedJob = await generationJobRepository.GetByIdAsync(trackedJob.Id);
        if (persistedJob is null)
        {
            return;
        }

        persistedJob.Status = "Failed";
        persistedJob.ErrorMessage = exception.Message;
        persistedJob.ProgressMessage = "Job generate full course kết thúc với lỗi hệ thống.";
        persistedJob.CompletedAt = DateTime.UtcNow;
        persistedJob.UpdatedAt = DateTime.UtcNow;
        await generationJobRepository.SaveChangesAsync();
    }

    private async Task UpdateStepStartProgressAsync(GenerationJob job, int lessonNumber, int totalLessons, string stepLabel, int processedSteps)
    {
        job.ProcessedItems = processedSteps;
        job.ProgressMessage = $"Bài {lessonNumber}/{totalLessons} - đang tạo {stepLabel}.";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();
    }

    private async Task UpdateStepCompletionProgressAsync(
        GenerationJob job,
        int lessonNumber,
        int totalLessons,
        string stepLabel,
        int processedSteps,
        int failedLessons,
        int totalPendingSteps)
    {
        job.ProcessedItems = processedSteps;
        job.FailedItems = failedLessons;
        job.ProgressMessage = $"Bài {lessonNumber}/{totalLessons} - đã hoàn tất {stepLabel} ({processedSteps}/{totalPendingSteps} bước).";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();
    }

    private async Task UpdateStepFailureProgressAsync(
        GenerationJob job,
        int lessonNumber,
        int totalLessons,
        string stepLabel,
        int processedSteps,
        int failedLessons,
        int totalPendingSteps)
    {
        job.ProcessedItems = processedSteps;
        job.FailedItems = failedLessons;
        job.ProgressMessage = $"Bài {lessonNumber}/{totalLessons} - lỗi khi tạo {stepLabel} ({processedSteps}/{totalPendingSteps} bước).";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();
    }

    private async Task<bool> HasRunningFullCourseJobAsync(Guid courseId)
    {
        var runningJobs = await _generationJobRepository.GetByCourseIdAsync(courseId);
        return runningJobs.Any(j => j.JobType == GenerateFullCourseJobType && (j.Status == "Pending" || j.Status == "GeneratingFullCourse"));
    }

    private static bool IsSystemicAudioFailure(LessonStepProgressResult result, Lesson lesson)
    {
        if (!string.Equals(result.FailedStepLabel, "audio", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var error = lesson.AudioGenerationError ?? string.Empty;
        return error.Contains("edge-tts", StringComparison.OrdinalIgnoreCase)
            || error.Contains("NoAudioReceived", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Timeout", StringComparison.OrdinalIgnoreCase);
    }

    private static int CountRemainingSteps(IReadOnlyList<(Module module, Lesson lesson)> lessonPairs)
    {
        return lessonPairs.Sum(pair => CountRemainingSteps(pair.lesson));
    }

    private static int CountRemainingSteps(Lesson lesson)
    {
        var remainingSteps = 0;
        if (lesson.ContentGenerationStatus != "Completed")
        {
            remainingSteps++;
        }

        if (lesson.AudioGenerationStatus != "Completed")
        {
            remainingSteps++;
        }

        if (lesson.VideoGenerationStatus != "Completed")
        {
            remainingSteps++;
        }

        return remainingSteps;
    }

    private static IReadOnlyList<(Module module, Lesson lesson)> GetEligibleLessons(Course course)
    {
        return course.Modules
            .OrderBy(m => m.OrderIndex)
            .SelectMany(m => m.Lessons
                .OrderBy(l => l.OrderIndex)
                .Where(l => l.ContentGenerationStatus != "Completed" || l.AudioGenerationStatus != "Completed" || l.VideoGenerationStatus != "Completed")
                .Select(l => (m, l)))
            .ToList();
    }

    private sealed record LessonStepProgressResult(bool Success, int ProcessedSteps, string? FailedStepLabel);
}
