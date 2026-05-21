using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly ILessonContentGenerationService _lessonContentGenerationService;
    private readonly ILessonAudioGenerationService _lessonAudioGenerationService;
    private readonly ILessonVideoGenerationService _lessonVideoGenerationService;

    public CourseService(
        ICourseRepository courseRepository,
        ILessonContentGenerationService lessonContentGenerationService,
        ILessonAudioGenerationService lessonAudioGenerationService,
        ILessonVideoGenerationService lessonVideoGenerationService)
    {
        _courseRepository = courseRepository;
        _lessonContentGenerationService = lessonContentGenerationService;
        _lessonAudioGenerationService = lessonAudioGenerationService;
        _lessonVideoGenerationService = lessonVideoGenerationService;
    }

    public async Task<IReadOnlyList<CourseResponse>> GetAllAsync()
    {
        var courses = await _courseRepository.GetAllAsync();
        return courses.Select(course => new CourseResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminCourseListItemResponse>> GetAdminCoursesAsync()
    {
        var courses = await _courseRepository.GetAdminCoursesAsync();
        return courses.Select(MapAdminListItem).ToList();
    }

    public async Task<IReadOnlyList<PublishedCourseListItemResponse>> GetPublishedCoursesAsync()
    {
        var courses = await _courseRepository.GetPublishedAsync();
        return courses.Select(course => new PublishedCourseListItemResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            IsPublished = course.IsPublished,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.Sum(module => module.Lessons.Count),
            CreatedAt = course.CreatedAt
        }).ToList();
    }

    public async Task<AdminCourseListItemResponse?> PublishAsync(Guid id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course is null)
        {
            return null;
        }

        course.IsPublished = true;
        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepository.SaveChangesAsync();
        return MapAdminListItem(course);
    }

    public async Task<AdminCourseListItemResponse?> UnpublishAsync(Guid id)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course is null)
        {
            return null;
        }

        course.IsPublished = false;
        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepository.SaveChangesAsync();
        return MapAdminListItem(course);
    }

    public Task<GenerateLessonContentResponse> GenerateLessonContentAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        return _lessonContentGenerationService.GenerateCourseContentAsync(id, createdByUserId, cancellationToken);
    }

    public Task<GenerateLessonContentResponse> RegenerateLessonContentAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        return _lessonContentGenerationService.RegenerateLessonContentAsync(courseId, lessonId, createdByUserId, cancellationToken);
    }

    public Task<GenerateLessonAudioResponse> GenerateLessonAudioAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        return _lessonAudioGenerationService.GenerateCourseAudioAsync(id, createdByUserId, cancellationToken);
    }

    public Task<GenerateLessonAudioResponse> RegenerateLessonAudioAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        return _lessonAudioGenerationService.GenerateLessonAudioAsync(courseId, lessonId, createdByUserId, cancellationToken);
    }

    public Task<GenerateLessonVideoResponse> GenerateLessonVideoAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        return _lessonVideoGenerationService.GenerateCourseVideoAsync(id, createdByUserId, cancellationToken);
    }

    public Task<GenerateLessonVideoResponse> RegenerateLessonVideoAsync(Guid courseId, Guid lessonId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        return _lessonVideoGenerationService.GenerateLessonVideoAsync(courseId, lessonId, createdByUserId, cancellationToken);
    }

    public async Task<CourseLearnResponse?> GetLearnPayloadAsync(Guid id, bool canPreviewDraft)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(id);
        if (course is null)
        {
            return null;
        }

        if (!course.IsPublished && !canPreviewDraft)
        {
            return null;
        }

        var modules = course.Modules
            .OrderBy(module => module.OrderIndex)
            .Select(module => new CourseLearnModuleResponse
            {
                ModuleId = module.Id,
                ModuleTitle = module.Title,
                ModuleDescription = module.Description,
                OrderIndex = module.OrderIndex,
                Lessons = module.Lessons
                    .OrderBy(lesson => lesson.OrderIndex)
                    .Select(MapLearnLesson)
                    .ToList()
            })
            .ToList();

        var selectedLesson = modules
            .SelectMany(module => module.Lessons)
            .OrderBy(lesson => lesson.OrderIndex)
            .FirstOrDefault();

        return new CourseLearnResponse
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            CourseDescription = course.Description,
            IsPublished = course.IsPublished,
            SelectedLessonId = selectedLesson?.LessonId,
            SelectedLesson = selectedLesson,
            Modules = modules
        };
    }

    public async Task<CourseStructureResponse?> GetStructureAsync(Guid id)
    {
        var course = await _courseRepository.GetByIdWithStructureAsync(id);
        if (course is null)
        {
            return null;
        }

        return new CourseStructureResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt,
            Modules = course.Modules
                .OrderBy(module => module.OrderIndex)
                .Select(module => new ModuleStructureResponse
                {
                    Id = module.Id,
                    Title = module.Title,
                    Description = module.Description,
                    OrderIndex = module.OrderIndex,
                    Lessons = module.Lessons
                        .OrderBy(lesson => lesson.OrderIndex)
                        .Select(lesson => new LessonStructureResponse
                        {
                            Id = lesson.Id,
                            Title = lesson.Title,
                            Description = lesson.Description,
                            OrderIndex = lesson.OrderIndex,
                            ContentSeed = lesson.ContentSeed,
                            ContentGenerationStatus = lesson.ContentGenerationStatus,
                            ContentGenerationError = lesson.ContentGenerationError ?? string.Empty,
                            AudioGenerationStatus = lesson.AudioGenerationStatus,
                            AudioGenerationError = lesson.AudioGenerationError ?? string.Empty,
                            AudioUrl = lesson.AudioUrl ?? string.Empty,
                            VideoGenerationStatus = lesson.VideoGenerationStatus,
                            VideoGenerationError = lesson.VideoGenerationError ?? string.Empty,
                            VideoUrl = lesson.VideoUrl ?? string.Empty
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static AdminCourseListItemResponse MapAdminListItem(Course course)
    {
        return new AdminCourseListItemResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            IsPublished = course.IsPublished,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.Sum(module => module.Lessons.Count),
            CreatedAt = course.CreatedAt
        };
    }

    private static CourseLearnLessonResponse MapLearnLesson(Lesson lesson)
    {
        return new CourseLearnLessonResponse
        {
            LessonId = lesson.Id,
            LessonTitle = lesson.Title,
            Description = lesson.Description,
            OrderIndex = lesson.OrderIndex,
            ContentSeed = lesson.ContentSeed,
            VideoUrl = lesson.VideoUrl,
            VideoGenerationStatus = lesson.VideoGenerationStatus,
            VideoGenerationError = lesson.VideoGenerationError ?? string.Empty,
            Duration = lesson.Duration
        };
    }
}
