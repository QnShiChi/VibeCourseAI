using System.Text.Json;
using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Models;
using CourseVideo.API.Models.OpenRouter;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonContentGenerationService : ILessonContentGenerationService
{
    private const string GenerateCourseJobType = "GenerateLessonContent";
    private const string RegenerateLessonJobType = "RegenerateLessonContent";
    private readonly ICourseRepository _courseRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IGenerationJobRepository _generationJobRepository;
    private readonly IOpenRouterLessonContentService _openRouterLessonContentService;
    private readonly IGenerationJobQueue _generationJobQueue;

    public LessonContentGenerationService(
        ICourseRepository courseRepository,
        ILessonRepository lessonRepository,
        IGenerationJobRepository generationJobRepository,
        IOpenRouterLessonContentService openRouterLessonContentService,
        IGenerationJobQueue generationJobQueue)
    {
        _courseRepository = courseRepository;
        _lessonRepository = lessonRepository;
        _generationJobRepository = generationJobRepository;
        _openRouterLessonContentService = openRouterLessonContentService;
        _generationJobQueue = generationJobQueue;
    }

    public async Task<GenerateLessonContentResponse> GenerateCourseContentAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Không tìm thấy khóa học.");

        ValidateCourseCanGenerate(course);

        if (await _generationJobRepository.HasRunningLessonContentJobForCourseAsync(courseId))
        {
            throw new InvalidOperationException("Khóa học này đang có job generate nội dung lesson chạy nền.");
        }

        var lessonPairs = GetEligibleLessonPairs(course);
        if (lessonPairs.Count == 0)
        {
            throw new InvalidOperationException("Không còn lesson cần generate nội dung. Chỉ những lesson chưa generate hoặc đang lỗi mới được đưa vào job mới.");
        }

        var job = new GenerationJob
        {
            SyllabusId = course.SyllabusId!.Value,
            CourseId = course.Id,
            CreatedByUserId = createdByUserId,
            JobType = GenerateCourseJobType,
            Status = "Pending",
            TotalItems = lessonPairs.Count,
            ProcessedItems = 0,
            FailedItems = 0,
            ProgressMessage = $"Đã tạo job generate {lessonPairs.Count} lesson."
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();
        _generationJobQueue.Enqueue(job.Id);

        return MapQueuedResponse(job, course.Id, lessonPairs.Count);
    }

    public async Task<GenerateLessonContentResponse> RegenerateLessonContentAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(lessonId)
            ?? throw new KeyNotFoundException("Không tìm thấy lesson.");

        if (lesson.Module?.CourseId != courseId)
        {
            throw new InvalidOperationException("Lesson này không thuộc khóa học đã chọn.");
        }

        if (lesson.Module.Course?.SyllabusId is not Guid syllabusId)
        {
            throw new InvalidOperationException("Khóa học này chưa có syllabus nguồn để gắn generation job.");
        }

        if (lesson.ContentGenerationStatus != "Failed")
        {
            throw new InvalidOperationException("Chỉ có thể generate lại lesson đang ở trạng thái lỗi.");
        }

        if (await _generationJobRepository.HasRunningLessonContentJobForCourseAsync(courseId))
        {
            throw new InvalidOperationException("Khóa học này đang có job generate nội dung lesson chạy nền.");
        }

        var job = new GenerationJob
        {
            SyllabusId = syllabusId,
            CourseId = courseId,
            LessonId = lesson.Id,
            CreatedByUserId = createdByUserId,
            JobType = RegenerateLessonJobType,
            Status = "Pending",
            TotalItems = 1,
            ProcessedItems = 0,
            FailedItems = 0,
            ProgressMessage = $"Đã tạo job generate lại lesson \"{lesson.Title}\"."
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();
        _generationJobQueue.Enqueue(job.Id);

        return MapQueuedResponse(job, courseId, 1);
    }

    public async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _generationJobRepository.GetByIdAsync(jobId);
        if (job is null)
        {
            return;
        }

        try
        {
            if (job.JobType == RegenerateLessonJobType)
            {
                await ProcessSingleLessonJobAsync(job, cancellationToken);
                return;
            }

            await ProcessCourseJobAsync(job, cancellationToken);
        }
        catch (Exception exception)
        {
            job.Status = "Failed";
            job.ErrorMessage = exception.Message;
            job.ProgressMessage = "Job generate nội dung lesson kết thúc với lỗi hệ thống.";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();
        }
    }

    private async Task ProcessCourseJobAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        if (!job.CourseId.HasValue)
        {
            throw new InvalidOperationException("Generation job không có course để xử lý.");
        }

        var course = await _courseRepository.GetByIdWithStructureAsync(job.CourseId.Value)
            ?? throw new KeyNotFoundException("Không tìm thấy khóa học cho generation job.");

        ValidateCourseCanGenerate(course);

        var lessonPairs = GetEligibleLessonPairs(course);
        if (lessonPairs.Count == 0)
        {
            job.Status = "Completed";
            job.TotalItems = 0;
            job.ProcessedItems = 0;
            job.FailedItems = 0;
            job.StartedAt = DateTime.UtcNow;
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressMessage = "Không còn lesson cần generate.";
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();
            return;
        }

        job.Status = "GeneratingLessonContent";
        job.StartedAt ??= DateTime.UtcNow;
        job.TotalItems = lessonPairs.Count;
        job.ProcessedItems = 0;
        job.FailedItems = 0;
        job.ProgressMessage = $"Bắt đầu generate {lessonPairs.Count} lesson.";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();

        var failed = 0;
        var processed = 0;

        foreach (var (module, lesson) in lessonPairs)
        {
            lesson.ContentGenerationStatus = "Processing";
            lesson.ContentGenerationError = null;
            lesson.UpdatedAt = DateTime.UtcNow;
            job.ProgressMessage = $"Đang xử lý lesson {processed + 1}/{lessonPairs.Count}: {lesson.Title}";
            job.UpdatedAt = DateTime.UtcNow;
            await _lessonRepository.SaveChangesAsync();
            await _generationJobRepository.SaveChangesAsync();

            try
            {
                var result = await _openRouterLessonContentService.GenerateAsync(course, module, lesson, cancellationToken);
                ApplyResult(lesson, result);
            }
            catch (Exception exception) when (exception is LessonContentGenerationException or OpenRouterConfigurationException or OpenRouterValidationException or OpenRouterTechnicalException)
            {
                lesson.ContentGenerationStatus = "Failed";
                lesson.ContentGenerationError = exception.Message;
                lesson.ContentGeneratedAt = DateTime.UtcNow;
                failed++;
            }

            lesson.UpdatedAt = DateTime.UtcNow;
            processed++;
            job.ProcessedItems = processed;
            job.FailedItems = failed;
            job.ProgressMessage = $"Đã xử lý {processed}/{lessonPairs.Count} lesson.";
            job.UpdatedAt = DateTime.UtcNow;
            await _lessonRepository.SaveChangesAsync();
            await _generationJobRepository.SaveChangesAsync();
        }

        FinalizeCourseJob(job, lessonPairs.Count, failed);
        await _generationJobRepository.SaveChangesAsync();
    }

    private async Task ProcessSingleLessonJobAsync(GenerationJob job, CancellationToken cancellationToken)
    {
        if (!job.LessonId.HasValue)
        {
            throw new InvalidOperationException("Generation job không có lesson để xử lý.");
        }

        var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(job.LessonId.Value)
            ?? throw new KeyNotFoundException("Không tìm thấy lesson cho generation job.");

        var module = lesson.Module ?? throw new InvalidOperationException("Lesson không có module đi kèm.");
        var course = module.Course ?? throw new InvalidOperationException("Lesson không có course đi kèm.");
        ValidateCourseCanGenerate(course);

        lesson.ContentGenerationStatus = "Processing";
        lesson.ContentGenerationError = null;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _lessonRepository.SaveChangesAsync();

        job.Status = "RegeneratingLessonContent";
        job.StartedAt ??= DateTime.UtcNow;
        job.TotalItems = 1;
        job.ProcessedItems = 0;
        job.FailedItems = 0;
        job.ProgressMessage = $"Đang generate lại lesson: {lesson.Title}";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();

        try
        {
            var result = await _openRouterLessonContentService.GenerateAsync(course, module, lesson, cancellationToken);
            ApplyResult(lesson, result);
            job.Status = "Completed";
            job.ProgressMessage = "Đã generate lại lesson thành công.";
            job.ErrorMessage = null;
            job.FailedItems = 0;
        }
        catch (Exception exception) when (exception is LessonContentGenerationException or OpenRouterConfigurationException or OpenRouterValidationException or OpenRouterTechnicalException)
        {
            lesson.ContentGenerationStatus = "Failed";
            lesson.ContentGenerationError = exception.Message;
            lesson.ContentGeneratedAt = DateTime.UtcNow;
            job.Status = "Failed";
            job.ProgressMessage = "Lesson vẫn generate lỗi sau khi thử lại.";
            job.ErrorMessage = exception.Message;
            job.FailedItems = 1;
        }

        lesson.UpdatedAt = DateTime.UtcNow;
        await _lessonRepository.SaveChangesAsync();

        job.ProcessedItems = 1;
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();
    }

    private static IReadOnlyList<(Module module, Lesson lesson)> GetEligibleLessonPairs(Course course)
    {
        return course.Modules
            .OrderBy(module => module.OrderIndex)
            .SelectMany(module => module.Lessons
                .OrderBy(lesson => lesson.OrderIndex)
                .Where(lesson => lesson.ContentGenerationStatus is "NotGenerated" or "Failed" or "Processing")
                .Select(lesson => (module, lesson)))
            .ToList();
    }

    private static void ValidateCourseCanGenerate(Course course)
    {
        if (!course.SyllabusId.HasValue)
        {
            throw new InvalidOperationException("Khóa học này chưa có syllabus nguồn để gắn generation job.");
        }
    }

    private static GenerateLessonContentResponse MapQueuedResponse(GenerationJob job, Guid courseId, int totalLessons)
    {
        return new GenerateLessonContentResponse
        {
            JobId = job.Id,
            CourseId = courseId,
            Status = job.Status,
            TotalLessons = totalLessons,
            ProcessedLessons = 0,
            FailedLessons = 0,
            Message = job.ProgressMessage ?? string.Empty
        };
    }

    private static void FinalizeCourseJob(GenerationJob job, int totalLessons, int failed)
    {
        var status = failed switch
        {
            0 => "Completed",
            _ when failed < totalLessons => "CompletedWithWarnings",
            _ => "Failed"
        };

        job.Status = status;
        job.CompletedAt = DateTime.UtcNow;
        job.ProgressMessage = status switch
        {
            "Completed" => "Đã generate nội dung cho toàn bộ lesson cần xử lý.",
            "CompletedWithWarnings" => "Đã generate nội dung nhưng vẫn còn lesson lỗi.",
            _ => "Không thể generate nội dung cho các lesson trong khóa học."
        };
        job.ErrorMessage = failed > 0 ? $"Có {failed} lesson generate lỗi." : null;
        job.UpdatedAt = DateTime.UtcNow;
    }

    private static void ApplyResult(Lesson lesson, OpenRouterLessonContentResult result)
    {
        lesson.TeachingScript = result.TeachingScript.Trim();
        lesson.SlideOutlineJson = JsonSerializer.Serialize(result.SlideOutline);
        lesson.VoiceoverPlanJson = JsonSerializer.Serialize(result.VoiceoverPlan);
        lesson.ContentGenerationStatus = "Completed";
        lesson.ContentGenerationError = null;
        lesson.ContentGeneratedAt = DateTime.UtcNow;
        lesson.AudioUrl = null;
        lesson.AudioSegmentsJson = null;
        lesson.AudioGenerationStatus = "NotGenerated";
        lesson.AudioGenerationError = null;
        lesson.AudioGeneratedAt = null;
        lesson.VideoUrl = null;
        lesson.VideoGenerationStatus = "NotGenerated";
        lesson.VideoGenerationError = null;
        lesson.VideoGeneratedAt = null;
        lesson.Duration = null;
    }
}
