namespace CourseVideo.API.DTOs.Courses;

public class ModuleStructureResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public IReadOnlyList<LessonStructureResponse> Lessons { get; set; } = [];
}
