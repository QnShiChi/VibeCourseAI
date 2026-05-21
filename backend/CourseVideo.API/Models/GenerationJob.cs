namespace CourseVideo.API.Models;

public class GenerationJob : BaseEntity
{
    public Guid SyllabusId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? LessonId { get; set; }
    public string? JobType { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public int? TotalItems { get; set; }
    public int? ProcessedItems { get; set; }
    public int? FailedItems { get; set; }
    public string? ProgressMessage { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Syllabus? Syllabus { get; set; }
    public Course? Course { get; set; }
    public User? CreatedByUser { get; set; }
}
