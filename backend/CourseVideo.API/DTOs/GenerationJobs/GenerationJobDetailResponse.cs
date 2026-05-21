namespace CourseVideo.API.DTOs.GenerationJobs;

public class GenerationJobDetailResponse
{
    public Guid Id { get; set; }
    public Guid SyllabusId { get; set; }
    public string SyllabusTitle { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
    public Guid? LessonId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int FailedItems { get; set; }
    public string ProgressMessage { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
