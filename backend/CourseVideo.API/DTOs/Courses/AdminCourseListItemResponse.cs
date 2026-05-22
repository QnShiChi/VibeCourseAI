namespace CourseVideo.API.DTOs.Courses;

public class AdminCourseListItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
