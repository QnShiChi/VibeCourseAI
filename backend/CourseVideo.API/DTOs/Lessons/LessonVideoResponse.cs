namespace CourseVideo.API.DTOs.Lessons;

public class LessonVideoResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public int? Duration { get; set; }
    public string VideoGenerationStatus { get; set; } = string.Empty;
    public string VideoGenerationError { get; set; } = string.Empty;
    public DateTime? VideoGeneratedAt { get; set; }
}
