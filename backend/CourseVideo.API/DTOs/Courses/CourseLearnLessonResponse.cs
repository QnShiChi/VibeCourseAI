namespace CourseVideo.API.DTOs.Courses;

public class CourseLearnLessonResponse
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string ContentSeed { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public int? Duration { get; set; }
}
