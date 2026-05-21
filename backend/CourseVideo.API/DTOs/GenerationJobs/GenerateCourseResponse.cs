namespace CourseVideo.API.DTOs.GenerationJobs;

public class GenerateCourseResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid SyllabusId { get; set; }
    public Guid? CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
