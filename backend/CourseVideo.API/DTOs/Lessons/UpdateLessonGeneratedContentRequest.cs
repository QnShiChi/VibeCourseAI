namespace CourseVideo.API.DTOs.Lessons;

public class UpdateLessonGeneratedContentRequest
{
    public string TeachingScript { get; set; } = string.Empty;
    public string SlideOutlineJson { get; set; } = string.Empty;
    public string VoiceoverPlanJson { get; set; } = string.Empty;
}
