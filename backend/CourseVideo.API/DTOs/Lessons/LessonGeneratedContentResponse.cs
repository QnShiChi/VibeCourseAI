namespace CourseVideo.API.DTOs.Lessons;

public class LessonGeneratedContentResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string TeachingScript { get; set; } = string.Empty;
    public string SlideOutlineJson { get; set; } = string.Empty;
    public string VoiceoverPlanJson { get; set; } = string.Empty;
    public string ContentGenerationStatus { get; set; } = string.Empty;
    public string? ContentGenerationError { get; set; }
    public DateTime? ContentGeneratedAt { get; set; }
}
