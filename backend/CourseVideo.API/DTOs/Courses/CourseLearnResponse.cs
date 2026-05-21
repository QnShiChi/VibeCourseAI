namespace CourseVideo.API.DTOs.Courses;

public class CourseLearnResponse
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseDescription { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public Guid? SelectedLessonId { get; set; }
    public CourseLearnLessonResponse? SelectedLesson { get; set; }
    public IReadOnlyList<CourseLearnModuleResponse> Modules { get; set; } = [];
}
