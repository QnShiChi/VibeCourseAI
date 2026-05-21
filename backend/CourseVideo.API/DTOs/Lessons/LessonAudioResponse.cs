namespace CourseVideo.API.DTOs.Lessons;

public class LessonAudioResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public int? Duration { get; set; }
    public string AudioGenerationStatus { get; set; } = string.Empty;
    public string AudioGenerationError { get; set; } = string.Empty;
    public DateTime? AudioGeneratedAt { get; set; }
    public List<LessonAudioSegmentResponse> Segments { get; set; } = [];
}
