namespace CourseVideo.API.DTOs.Courses;

public class GenerateLessonVideoResponse
{
    public Guid JobId { get; set; }
    public Guid CourseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int ProcessedLessons { get; set; }
    public int FailedLessons { get; set; }
    public string Message { get; set; } = string.Empty;
}
