using CourseVideo.API.DTOs.GenerationJobs;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class CourseGenerationService : ICourseGenerationService
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IGenerationJobRepository _generationJobRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IOpenRouterCourseStructureService _openRouterCourseStructureService;
    private readonly ICourseStructureParser _courseStructureParser;
    private readonly IJobCancellationTracker _cancellationTracker;

    public CourseGenerationService(
        ISyllabusRepository syllabusRepository,
        IGenerationJobRepository generationJobRepository,
        ICourseRepository courseRepository,
        IModuleRepository moduleRepository,
        ILessonRepository lessonRepository,
        IOpenRouterCourseStructureService openRouterCourseStructureService,
        ICourseStructureParser courseStructureParser,
        IJobCancellationTracker cancellationTracker)
    {
        _syllabusRepository = syllabusRepository;
        _generationJobRepository = generationJobRepository;
        _courseRepository = courseRepository;
        _moduleRepository = moduleRepository;
        _lessonRepository = lessonRepository;
        _openRouterCourseStructureService = openRouterCourseStructureService;
        _courseStructureParser = courseStructureParser;
        _cancellationTracker = cancellationTracker;
    }

    public async Task<GenerateCourseResponse> GenerateFromSyllabusAsync(Guid syllabusId, Guid createdByUserId, string createdByName)
    {
        var syllabus = await _syllabusRepository.GetEntityByIdAsync(syllabusId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        if (string.IsNullOrWhiteSpace(syllabus.ExtractedText))
        {
            throw new InvalidOperationException("Đề cương chưa có nội dung để generate khóa học.");
        }

        if (await _generationJobRepository.HasRunningJobForSyllabusAsync(syllabusId))
        {
            throw new InvalidOperationException("Đề cương này đang có job generate đang chạy.");
        }

        if (await _generationJobRepository.HasCompletedJobForSyllabusAsync(syllabusId))
        {
            throw new InvalidOperationException("Đề cương này đã được generate khóa học thành công rồi.");
        }

        var job = new GenerationJob
        {
            SyllabusId = syllabus.Id,
            CreatedByUserId = createdByUserId,
            Status = "Pending"
        };

        await _generationJobRepository.AddAsync(job);
        await _generationJobRepository.SaveChangesAsync();

        try
        {
            job.Status = "Processing";
            job.StartedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();

            var structure = await BuildStructureAsync(syllabus);
            var course = new Course
            {
                Title = ResolveCourseTitle(syllabus, structure),
                Description = ResolveCourseDescription(syllabus, structure),
                IsPublished = false,
                SyllabusId = syllabus.Id,
                CreatedByUserId = createdByUserId
            };

            await _courseRepository.AddAsync(course);
            await _courseRepository.SaveChangesAsync();

            var modules = structure.Modules
                .Select((module, moduleIndex) => new Module
                {
                    CourseId = course.Id,
                    Title = module.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(module.Description)
                        ? $"Module {moduleIndex + 1} duoc tao tu de cuong."
                        : module.Description.Trim(),
                    OrderIndex = moduleIndex + 1
                })
                .ToList();

            if (modules.Count > 0)
            {
                await _moduleRepository.AddRangeAsync(modules);
                await _moduleRepository.SaveChangesAsync();
            }

            var lessons = structure.Modules
                .SelectMany((module, moduleIndex) =>
                {
                    var moduleId = modules[moduleIndex].Id;
                    return module.Lessons.Select((lesson, lessonIndex) => new Lesson
                    {
                        ModuleId = moduleId,
                        Title = lesson.Title.Trim(),
                        Description = string.IsNullOrWhiteSpace(lesson.Description)
                            ? $"Bai hoc {lessonIndex + 1} duoc tao tu de cuong."
                            : lesson.Description.Trim(),
                        OrderIndex = lessonIndex + 1,
                        ContentSeed = lesson.ContentSeed.Trim()
                    });
                })
                .ToList();

            if (lessons.Count > 0)
            {
                await _lessonRepository.AddRangeAsync(lessons);
                await _lessonRepository.SaveChangesAsync();
            }

            job.CourseId = course.Id;
            job.Course = course;
            job.Status = "Completed";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();

            return new GenerateCourseResponse
            {
                JobId = job.Id,
                Status = job.Status,
                SyllabusId = syllabus.Id,
                CourseId = course.Id,
                CourseTitle = course.Title,
                CreatedAt = job.CreatedAt
            };
        }
        catch (Exception exception)
        {
            job.Status = "Failed";
            job.ErrorMessage = exception.Message;
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await _generationJobRepository.SaveChangesAsync();

            if (exception is OpenRouterConfigurationException or OpenRouterValidationException)
            {
                throw new InvalidOperationException(exception.Message, exception);
            }

            throw new InvalidOperationException("Không thể tạo khóa học từ đề cương.", exception);
        }
    }

    public async Task<IReadOnlyList<GenerationJobListItemResponse>> GetAllJobsAsync()
    {
        var jobs = await _generationJobRepository.GetAllAsync();
        return jobs.Select(MapListItem).ToList();
    }

    public async Task<GenerationJobDetailResponse?> GetJobByIdAsync(Guid id)
    {
        var job = await _generationJobRepository.GetByIdAsync(id);
        return job is null ? null : MapDetail(job);
    }

    public async Task CancelJobAsync(Guid id)
    {
        var job = await _generationJobRepository.GetByIdAsync(id);
        if (job is null)
        {
            throw new KeyNotFoundException("Không tìm thấy tiến trình.");
        }

        if (job.Status is "Completed" or "CompletedWithWarnings" or "Failed" or "Cancelled")
        {
            throw new InvalidOperationException($"Không thể hủy tiến trình đã kết thúc (trạng thái hiện tại: {job.Status}).");
        }

        _cancellationTracker.CancelJob(id);

        job.Status = "Cancelled";
        job.ProgressMessage = "Tiến trình đã bị hủy bởi người dùng.";
        job.CompletedAt = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;

        await _generationJobRepository.SaveChangesAsync();
    }

    private static string BuildCourseDescription(Syllabus syllabus)
    {
        if (!string.IsNullOrWhiteSpace(syllabus.Description))
        {
            return syllabus.Description.Trim();
        }

        var extractedText = syllabus.ExtractedText.Trim();
        if (extractedText.Length <= 280)
        {
            return extractedText;
        }

        return $"{extractedText[..280].Trim()}...";
    }

    private async Task<ParsedCourseStructure> BuildStructureAsync(Syllabus syllabus)
    {
        try
        {
            return await _openRouterCourseStructureService.GenerateStructureAsync(syllabus.ExtractedText);
        }
        catch (OpenRouterTechnicalException)
        {
            return _courseStructureParser.Parse(syllabus.ExtractedText);
        }
    }

    private static string ResolveCourseTitle(Syllabus syllabus, ParsedCourseStructure structure)
    {
        return string.IsNullOrWhiteSpace(structure.CourseTitle)
            ? syllabus.Title.Trim()
            : structure.CourseTitle.Trim();
    }

    private static string ResolveCourseDescription(Syllabus syllabus, ParsedCourseStructure structure)
    {
        return string.IsNullOrWhiteSpace(structure.CourseDescription)
            ? BuildCourseDescription(syllabus)
            : structure.CourseDescription.Trim();
    }

    private static GenerationJobListItemResponse MapListItem(GenerationJob job)
    {
        return new GenerationJobListItemResponse
        {
            Id = job.Id,
            SyllabusId = job.SyllabusId,
            SyllabusTitle = job.Syllabus?.Title ?? string.Empty,
            CourseId = job.CourseId,
            LessonId = job.LessonId,
            CourseTitle = job.Course?.Title ?? string.Empty,
            JobType = job.JobType ?? string.Empty,
            Status = job.Status,
            ErrorMessage = job.ErrorMessage ?? string.Empty,
            TotalItems = job.TotalItems ?? 0,
            ProcessedItems = job.ProcessedItems ?? 0,
            FailedItems = job.FailedItems ?? 0,
            ProgressMessage = job.ProgressMessage ?? string.Empty,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };
    }

    private static GenerationJobDetailResponse MapDetail(GenerationJob job)
    {
        return new GenerationJobDetailResponse
        {
            Id = job.Id,
            SyllabusId = job.SyllabusId,
            CourseId = job.CourseId,
            LessonId = job.LessonId,
            SyllabusTitle = job.Syllabus?.Title ?? string.Empty,
            CourseTitle = job.Course?.Title ?? string.Empty,
            JobType = job.JobType ?? string.Empty,
            Status = job.Status,
            ErrorMessage = job.ErrorMessage ?? string.Empty,
            TotalItems = job.TotalItems ?? 0,
            ProcessedItems = job.ProcessedItems ?? 0,
            FailedItems = job.FailedItems ?? 0,
            ProgressMessage = job.ProgressMessage ?? string.Empty,
            CreatedByUserId = job.CreatedByUserId,
            CreatedByName = job.CreatedByUser?.FullName ?? string.Empty,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };
    }
}
