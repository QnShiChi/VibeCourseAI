namespace CourseVideo.API.Models;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; }
    public Guid? SyllabusId { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
