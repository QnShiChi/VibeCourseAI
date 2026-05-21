namespace CourseVideo.API.DTOs.Syllabuses;

public class ImportSyllabusResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
