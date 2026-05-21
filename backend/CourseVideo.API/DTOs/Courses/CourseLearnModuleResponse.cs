namespace CourseVideo.API.DTOs.Courses;

public class CourseLearnModuleResponse
{
    public Guid ModuleId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string ModuleDescription { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public IReadOnlyList<CourseLearnLessonResponse> Lessons { get; set; } = [];
}
