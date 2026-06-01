using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CourseVideo.API.Services;

public class CourseService : ICourseService
{
    private static readonly HashSet<string> AllowedThumbnailExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private readonly ICourseRepository _courseRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILessonContentGenerationService _lessonContentGenerationService;
    private readonly ILessonAudioGenerationService _lessonAudioGenerationService;
    private readonly ILessonVideoGenerationService _lessonVideoGenerationService;
    private readonly IFullCourseGenerationService _fullCourseGenerationService;
    private readonly IQuizGenerationService _quizGenerationService;
    private readonly IWebHostEnvironment _environment;

    public CourseService(
        ICourseRepository courseRepository,
        ICategoryRepository categoryRepository,
        ILessonContentGenerationService lessonContentGenerationService,
        ILessonAudioGenerationService lessonAudioGenerationService,
        ILessonVideoGenerationService lessonVideoGenerationService,
        IFullCourseGenerationService fullCourseGenerationService,
        IQuizGenerationService quizGenerationService,
        IWebHostEnvironment environment)
    {
        _courseRepository = courseRepository;
        _categoryRepository = categoryRepository;
        _lessonContentGenerationService = lessonContentGenerationService;
        _lessonAudioGenerationService = lessonAudioGenerationService;
        _lessonVideoGenerationService = lessonVideoGenerationService;
        _fullCourseGenerationService = fullCourseGenerationService;
        _quizGenerationService = quizGenerationService;
        _environment = environment;
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
        return courses.Select(MapPublishedListItem).ToList();
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

    public async Task<CourseStructureResponse?> UpdateCategoryAsync(Guid id, Guid categoryId)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course is null)
        {
            return null;
        }

        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category is null)
        {
            throw new InvalidOperationException("Category khóa học không hợp lệ.");
        }

        if (category.Status != CategoryStatus.Visible)
        {
            throw new InvalidOperationException("Chỉ có thể gán category đang hiển thị cho khóa học.");
        }

        course.CategoryId = category.Id;
        course.Category = category;
        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepository.SaveChangesAsync();
        return await GetStructureAsync(id);
    }

    public async Task<CourseStructureResponse?> UploadThumbnailAsync(Guid id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course is null)
        {
            return null;
        }

        if (file.Length == 0)
        {
            throw new InvalidOperationException("Vui lòng chọn ảnh thumbnail hợp lệ.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedThumbnailExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Chỉ hỗ trợ ảnh PNG, JPG, JPEG hoặc WEBP.");
        }

        var storageDirectory = Path.Combine(_environment.ContentRootPath, "storage", "course-thumbnails");
        Directory.CreateDirectory(storageDirectory);

        var storedFileName = $"{course.Id:N}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(storageDirectory, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
        }

        course.ThumbnailUrl = Path.Combine("/storage", "course-thumbnails", storedFileName).Replace("\\", "/");
        course.UpdatedAt = DateTime.UtcNow;
        await _courseRepository.SaveChangesAsync();
        return await GetStructureAsync(id);
    }

    public async Task<GenerateFullCourseResponse> GenerateFullCourseAsync(Guid courseId, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        return await _fullCourseGenerationService.GenerateFullCourseAsync(courseId, createdByUserId, cancellationToken);
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

    public Task GenerateLessonQuizAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken = default)
    {
        return _quizGenerationService.GenerateLessonQuizAsync(courseId, lessonId, cancellationToken);
    }

    public Task GenerateFinalQuizAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return _quizGenerationService.GenerateFinalQuizAsync(courseId, cancellationToken);
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

        var lessonQuizLookup = course.Quizzes
            .Where(quiz => quiz.LessonId.HasValue)
            .ToDictionary(quiz => quiz.LessonId!.Value, quiz => quiz);
        var finalQuiz = course.Quizzes.FirstOrDefault(quiz => quiz.CourseId == course.Id && quiz.Type == "Final");

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
                    .Select(lesson => MapLearnLesson(lesson, lessonQuizLookup))
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
            FinalQuizId = finalQuiz?.Id,
            HasFinalQuiz = finalQuiz is not null,
            FinalQuizStatus = finalQuiz?.Status ?? string.Empty,
            FinalQuizQuestionCount = finalQuiz?.QuestionCount ?? 0,
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
            CategoryId = course.CategoryId,
            Title = course.Title,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Category = course.Category?.Name ?? "Chưa phân loại",
            CategoryStatus = course.Category?.Status.ToString() ?? string.Empty,
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
            CategoryId = course.CategoryId,
            Title = course.Title,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Category = course.Category?.Name ?? "Chưa phân loại",
            CategoryStatus = course.Category?.Status.ToString() ?? string.Empty,
            IsPublished = course.IsPublished,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.Sum(module => module.Lessons.Count),
            CreatedAt = course.CreatedAt
        };
    }

    private static PublishedCourseListItemResponse MapPublishedListItem(Course course)
    {
        return new PublishedCourseListItemResponse
        {
            Id = course.Id,
            CategoryId = course.CategoryId,
            Title = course.Title,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Category = course.Category?.Name ?? "Chưa phân loại",
            IsPublished = course.IsPublished,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.Sum(module => module.Lessons.Count),
            CreatedAt = course.CreatedAt
        };
    }

    private static CourseLearnLessonResponse MapLearnLesson(Lesson lesson, IReadOnlyDictionary<Guid, Quiz> lessonQuizLookup)
    {
        lessonQuizLookup.TryGetValue(lesson.Id, out var quiz);

        return new CourseLearnLessonResponse
        {
            LessonId = lesson.Id,
            LessonTitle = lesson.Title,
            Description = lesson.Description,
            OrderIndex = lesson.OrderIndex,
            ContentSeed = lesson.ContentSeed,
            AudioGenerationStatus = lesson.AudioGenerationStatus,
            AudioGenerationError = lesson.AudioGenerationError ?? string.Empty,
            AudioUrl = lesson.AudioUrl ?? string.Empty,
            VideoUrl = lesson.VideoUrl,
            VideoGenerationStatus = lesson.VideoGenerationStatus,
            VideoGenerationError = lesson.VideoGenerationError ?? string.Empty,
            Duration = lesson.Duration,
            QuizId = quiz?.Id,
            QuizStatus = quiz?.Status ?? string.Empty,
            QuizQuestionCount = quiz?.QuestionCount ?? 0
        };
    }
}
