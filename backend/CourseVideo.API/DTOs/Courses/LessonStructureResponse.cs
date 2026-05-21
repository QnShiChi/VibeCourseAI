namespace CourseVideo.API.DTOs.Courses;

public class LessonStructureResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string ContentSeed { get; set; } = string.Empty;
    public string ContentGenerationStatus { get; set; } = string.Empty;
    public string ContentGenerationError { get; set; } = string.Empty;
}
