namespace CourseVideo.API.Models;

public class Syllabus : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<GenerationJob> GenerationJobs { get; set; } = new List<GenerationJob>();
}
