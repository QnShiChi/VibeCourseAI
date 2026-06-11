namespace CourseVideo.API.Services.Interfaces;

public interface ILessonContextBuilder
{
    Task<LessonTutorContext> BuildAsync(Guid lessonId, double playbackTimeSeconds, CancellationToken cancellationToken);
}

public record LessonTutorContext(
    string CourseTitle,
    string ModuleTitle,
    string LessonTitle,
    string LessonDescription,
    string TeachingScript,
    string SlideOutlineJson,
    string VoiceoverPlanJson,
    string TranscriptText,
    double PlaybackTimeSeconds);
