namespace CourseVideo.API.DTOs.Lessons;

public class LessonAudioSegmentResponse
{
    public int SlideNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
}
