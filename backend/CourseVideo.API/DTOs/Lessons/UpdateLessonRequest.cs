namespace CourseVideo.API.DTOs.Lessons;

public class UpdateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentSeed { get; set; } = string.Empty;
}
