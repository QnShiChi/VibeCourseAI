namespace CourseVideo.API.DTOs.Courses;

public class CourseStructureResponse
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CategoryStatus { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<ModuleStructureResponse> Modules { get; set; } = [];
}
