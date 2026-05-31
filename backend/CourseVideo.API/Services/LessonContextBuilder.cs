using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonContextBuilder : ILessonContextBuilder
{
    private readonly ILessonRepository _lessonRepository;

    public LessonContextBuilder(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<LessonTutorContext> BuildAsync(Guid lessonId, double playbackTimeSeconds, CancellationToken cancellationToken)
    {
        var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(lessonId)
            ?? throw new KeyNotFoundException("Lesson not found.");

        return new LessonTutorContext(
            lesson.Module?.Course?.Title ?? string.Empty,
            lesson.Module?.Title ?? string.Empty,
            lesson.Title,
            lesson.Description,
            lesson.TeachingScript ?? string.Empty,
            lesson.SlideOutlineJson ?? "[]",
            lesson.VoiceoverPlanJson ?? "{}",
            lesson.TranscriptText ?? string.Empty,
            playbackTimeSeconds);
    }
}
