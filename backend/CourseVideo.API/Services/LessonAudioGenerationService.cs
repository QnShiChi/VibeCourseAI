using System.Text.Json;
using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.AudioWorker;
using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Audio;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonAudioGenerationService : ILessonAudioGenerationService
{
    private const string GenerateCourseJobType = "GenerateLessonAudio";
    private const string GenerateLessonJobType = "RegenerateLessonAudio";
    private readonly ICourseRepository _courseRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IGenerationJobRepository _generationJobRepository;
    private readonly ILessonAudioJobQueue _lessonAudioJobQueue;
    private readonly INarrationService _narrationService;
    private readonly IAudioPipelineService _audioPipelineService;

    public LessonAudioGenerationService(
        ICourseRepository courseRepository,
        ILessonRepository lessonRepository,
        IGenerationJobRepository generationJobRepository,
        ILessonAudioJobQueue lessonAudioJobQueue,
        INarrationService narrationService,
        IAudioPipelineService audioPipelineService)
    {
        _courseRepository = courseRepository;
        _lessonRepository = lessonRepository;
        _generationJobRepository = generationJobRepository;
        _lessonAudioJobQueue = lessonAudioJobQueue;
        _narrationService = narrationService;
        _audioPipelineService = audioPipelineService;
    }

    public async Task<GenerateLessonAudioResponse> GenerateCourseAudioAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(courseId)
            ?? throw new KeyNotFoundException("Không tìm thấy khóa học.");

        ValidateCourseCanGenerate(course);

        if (await _generationJobRepository.HasRunningLessonAudioJobForCourseAsync(courseId))
        {
            throw new InvalidOperationException("Khóa học này đang có job generate audio chạy nền.");
        }

        var lessonPairs = GetEligibleLessonPairs(course);
        if (lessonPairs.Count == 0)
        {
            throw new InvalidOperationException("Không còn lesson sẵn sàng để generate audio.");
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
            ProgressMessage = $"Đã tạo job generate audio cho {lessonPairs.Count} lesson."
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();
        _lessonAudioJobQueue.Enqueue(job.Id);

        return MapQueuedResponse(job, courseId, lessonPairs.Count);
    }

    public async Task<GenerateLessonAudioResponse> GenerateLessonAudioAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default)
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

        LessonAudioValidation.ValidateReadyForAudio(lesson);

        if (await _generationJobRepository.HasRunningLessonAudioJobForCourseAsync(courseId))
        {
            throw new InvalidOperationException("Khóa học này đang có job generate audio chạy nền.");
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
            ProgressMessage = $"Đã tạo job generate audio cho lesson \"{lesson.Title}\"."
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();
        _lessonAudioJobQueue.Enqueue(job.Id);

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
        catch (Exception exception)
        {
            job.Status = "Failed";
            job.ErrorMessage = exception.Message;
            job.ProgressMessage = "Job generate audio kết thúc với lỗi hệ thống.";
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
            job.ProgressMessage = "Không còn lesson sẵn sàng để generate audio.";
            job.TotalItems = 0;
            job.ProcessedItems = 0;
            job.FailedItems = 0;
            job.StartedAt = DateTime.UtcNow;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();
            return;
        }

        job.Status = "GeneratingLessonAudio";
        job.StartedAt ??= DateTime.UtcNow;
        job.TotalItems = lessonPairs.Count;
        job.ProcessedItems = 0;
        job.FailedItems = 0;
        job.ProgressMessage = $"Bắt đầu generate audio cho {lessonPairs.Count} lesson.";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();

        var processed = 0;
        var failed = 0;

        foreach (var lesson in lessonPairs)
        {
            lesson.AudioGenerationStatus = "GeneratingAudio";
            lesson.AudioGenerationError = null;
            lesson.UpdatedAt = DateTime.UtcNow;
            job.ProgressMessage = $"Đang xử lý audio lesson {processed + 1}/{lessonPairs.Count}: {lesson.Title}";
            job.UpdatedAt = DateTime.UtcNow;
            await _lessonRepository.SaveChangesAsync();
            await _generationJobRepository.SaveChangesAsync();

            try
            {
                await GenerateAudioForLessonInternalAsync(lesson, cancellationToken);
            }
            catch (Exception exception)
            {
                lesson.AudioGenerationStatus = "Failed";
                lesson.AudioGenerationError = exception.Message;
                lesson.AudioGeneratedAt = DateTime.UtcNow;
                lesson.UpdatedAt = DateTime.UtcNow;
                failed++;
            }

            processed++;
            job.ProcessedItems = processed;
            job.FailedItems = failed;
            job.ProgressMessage = $"Đã xử lý {processed}/{lessonPairs.Count} lesson audio.";
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

        LessonAudioValidation.ValidateReadyForAudio(lesson);

        lesson.AudioGenerationStatus = "GeneratingAudio";
        lesson.AudioGenerationError = null;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _lessonRepository.SaveChangesAsync();

        job.Status = "GeneratingLessonAudio";
        job.StartedAt ??= DateTime.UtcNow;
        job.TotalItems = 1;
        job.ProcessedItems = 0;
        job.FailedItems = 0;
        job.ProgressMessage = $"Đang generate audio cho lesson: {lesson.Title}";
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();

        try
        {
            await GenerateAudioForLessonInternalAsync(lesson, cancellationToken);
            job.Status = "Completed";
            job.ProgressMessage = "Đã generate audio lesson thành công.";
            job.ErrorMessage = null;
            job.FailedItems = 0;
        }
        catch (Exception exception)
        {
            lesson.AudioGenerationStatus = "Failed";
            lesson.AudioGenerationError = exception.Message;
            lesson.AudioGeneratedAt = DateTime.UtcNow;
            lesson.UpdatedAt = DateTime.UtcNow;
            job.Status = "Failed";
            job.ProgressMessage = "Lesson vẫn generate audio lỗi sau khi thử lại.";
            job.ErrorMessage = exception.Message;
            job.FailedItems = 1;
        }

        await _lessonRepository.SaveChangesAsync();

        job.ProcessedItems = 1;
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await _generationJobRepository.SaveChangesAsync();
    }

    public async Task GenerateAudioForLessonInternalAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        LessonAudioValidation.ValidateReadyForAudio(lesson);

        var narrationSegments = _narrationService.BuildNarrationSegments(
            lesson.TeachingScript ?? string.Empty,
            lesson.SlideOutlineJson ?? string.Empty,
            lesson.VoiceoverPlanJson ?? string.Empty);

        if (narrationSegments.Count == 0)
        {
            throw new InvalidOperationException("Lesson phải có ít nhất một slide để render audio.");
        }

        var workerResponse = await _audioPipelineService.GenerateLessonAudioAsync(lesson.Id, narrationSegments, cancellationToken);

        if (workerResponse is null)
        {
            throw new InvalidOperationException("Audio pipeline trả về dữ liệu audio rỗng.");
        }

        lesson.AudioUrl = workerResponse.AudioUrl;
        lesson.AudioSegmentsJson = JsonSerializer.Serialize(workerResponse.Segments.Select(segment => new LessonAudioSegmentResponse
        {
            SlideNumber = segment.SlideNumber,
            Title = segment.Title,
            NarrationText = segment.NarrationText,
            AudioUrl = segment.AudioUrl,
            DurationSeconds = segment.DurationSeconds
        }).ToList());
        lesson.VideoUrl = null;
        lesson.VideoGenerationStatus = "NotGenerated";
        lesson.VideoGenerationError = null;
        lesson.VideoGeneratedAt = null;
        lesson.Duration = workerResponse.DurationSeconds > 0
            ? (int)Math.Ceiling(workerResponse.DurationSeconds)
            : lesson.Duration;
        lesson.AudioGenerationStatus = "Completed";
        lesson.AudioGenerationError = null;
        lesson.AudioGeneratedAt = DateTime.UtcNow;
        lesson.UpdatedAt = DateTime.UtcNow;
    }

    private static IReadOnlyList<Lesson> GetEligibleLessonPairs(Course course)
    {
        return course.Modules
            .OrderBy(module => module.OrderIndex)
            .SelectMany(module => module.Lessons
                .OrderBy(lesson => lesson.OrderIndex)
                .Where(IsLessonEligibleForAudio)
                .ToList())
            .ToList();
    }

    private static bool IsLessonEligibleForAudio(Lesson lesson)
    {
        try
        {
            LessonAudioValidation.ValidateReadyForAudio(lesson);
            return lesson.AudioGenerationStatus is "NotGenerated" or "Failed" or "GeneratingAudio";
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

    private static GenerateLessonAudioResponse MapQueuedResponse(GenerationJob job, Guid courseId, int totalLessons)
    {
        return new GenerateLessonAudioResponse
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
            "Completed" => "Đã generate audio cho toàn bộ lesson cần xử lý.",
            "CompletedWithWarnings" => "Đã generate audio nhưng vẫn còn lesson lỗi.",
            _ => "Không thể generate audio cho các lesson trong khóa học."
        };
        job.ErrorMessage = failed > 0 ? $"Có {failed} lesson generate audio lỗi." : null;
        job.UpdatedAt = DateTime.UtcNow;
    }
}
