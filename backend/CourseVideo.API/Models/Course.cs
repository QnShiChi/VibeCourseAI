namespace CourseVideo.API.Models;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public CourseCategory Category { get; set; } = CourseCategory.UiUxDesign;
    public bool IsPublished { get; set; }
    public Guid? SyllabusId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Syllabus? Syllabus { get; set; }
    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<GenerationJob> GenerationJobs { get; set; } = new List<GenerationJob>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
