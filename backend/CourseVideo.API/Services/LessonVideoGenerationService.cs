using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.VideoWorker;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;
using CourseVideo.API.Services.Video;

namespace CourseVideo.API.Services;

public class LessonVideoGenerationService : ILessonVideoGenerationService
{
    private const string GenerateCourseJobType = "GenerateLessonVideo";
    private const string GenerateLessonJobType = "RegenerateLessonVideo";
    private readonly ICourseRepository _courseRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IGenerationJobRepository _generationJobRepository;
    private readonly ILessonVideoJobQueue _lessonVideoJobQueue;
    private readonly ITimelineService _timelineService;
    private readonly IStorageService _storageService;
    private readonly IRenderService _renderService;
    private readonly IFFmpegService _ffmpegService;

    public LessonVideoGenerationService(
        ICourseRepository courseRepository,
        ILessonRepository lessonRepository,
        IGenerationJobRepository generationJobRepository,
        ILessonVideoJobQueue lessonVideoJobQueue,
        ITimelineService timelineService,
        IStorageService storageService,
        IRenderService renderService,
        IFFmpegService ffmpegService)
    {
        _courseRepository = courseRepository;
        _lessonRepository = lessonRepository;
        _generationJobRepository = generationJobRepository;
        _lessonVideoJobQueue = lessonVideoJobQueue;
        _timelineService = timelineService;
        _storageService = storageService;
        _renderService = renderService;
        _ffmpegService = ffmpegService;
    }

    public async Task<GenerateLessonVideoResponse> GenerateCourseVideoAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Không tìm thấy khóa học.");

        ValidateCourseCanGenerate(course);

        if (await _generationJobRepository.HasRunningLessonVideoJobForCourseAsync(courseId))
        {
            throw new InvalidOperationException("Khóa học này đang có job generate video chạy nền.");
        }

        var lessons = GetEligibleLessons(course);
        if (lessons.Count == 0)
        {
            throw new InvalidOperationException("Không còn lesson sẵn sàng để generate video.");
        }

        var job = new GenerationJob
        {
            SyllabusId = course.SyllabusId!.Value,
            CourseId = course.Id,
            CreatedByUserId = createdByUserId,
            JobType = GenerateCourseJobType,
            Status = "Pending",
            TotalItems = lessons.Count,
            ProcessedItems = 0,
            FailedItems = 0,
            ProgressMessage = $"Đã tạo job generate video cho {lessons.Count} lesson."
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();
        _lessonVideoJobQueue.Enqueue(job.Id);

        return MapQueuedResponse(job, courseId, lessons.Count);
    }

    public async Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default)
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

        LessonVideoValidation.ValidateReadyForVideo(lesson);

        if (await _generationJobRepository.HasRunningLessonVideoJobForCourseAsync(courseId))
        {
            throw new InvalidOperationException("Khóa học này đang có job generate video chạy nền.");
        }

        var job = new GenerationJob
        {
            SyllabusId = syllabusId,
            CourseId = courseId,
            LessonId = lesson.Id,
            CreatedByUserId = createdByUserId,
            JobType = GenerateLessonJobType,
            Status = "Pending",
            TotalItems = 1,
            ProcessedItems = 0,
            FailedItems = 0,
            ProgressMessage = $"Đã tạo job generate video cho lesson \"{lesson.Title}\"."
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();
        _lessonVideoJobQueue.Enqueue(job.Id);

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
            if (job.JobType == GenerateLessonJobType)
            {
                await ProcessSingleLessonJobAsync(job, cancellationToken);
                return;
            }

            await ProcessCourseJobAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            job.Status = "Failed";
            job.ErrorMessage = exception.Message;
            job.ProgressMessage = "Job generate video kết thúc với lỗi hệ thống.";
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
        var lessons = GetEligibleLessons(course);

        if (lessons.Count == 0)
        {
            job.Status = "Completed";
            job.ProgressMessage = "Không còn lesson sẵn sàng để generate video.";
            job.TotalItems = 0;
            job.ProcessedItems = 0;
            job.FailedItems = 0;
            job.StartedAt = DateTime.UtcNow;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();
            return;
        }

        job.Status = "GeneratingLessonVideo";
        job.StartedAt ??= DateTime.UtcNow;
        job.TotalItems = lessons.Count;
        job.ProcessedItems = 0;
        job.FailedItems = 0;
        job.ProgressMessage = $"Bắt đầu generate video cho {lessons.Count} lesson.";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();

        var processed = 0;
        var failed = 0;

        foreach (var lesson in lessons)
        {
            lesson.VideoGenerationStatus = "GeneratingVideo";
            lesson.VideoGenerationError = null;
            lesson.UpdatedAt = DateTime.UtcNow;
            job.ProgressMessage = $"Đang xử lý video lesson {processed + 1}/{lessons.Count}: {lesson.Title}";
            job.UpdatedAt = DateTime.UtcNow;
            await _lessonRepository.SaveChangesAsync();
            await _generationJobRepository.SaveChangesAsync();

            try
            {
                await GenerateVideoForLessonInternalAsync(lesson, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ResetVideoGenerationState(lesson, "Tiến trình tạo video đã bị hủy.");
                throw;
            }
            catch (Exception exception)
            {
                lesson.VideoGenerationStatus = "Failed";
                lesson.VideoGenerationError = exception.Message;
                lesson.VideoGeneratedAt = DateTime.UtcNow;
                lesson.UpdatedAt = DateTime.UtcNow;
                failed++;
            }

            processed++;
            job.ProcessedItems = processed;
            job.FailedItems = failed;
            job.ProgressMessage = $"Đã xử lý {processed}/{lessons.Count} lesson video.";
            job.UpdatedAt = DateTime.UtcNow;
            await _lessonRepository.SaveChangesAsync();
            await _generationJobRepository.SaveChangesAsync();
        }

        FinalizeCourseJob(job, lessons.Count, failed);
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

        LessonVideoValidation.ValidateReadyForVideo(lesson);

        lesson.VideoGenerationStatus = "GeneratingVideo";
        lesson.VideoGenerationError = null;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _lessonRepository.SaveChangesAsync();

        job.Status = "GeneratingLessonVideo";
        job.StartedAt ??= DateTime.UtcNow;
        job.TotalItems = 1;
        job.ProcessedItems = 0;
        job.FailedItems = 0;
        job.ProgressMessage = $"Đang generate video cho lesson: {lesson.Title}";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();

        try
        {
            await GenerateVideoForLessonInternalAsync(lesson, cancellationToken);
            job.Status = "Completed";
            job.ProgressMessage = "Đã generate video lesson thành công.";
            job.ErrorMessage = null;
            job.FailedItems = 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ResetVideoGenerationState(lesson, "Tiến trình tạo video đã bị hủy.");
            throw;
        }
        catch (Exception exception)
        {
            lesson.VideoGenerationStatus = "Failed";
            lesson.VideoGenerationError = exception.Message;
            lesson.VideoGeneratedAt = DateTime.UtcNow;
            lesson.UpdatedAt = DateTime.UtcNow;
            job.Status = "Failed";
            job.ProgressMessage = "Lesson vẫn generate video lỗi sau khi thử lại.";
            job.ErrorMessage = exception.Message;
            job.FailedItems = 1;
        }

        await _lessonRepository.SaveChangesAsync();

        job.ProcessedItems = 1;
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();
    }

    public async Task GenerateVideoForLessonInternalAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        LessonVideoValidation.ValidateReadyForVideo(lesson);

        var slides = _timelineService.ParseSlideOutlineJson(lesson.SlideOutlineJson ?? string.Empty);
        if (slides.Count == 0)
        {
            throw new InvalidOperationException("Lesson phải có ít nhất một slide để render video.");
        }

        var audioSegments = _timelineService.ParseAudioSegmentsJson(lesson.AudioSegmentsJson ?? string.Empty);
        var timeline = _timelineService.BuildSlideTimeline(audioSegments);
        var slideLookup = slides.ToDictionary(slide => slide.SlideNumber, slide => slide);
        var slidePaths = new List<string>();
        var durations = new List<double>();
        var framesDir = _storageService.BuildVideoFramesDir(lesson.Id.ToString());

        foreach (var item in timeline)
        {
            if (!slideLookup.TryGetValue(item.SlideNumber, out var slide))
            {
                throw new InvalidOperationException($"Không tìm thấy slide {item.SlideNumber} để khớp với audio segment.");
            }

            var slidePath = Path.Combine(framesDir, $"slide-{item.SlideNumber:D3}.png");
            await _renderService.RenderSlidePngAsync(slidePath, slide, cancellationToken);
            slidePaths.Add(slidePath);
            durations.Add(item.DurationSeconds);
        }

        var audioPath = _storageService.ResolveStoragePathFromUrl(lesson.AudioUrl ?? string.Empty);
        var finalPath = _storageService.BuildVideoOutputPath(lesson.Id.ToString());
        var totalDuration = await _ffmpegService.AssembleVideoAsync(slidePaths, durations, audioPath, finalPath, cancellationToken);

        lesson.VideoUrl = $"/storage/video/{Path.GetFileName(finalPath)}";
        lesson.Duration = totalDuration > 0
            ? (int)Math.Ceiling(totalDuration)
            : lesson.Duration;
        lesson.VideoGenerationStatus = "Completed";
        lesson.VideoGenerationError = null;
        lesson.VideoGeneratedAt = DateTime.UtcNow;
        lesson.UpdatedAt = DateTime.UtcNow;
    }

    private static IReadOnlyList<Lesson> GetEligibleLessons(Course course)
    {
        return course.Modules
            .OrderBy(module => module.OrderIndex)
            .SelectMany(module => module.Lessons
                .OrderBy(lesson => lesson.OrderIndex)
                .Where(IsLessonEligibleForVideo)
                .ToList())
            .ToList();
    }

    private static bool IsLessonEligibleForVideo(Lesson lesson)
    {
        try
        {
            LessonVideoValidation.ValidateReadyForVideo(lesson);
            return lesson.VideoGenerationStatus is "NotGenerated" or "Failed" or "GeneratingVideo";
        }
        catch
        {
            return false;
        }
    }

    private static void ValidateCourseCanGenerate(Course course)
    {
        if (!course.SyllabusId.HasValue)
        {
            throw new InvalidOperationException("Khóa học này chưa có syllabus nguồn để gắn generation job.");
        }
    }

    private static GenerateLessonVideoResponse MapQueuedResponse(GenerationJob job, Guid courseId, int totalLessons)
    {
        return new GenerateLessonVideoResponse
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
            "Completed" => "Đã generate video cho toàn bộ lesson cần xử lý.",
            "CompletedWithWarnings" => "Đã generate video nhưng vẫn còn lesson lỗi.",
            _ => "Không thể generate video cho các lesson trong khóa học."
        };
        job.ErrorMessage = failed > 0 ? $"Có {failed} lesson generate video lỗi." : null;
        job.UpdatedAt = DateTime.UtcNow;
    }

    private static void ResetVideoGenerationState(Lesson lesson, string message)
    {
        lesson.VideoGenerationStatus = "NotGenerated";
        lesson.VideoGenerationError = message;
        lesson.VideoGeneratedAt = null;
        lesson.UpdatedAt = DateTime.UtcNow;
    }
}
