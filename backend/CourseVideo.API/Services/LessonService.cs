using CourseVideo.API.DTOs.Courses;
using CourseVideo.API.DTOs.Lessons;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _lessonRepository;

    public LessonService(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<LessonStructureResponse?> UpdateAsync(Guid id, UpdateLessonRequest request)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id);
        if (lesson is null)
        {
            return null;
        }

        lesson.Title = request.Title.Trim();
        lesson.Description = request.Description.Trim();
        lesson.ContentSeed = request.ContentSeed.Trim();
        lesson.UpdatedAt = DateTime.UtcNow;
        await _lessonRepository.SaveChangesAsync();

        return new LessonStructureResponse
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            OrderIndex = lesson.OrderIndex,
            ContentSeed = lesson.ContentSeed,
            ContentGenerationStatus = lesson.ContentGenerationStatus
        };
    }

    public async Task<LessonGeneratedContentResponse?> GetGeneratedContentAsync(Guid id)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id);
        return lesson is null ? null : MapGeneratedContent(lesson);
    }

    public async Task<LessonGeneratedContentResponse?> UpdateGeneratedContentAsync(Guid id, UpdateLessonGeneratedContentRequest request)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id);
        if (lesson is null)
        {
            return null;
        }

        lesson.TeachingScript = request.TeachingScript.Trim();
        lesson.SlideOutlineJson = request.SlideOutlineJson.Trim();
        lesson.VoiceoverPlanJson = request.VoiceoverPlanJson.Trim();
        lesson.ContentGenerationStatus = "ManuallyEdited";
        lesson.ContentGenerationError = null;
        lesson.ContentGeneratedAt = DateTime.UtcNow;
        lesson.UpdatedAt = DateTime.UtcNow;
        await _lessonRepository.SaveChangesAsync();

        return MapGeneratedContent(lesson);
    }

    private static LessonGeneratedContentResponse MapGeneratedContent(Models.Lesson lesson)
    {
        return new LessonGeneratedContentResponse
        {
            LessonId = lesson.Id,
            LessonTitle = lesson.Title,
            TeachingScript = lesson.TeachingScript ?? string.Empty,
            SlideOutlineJson = lesson.SlideOutlineJson ?? string.Empty,
            VoiceoverPlanJson = lesson.VoiceoverPlanJson ?? string.Empty,
            ContentGenerationStatus = lesson.ContentGenerationStatus,
            ContentGenerationError = lesson.ContentGenerationError,
            ContentGeneratedAt = lesson.ContentGeneratedAt
        };
    }
}
